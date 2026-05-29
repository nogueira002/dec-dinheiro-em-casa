# Infraestrutura Docker — DEC Dinheiro Em Casa

## Visão Geral

O projeto utiliza Docker para orquestrar múltiplos serviços numa arquitectura de microserviços. Toda a comunicação externa passa pelo Nginx, que actua como reverse proxy seguro. Os serviços internos nunca estão expostos directamente ao exterior.

---

## Arquitectura

```
Internet (browser)
        │
        ▼ porta 80 (HTTP)
   ┌─────────┐
   │  Nginx  │  → redireciona automaticamente para HTTPS
   └────┬────┘
        │
        ▼ porta 443 (HTTPS)
   ┌─────────┐
   │  Nginx  │  ← único ponto de entrada público
   └────┬────┘
        │
   ┌────┴────────────────────┐
   │                         │
   ▼ /api/*                  ▼ /*
┌──────────────┐      ┌──────────────┐
│  Django API  │      │ Portal .NET  │
│  (Python)    │      │    (C# MVC)  │
│  porta 8000  │      │  porta 8080  │
└──────────────┘      └──────┬───────┘
                             │
                             ▼
                      ┌──────────────┐
                      │    MySQL     │
                      │ dec_educativa│
                      │  (interno)   │
                      └──────────────┘
```

---

## Serviços

### Nginx (Reverse Proxy)
- Único serviço exposto ao exterior (portas 80 e 443)
- Porta 80 redireciona automaticamente para HTTPS (porta 443)
- Distribui o tráfego pelos serviços internos consoante o URL:
  - `/api/*` → Django API
  - `/*` → Portal .NET MVC
- Termina o TLS — os serviços internos comunicam em HTTP simples dentro da rede Docker

### Portal .NET MVC
- Aplicação web educativa desenvolvida em C# com ASP.NET Core 8.0
- Gere autenticação de utilizadores (Alunos, Professores, Administradores)
- Comunica com a base de dados MySQL
- Corre internamente na porta 8080, nunca exposta ao exterior

### Django REST API
- API de simulação financeira desenvolvida em Python com Django e Django REST Framework
- Endpoints de cálculo: taxa de esforço, capitalização, simulação de crédito, projecção de poupança
- Sem base de dados — todos os cálculos são feitos em memória
- Documentação automática via Swagger UI em `/api/docs/`
- Corre internamente na porta 8000, nunca exposta ao exterior

### MySQL
- Base de dados relacional da plataforma educativa
- Apenas acessível internamente — sem qualquer porta exposta ao exterior
- Dados persistidos num Docker Volume para sobreviver a restarts
- Base de dados: `dec_educativa`

---

## Segurança

### HTTPS com certificado Self-Signed
O Nginx está configurado com TLS usando um certificado auto-assinado gerado com OpenSSL:

```bash
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/dec.key \
  -out nginx/certs/dec.crt \
  -subj "/C=PT/ST=Porto/L=Porto/O=ISTEC/OU=DEC/CN=localhost"
```

- **TLS 1.2 e 1.3** — protocolos modernos e seguros
- O browser apresenta um aviso porque o certificado não foi emitido por uma CA pública — em produção substituir por um certificado Let's Encrypt
- As chaves privadas (`nginx/certs/`) estão no `.gitignore` e nunca são enviadas para o repositório

### Rede Docker Interna
Todos os serviços partilham uma rede Docker privada (`internal`). O MySQL e os servidores de aplicação não têm portas mapeadas para o host — só o Nginx comunica com o exterior.

### Containers Non-Root
Todos os containers (Django API e Portal .NET) correm com utilizadores de sistema sem privilégios de root, limitando o impacto em caso de comprometimento.

### Variáveis de Ambiente
Credenciais e configurações sensíveis são geridas através de ficheiros `.env`, nunca hardcoded no código ou nos Dockerfiles. O repositório inclui apenas o `.env.example` como template.

---

## Health Checks

Cada serviço tem um health check configurado. O Docker Compose garante que os serviços só arrancam quando as suas dependências estão saudáveis:

