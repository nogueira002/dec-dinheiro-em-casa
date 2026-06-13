# DEC – Dinheiro em Casa · Documentação técnica do portal .NET

## Arquitetura geral

O projeto é um **ASP.NET Core 8 MVC** com padrão clássico Controller → Service → DbContext. Não há API REST no portal — tudo é server-rendered com Razor Views. As únicas chamadas HTTP externas são para a **API Django** de cálculo financeiro.

---

## Autenticação e controlo de acesso

### Como o login funciona
`AuthController.Login` (POST) recebe email + password, vai à BD, verifica o hash com `IPasswordHasher<User>.VerifyHashedPassword()` (PBKDF2-SHA256), e se estiver correto cria um cookie de sessão via `HttpContext.SignInAsync()`.

### Claims (o que fica no cookie)
`AppClaimsFactory.CriarClaims(user)` gera:

| Claim | Valor |
|-------|-------|
| `NameIdentifier` | ID do utilizador (int) |
| `Name` | Nome |
| `Email` | Email |
| `Role` | `"Student"` / `"Professor"` / `"Admin"` |
| `dec:role_label` | Label legível ("Aluno", "Professor", "Administrador") |

### Duração da sessão
- Normal: **8 horas** (`AuthConstants.SessionLifetime`)
- "Manter sessão": **30 dias** (`AuthConstants.PersistentSessionLifetime`)

### Proteção dos controllers
```csharp
[Authorize(Roles = "Student")]           // StudentController
[Authorize(Roles = "Professor,Admin")]   // ProfessorController
[Authorize(Roles = "Admin")]             // AdminController
```
Se não autenticado → redireciona para `/Auth/Login`.  
Se autenticado mas sem permissão → `/Auth/AccessDenied`.

### Extensions de conveniência
`ClaimsPrincipalExtensions` expõe:
- `User.GetUserId()` → `int?` — lê o `NameIdentifier`
- `User.GetDisplayName()` → string
- `User.GetRoleLabel()` → string (claim `dec:role_label`)

---

## Controllers

### `HomeController`
| Ação | O que faz |
|------|-----------|
| `Index` | Landing page pública. Se já autenticado, redireciona para a dashboard do papel |
| `Privacy` | Página estática |
| `Error` | Página de erro com `RequestId` |

### `AuthController`
| Ação | O que faz |
|------|-----------|
| `Login` GET/POST | Valida credenciais, cria cookie, redireciona para `ReturnUrl` |
| `Register` GET/POST | Cria utilizador com `PasswordHasher`, papel selecionado no formulário |
| `ForgotPassword` GET/POST | Placeholder — mostra confirmação mas não envia email real |
| `Logout` POST | `HttpContext.SignOutAsync()` |
| `AccessDenied` GET | Página de acesso negado |

### `StudentController` (requer papel `Student`)
| Ação | O que faz |
|------|-----------|
| `Dashboard` | Chama `StudentDashboardService` — agrega saldo, receitas/despesas do mês, objetivos, desafios pendentes |
| `Scenarios` / `CreateScenario` / `EditScenario` | CRUD de cenários familiares via `ScenarioService` |
| `Entries` / `AddEntry` / `UpdateEntry` / `DeleteEntry` | Lançamentos financeiros via `FinancialEntryService` |
| `Objectives` / `CreateObjective` / `AddSavings` / `DeleteObjective` | Objetivos de poupança via `SavingsGoalService` |
| `Calculator` + 4 ações POST | Cada calculadora envia para a API Django e mostra o resultado na mesma view |
| `Simulations` | Simula cenário completo via API Django; guarda histórico com `SimulationHistoryService` |
| `Challenges` | Lista desafios inscritos via `AcademicManagementService` |
| `ChallengeDetails` / `ChallengeWorkspace` | Ver e submeter respostas a desafios; workspace tem acesso às ferramentas |
| `JoinCode` | Junta-se a turma ou desafio pelo código de acesso |
| `ScenarioQuickData` | Endpoint JSON (AJAX) — devolve dados resumidos do cenário ativo |

### `ProfessorController` (requer `Professor` ou `Admin`)
| Ação | O que faz |
|------|-----------|
| `Dashboard` | Resumo: turmas ativas, desafios, submissões pendentes |
| `Classes` | Lista turmas. **Admin vê todas**; Professor vê as suas |
| `Challenges` | Lista desafios. **Admin vê todos**; Professor vê os seus |
| `Submissions` | Lista submissões com filtros. **Admin vê todas** |
| `Progress` | Lista alunos com métricas. **Admin vê todos** |
| `CreateClass` / `CreateChallenge` / `EditChallenge` | Criação e edição |
| `AssignChallenge` | Atribui desafio a turma. **Admin vê todas as turmas e desafios** |
| `ReviewSubmission` | Vê resposta do aluno, atribui nota (0–20) e feedback |
| `RemoveStudent` | Remove aluno de turma |

### `AdminController` (requer `Admin`)
| Ação | O que faz |
|------|-----------|
| `Dashboard` | Stats globais: total utilizadores, turmas, submissões |
| `Users` | Lista utilizadores com filtros (papel, estado, pesquisa por nome/email) |
| `ToggleUserStatus` | Ativa/desativa conta |
| `EditUserRole` | Muda papel do utilizador |
| `Monitoring` | Estado do sistema via `MonitoringService` |

---

## Serviços