| Serviço | Health Check | Dependência |
|---|---|---|
| `mysql` | `mysqladmin ping` | — |
| `portal` | `curl http://localhost:8080/` | mysql healthy |
| `django-api` | `curl http://localhost:8000/api/` | — |
| `nginx` | `wget http://localhost/` | portal + django-api healthy |

---

## Ficheiros do Projecto

```
dec-dinheiro-em-casa/
├── docker-compose.yml          # Orquestração de todos os serviços
├── .env.example                # Template de variáveis de ambiente
├── .env                        # Variáveis reais (não commitado)
├── nginx/
│   ├── nginx.conf              # Configuração do reverse proxy + TLS
│   └── certs/                  # Certificados SSL (não commitados)
│       ├── dec.crt
│       └── dec.key
├── django-api/
│   ├── Dockerfile              # Imagem Python + Gunicorn
│   └── ...
└── dotnet-mvc/
    ├── Dockerfile              # Imagem .NET 8 (build multistage)
    └── ...
```

---

## Como Executar

### Pré-requisitos
- Docker Desktop instalado e em execução

### Primeira execução
```bash
# 1. Copiar o template de variáveis
cp .env.example .env

# 2. Editar o .env com as passwords desejadas
# (qualquer valor serve para desenvolvimento local)

# 3. Gerar o certificado SSL
mkdir -p nginx/certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/dec.key \
  -out nginx/certs/dec.crt \
  -subj "/C=PT/ST=Porto/L=Porto/O=ISTEC/OU=DEC/CN=localhost"

# 4. Arrancar todos os serviços
docker compose up --build -d
```

### Comandos úteis
```bash
# Ver o estado de todos os containers
docker compose ps

# Ver logs em tempo real
docker compose logs -f

# Ver logs de um serviço específico
docker compose logs -f portal
docker compose logs -f django-api

# Parar todos os serviços
docker compose down

# Parar e apagar os volumes (reset completo da BD)
docker compose down -v
```

### URLs disponíveis
| URL | Descrição |
|---|---|
| `https://localhost/` | Portal educativo (.NET MVC) |
| `https://localhost/api/` | Django REST API |
| `https://localhost/api/docs/` | Swagger UI — documentação da API |

> O browser vai mostrar um aviso de certificado por ser self-signed. Clicar em "Avançar" / "Proceed" para continuar.

---

## Conceitos de Redes Aplicados

| Conceito | Aplicação no Projecto |
|---|---|
| **Reverse Proxy** | Nginx recebe todos os pedidos e encaminha para o serviço correcto |
| **TLS/HTTPS** | Tráfego encriptado entre cliente e Nginx com certificado X.509 |
| **Rede privada** | Rede Docker interna — serviços comunicam entre si sem exposição externa |
| **Isolamento de portas** | MySQL e servidores app sem portas públicas — só o Nginx é acessível |
| **Virtual Hosting** | Nginx serve múltiplos serviços no mesmo IP/porta com base no URL path |
| **HTTP → HTTPS redirect** | Redirecionamento automático 301 de porta 80 para 443 |

---

## Acesso à Base de Dados em Desenvolvimento

Por defeito o MySQL não tem portas expostas ao exterior. Para aceder durante o desenvolvimento, adiciona temporariamente o `ports` ao serviço `mysql` no `docker-compose.yml`:

```yaml
mysql:
  ports:
    - "3307:3306"
```

Depois reinicia os containers:

```bash
docker compose down --remove-orphans
docker compose up -d
```

### Ligação com cliente gráfico (TablePlus, DBeaver, MySQL Workbench)

| Campo | Valor |
|---|---|
| Host | `127.0.0.1` |
| Port | `3307` |
| User | `dec_portal` |
| Password | `portalpass123` |
| Database | `dec_educativa` |

### Ligação no Rider (appsettings.json)

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;port=3307;database=dec_educativa;user=dec_portal;password=portalpass123"
}
```

> **Importante:** Remover o `ports` do `docker-compose.yml` antes de fazer commit — a base de dados nunca deve ficar exposta em produção.