### `AcademicManagementService`
O mais complexo. Gere tudo o que é académico:
- Turmas, membros, desafios, submissões, progresso
- Queries EF Core com `.AsNoTracking()` e projeções anónimas para performance
- **Distinção Admin/Professor**: métodos `GetAll*` (sem filtro de owner) vs `GetProfessor*` (filtrado por `OwnerId` ou `CreatedByUserId`)
- `JoinByAccessCodeAsync` — código único para turma ou desafio, cria `ClassroomMembership` ou `ChallengeEnrollment` conforme o tipo

### `ScenarioService`
CRUD de cenários familiares (`Scenario`). Verifica que o `OwnerId` corresponde ao utilizador antes de editar/apagar.

### `FinancialEntryService`
Lançamentos financeiros (`FinancialEntry`). Operações simples de create/update/delete com validação de ownership.

### `SavingsGoalService`
Objetivos de poupança (`SavingsGoal`). Inclui `AddSavingsAsync` que incrementa `SavedAmount`.

### `SimulationHistoryService`
Guarda e lista histórico de simulações (`SimulationHistory`). O controller passa `limit: 8` — não há paginação real, apenas `.Take(limit)`.

### `StudentDashboardService`
Agrega dados para a dashboard do aluno: saldo atual, total receitas/despesas do mês, objetivos com percentagem, lista de desafios pendentes. Usa `.Take(5)` ou `.Take(6)` para listas resumidas.

### `AdminUserService`
Pesquisa de utilizadores com filtros opcionais (papel, estado, texto). Operações de toggle de estado e mudança de papel.

### `MonitoringService`
Verifica saúde do sistema (BD acessível, API Django a responder, etc.).

### `AccessCodeService`
Gera códigos únicos alfanuméricos para turmas e desafios. Verifica colisões na BD antes de confirmar.

---

## Comunicação com a API Django

A API Django corre em `http://django-api:8000/api/` (Docker) e faz os cálculos financeiros — o portal **não calcula nada localmente**.

### `FinanceApiClient` — 5 endpoints

| Método | Endpoint | O que calcula |
|--------|----------|---------------|
| `CalcTaxaEsforcoAsync` | `POST /taxa-esforco/` | Taxa de esforço financeiro (encargos ÷ rendimento) |
| `CalcCapitalizacaoAsync` | `POST /capitalizacao/` | Montante final com juros compostos |
| `CalcCreditoAsync` | `POST /simular-credito/` | Prestação mensal e custo total de crédito |
| `CalcProjecaoAsync` | `POST /projecao-poupanca/` | Projeção de poupança com inflação |
| `SimulateAsync` | `POST /simulate/` | Simulação completa (agrega os 4 acima numa só chamada) |

### Detalhes técnicos
- **HTTP/1.1 forçado** — o Django wsgiref não suporta HTTP/2
- **Content-Length explícito** em vez de Transfer-Encoding chunked (limitação do wsgiref)
- **Certificado auto-assinado** aceite via `DangerousAcceptAnyServerCertificateValidator` (Nginx local)
- **Timeout**: 15 segundos
- Erros da API tratados em dois formatos: `{"erro": "..."}` e erros DRF `{"campo": ["mensagem"]}`

### Fluxo de uma calculadora (ex: Taxa de Esforço)
```
Utilizador preenche form → POST /Student/CalculatorEsforco
→ StudentController valida ModelState
→ chama FinanceApiClient.CalcTaxaEsforcoAsync(request)
→ HTTP POST para django-api:8000/api/taxa-esforco/
→ deserializa TaxaEsforcoResult
→ view mostra resultado (ou erro)
```

---

## Acesso à base de dados

### EF Core (principal)
Todos os serviços recebem `AppDbContext` por injeção. MySQL 8.0 via `Pomelo.EntityFrameworkCore.MySql`. Migrações correm automaticamente no arranque (`db.Database.Migrate()`).

### DAL legado (`DriverDB` / `*CRUD`)
`FinancialEntryCRUD` e `ScenarioCRUD` são classes legadas com ADO.NET direto (stored procedures). A connection string é passada estaticamente via `DriverDB.ConnectionString = connectionString` em `Program.cs`. Coexistem com o EF Core.

---

## Paginação

**Não existe paginação real.** Os dados são carregados todos de uma vez. O que existe é truncagem com `.Take(n)`:

| Local | Limite |
|-------|--------|
| Dashboard do aluno (listas resumidas) | `.Take(5)` / `.Take(6)` |
| Histórico de simulações | `.Take(8)` (parâmetro `limit`) |
| Listas de desafios no serviço académico | `.Take(5)` |

Listas longas (turmas, alunos, submissões) são carregadas integralmente.

---

## Layouts

| Layout | Usado em |
|--------|----------|
| `_AuthLayout` | Login, Register, ForgotPassword — split-screen azul + branco |
| `_DashboardLayout` + `_Sidebar` | Todas as páginas autenticadas |
| `Layout = null` (standalone) | Landing page (`Home/Index`) |

---

## Seed de demo

`SeedData.SeedDemoAsync()` corre automaticamente no arranque (após migração). É idempotente — verifica se `admin@dec.pt` já existe antes de inserir.

| Utilizador | Email | Password | Papel |
|------------|-------|----------|-------|
| Administrador DEC | `admin@dec.pt` | `Admin@123` | Admin |
| João Silva | `prof.joao@escola.pt` | `Demo@123` | Professor |
| Maria Santos | `maria.santos@escola.pt` | `Demo@123` | Student |
| Pedro Costa | `pedro.costa@escola.pt` | `Demo@123` | Student |

Dados criados: turma **"10.º A – TIC"** (código `TIC10A`), desafio **"Gestão do Orçamento Familiar"** (código `DESAFIO1`), cenários financeiros com lançamentos e objetivos de poupança para ambos os alunos.
