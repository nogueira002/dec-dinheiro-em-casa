using dotnet_mvc.Models;
using dotnet_mvc.Models.DAL;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace dotnet_mvc;

public static class SeedData
{
    public static async Task SeedDemoAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        await using var conn = new MySqlConnection(DriverDB.ConnectionString);
        await conn.OpenAsync();

        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = 'admin@dec.pt'";
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0) return;

        var now = DateTime.UtcNow;

        // ── Utilizadores ──────────────────────────────────────────────────────
        var adminId  = await InsertUserAsync(conn, hasher, "Administrador DEC",    "admin@dec.pt",              "Admin",     "Admin@123", "M", new DateTime(1980, 1, 1));
        var prof1Id  = await InsertUserAsync(conn, hasher, "João Silva",            "prof.joao@escola.pt",       "Professor", "Demo@123",  "M", new DateTime(1978, 9, 20));
        var prof2Id  = await InsertUserAsync(conn, hasher, "Ana Ferreira",          "prof.ana@escola.pt",        "Professor", "Demo@123",  "F", new DateTime(1982, 4, 5));

        var s1Id = await InsertUserAsync(conn, hasher, "Maria Santos",      "maria.santos@escola.pt",    "Student", "Demo@123", "F", new DateTime(2008,  4, 12));
        var s2Id = await InsertUserAsync(conn, hasher, "Pedro Costa",       "pedro.costa@escola.pt",     "Student", "Demo@123", "M", new DateTime(2008, 11,  5));
        var s3Id = await InsertUserAsync(conn, hasher, "Sofia Rodrigues",   "sofia.rodrigues@escola.pt", "Student", "Demo@123", "F", new DateTime(2007,  6, 22));
        var s4Id = await InsertUserAsync(conn, hasher, "Tiago Almeida",     "tiago.almeida@escola.pt",   "Student", "Demo@123", "M", new DateTime(2008,  2, 14));
        var s5Id = await InsertUserAsync(conn, hasher, "Beatriz Oliveira",  "beatriz.o@escola.pt",       "Student", "Demo@123", "F", new DateTime(2007,  9,  3));
        var s6Id = await InsertUserAsync(conn, hasher, "Gonçalo Martins",   "goncalo.m@escola.pt",       "Student", "Demo@123", "M", new DateTime(2008,  7, 18));
        var s7Id = await InsertUserAsync(conn, hasher, "Inês Carvalho",     "ines.carvalho@escola.pt",   "Student", "Demo@123", "F", new DateTime(2007, 12,  1));
        var s8Id = await InsertUserAsync(conn, hasher, "Rui Pereira",       "rui.pereira@escola.pt",     "Student", "Demo@123", "M", new DateTime(2008,  3, 25));

        // ── Turmas ────────────────────────────────────────────────────────────
        var class1Id = await InsertClassroomAsync(conn, prof1Id, "10.º A – TIC",       "Tecnologias de Informação e Comunicação – 10.º Ano", "TIC10A");
        var class2Id = await InsertClassroomAsync(conn, prof1Id, "11.º B – Economia",  "Economia e Finanças Pessoais – 11.º Ano",           "ECO11B");
        var class3Id = await InsertClassroomAsync(conn, prof2Id, "10.º C – Matemática","Matemática Aplicada e Literacia Financeira",         "MAT10C");

        // ── Desafios ──────────────────────────────────────────────────────────
        var deadline1 = new DateTime(2026, 7, 15, 23, 59, 0, DateTimeKind.Utc);
        var deadline2 = new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc);
        var deadline3 = new DateTime(2026, 8,  1, 23, 59, 0, DateTimeKind.Utc);

        var ch1Id = await InsertChallengeAsync(conn, class1Id, prof1Id,
            "Gestão do Orçamento Familiar",
            "Analisa o cenário financeiro da tua família e cria um plano para reduzir as despesas mensais em pelo menos 10%, sem comprometer necessidades básicas. Usa os simuladores disponíveis para justificares as tuas escolhas e apresenta uma estratégia de poupança para os próximos 6 meses.",
            "DESAFIO1", deadline1);

        var ch2Id = await InsertChallengeAsync(conn, class2Id, prof1Id,
            "Simulação de Crédito Habitação",
            "Simula a aquisição de um imóvel com recurso a crédito habitação. Compara pelo menos 3 cenários com diferentes taxas de juro (TAN) e prazos. Calcula a prestação mensal, o custo total do crédito e o impacto no orçamento familiar. Justifica qual o cenário mais vantajoso.",
            "CRED11B", deadline2);

        var ch3Id = await InsertChallengeAsync(conn, class3Id, prof2Id,
            "Plano de Poupança para a Reforma",
            "Elabora um plano de poupança de longo prazo orientado para a reforma. Considerando um horizonte de 30 anos, compara estratégias conservadora, moderada e agressiva usando os simuladores. Apresenta o capital acumulado esperado em cada cenário e justifica a estratégia que recomendas.",
            "POUPC10", deadline3);

        // ── Membros das turmas ────────────────────────────────────────────────
        // Turma 1: s1, s2, s3, s4
        await InsertMembershipsAsync(conn, class1Id, s1Id, s2Id, s3Id, s4Id);
        // Turma 2: s3, s4, s5, s6
        await InsertMembershipsAsync(conn, class2Id, s3Id, s4Id, s5Id, s6Id);
        // Turma 3: s5, s6, s7, s8
        await InsertMembershipsAsync(conn, class3Id, s5Id, s6Id, s7Id, s8Id);

        // ── Inscrições nos desafios (via turma) ───────────────────────────────
        await InsertEnrollmentsAsync(conn, ch1Id, s1Id, s2Id, s3Id, s4Id);
        await InsertEnrollmentsAsync(conn, ch2Id, s3Id, s4Id, s5Id, s6Id);
        await InsertEnrollmentsAsync(conn, ch3Id, s5Id, s6Id, s7Id, s8Id);

        // ── Submissões ────────────────────────────────────────────────────────
        // Desafio 1 — Maria (avaliada), Pedro (avaliado), Sofia (submetida, não avaliada), Tiago (não submeteu)
        await InsertSubmissionAsync(conn, ch1Id, s1Id,
            "Analisei o orçamento da minha família com 4 membros. O rendimento líquido mensal é de 2 400€. Identifiquei que as despesas com telecomunicações (85€) e lazer (120€) podem ser reduzidas em 30%, poupando 61,50€/mês. Com a estratégia conservadora do simulador, ao fim de 6 meses acumulo 369€ de poupança adicional. Proponho também renegociar o seguro automóvel, estimando uma poupança extra de 25€/mês.",
            grade: 17.5m, feedback: "Excelente análise e justificação muito fundamentada. Podias ter explorado mais a componente de crédito habitação.", gradedBy: prof1Id);

        await InsertSubmissionAsync(conn, ch1Id, s2Id,
            "O meu cenário é de estudante deslocado em Lisboa. Rendimento: 900€ (bolsa + apoio familiar). Despesas fixas: 690€. Margem: 210€. Para reduzir 10% das despesas, negoceio um quarto partilhado poupando 50€/mês em habitação. Uso o simulador de capitalização: 150€/mês durante 6 meses a 1% ao ano = 901,12€ de capital acumulado.",
            grade: 15.0m, feedback: "Boa análise do cenário. Faltou comparar mais estratégias de poupança.", gradedBy: prof1Id);

        await InsertSubmissionAsync(conn, ch1Id, s3Id,
            "Família monoparental, 2 membros. Rendimento: 1 650€. Despesas identificadas: habitação 600€, alimentação 320€, transportes 90€, telecomunicações 45€, educação 80€. Redução proposta: pack telecomunicações (−20€) e uso de transportes públicos (−30€). Poupança mensal adicional: 50€. Simulação de 6 meses com estratégia moderada: 302,50€ acumulados.",
            grade: null, feedback: null, gradedBy: null);

        // Desafio 2 — Sofia (avaliada), Tiago (avaliado), Beatriz (submetida), Gonçalo (não submeteu)
        await InsertSubmissionAsync(conn, ch2Id, s3Id,
            "Imóvel de 180 000€. Cenário A: TAN 3%, 30 anos — prestação 758€, custo total 272 880€. Cenário B: TAN 3,5%, 25 anos — prestação 899€, custo total 269 700€. Cenário C: TAN 2,8%, 35 anos — prestação 698€, custo total 293 160€. Recomendo o Cenário B: maior prestação mas menor custo total e menor prazo de endividamento.",
            grade: 18.0m, feedback: "Análise muito completa e bem estruturada. Recomendação fundamentada.", gradedBy: prof1Id);

        await InsertSubmissionAsync(conn, ch2Id, s4Id,
            "Simulei crédito de 150 000€. TAN 3%/20 anos: 832€/mês, total 199 680€. TAN 4%/25 anos: 792€/mês, total 237 600€. TAN 2,5%/30 anos: 593€/mês, total 213 480€. Escolho TAN 3%/20 anos pelo menor custo total apesar da prestação mais alta, pois o esforço mensal é sustentável no cenário familiar.",
            grade: 14.0m, feedback: "Boa comparação. Devias ter incluído o impacto da taxa de esforço em cada cenário.", gradedBy: prof1Id);

        await InsertSubmissionAsync(conn, ch2Id, s5Id,
            "Analisei 3 propostas de crédito para aquisição de habitação no valor de 200 000€. Utilizei o simulador de crédito para calcular as prestações mensais e comparei o custo total. A proposta com TAN 2,9% a 30 anos apresenta a melhor relação entre prestação acessível e custo total controlado.",
            grade: null, feedback: null, gradedBy: null);

        // Desafio 3 — Beatriz (avaliada), Gonçalo (submetido), Inês (não submeteu)
        await InsertSubmissionAsync(conn, ch3Id, s5Id,
            "Horizonte de 30 anos, poupança inicial de 5 000€, depósito mensal de 200€. Estratégia conservadora (3%/ano): 117 324€. Estratégia moderada (6%/ano): 197 409€. Estratégia agressiva (9%/ano): 338 095€. Recomendo a estratégia moderada: rendimento significativamente superior à conservadora com risco controlado, adequado para um horizonte de 30 anos.",
            grade: 19.0m, feedback: "Excelente trabalho! Análise muito rigorosa e recomendação perfeitamente justificada.", gradedBy: prof2Id);

        await InsertSubmissionAsync(conn, ch3Id, s6Id,
            "Comparei as três estratégias com capital inicial de 10 000€ e contribuição mensal de 300€ ao longo de 30 anos. Conservadora (2,5%): 162 000€ estimados. Moderada (5%): 249 000€. Agressiva (8%): 408 000€. Tendo em conta o perfil de risco e a volatilidade dos mercados, defendo uma abordagem moderada com revisão periódica da carteira.",
            grade: null, feedback: null, gradedBy: null);

        // ── Cenários financeiros ──────────────────────────────────────────────
        // Maria — Família Santos (Casal)
        var sc1 = await InsertScenarioAsync(conn, s1Id, "Família Santos", "Porto", 4, 1200m, (int)ScenarioProfileType.Casal);
        await InsertEntriesAsync(conn, sc1, now, new[]
        {
            ("Income",  "Salário",          1800m, true),
            ("Income",  "Subsídio",          150m, true),
            ("Expense", "Habitação",          650m, true),
            ("Expense", "Alimentação",        280m, true),
            ("Expense", "Transportes",         75m, true),
            ("Expense", "Telecomunicações",    35m, true),
            ("Expense", "Saúde",               40m, false),
            ("Expense", "Educação",            60m, true),
            ("Expense", "Lazer",               90m, false),
        });
        await InsertGoalAsync(conn, sc1, "Fundo de Emergência",  1000m, 350m, 6);
        await InsertGoalAsync(conn, sc1, "Férias de Verão",       800m, 200m, 4);

        // Pedro — Estudante deslocado
        var sc2 = await InsertScenarioAsync(conn, s2Id, "Apartamento em Lisboa", "Lisboa", 1, 850m, (int)ScenarioProfileType.StudentDeslocado);
        await InsertEntriesAsync(conn, sc2, now, new[]
        {
            ("Income",  "Bolsa de Estudo",   600m, true),
            ("Income",  "Apoio Familiar",    300m, true),
            ("Expense", "Habitação",         450m, true),
            ("Expense", "Alimentação",       180m, true),
            ("Expense", "Transportes",        40m, true),
            ("Expense", "Telecomunicações",   20m, true),
            ("Expense", "Material escolar",   30m, false),
        });
        await InsertGoalAsync(conn, sc2, "Viagem de finalistas", 500m, 120m, 8);

        // Sofia — Família Monoparental
        var sc3 = await InsertScenarioAsync(conn, s3Id, "Casa da Sofia", "Braga", 2, 600m, (int)ScenarioProfileType.Monoparental);
        await InsertEntriesAsync(conn, sc3, now, new[]
        {
            ("Income",  "Salário",          1650m, true),
            ("Income",  "Pensão alimentos",  250m, true),
            ("Expense", "Habitação",          600m, true),
            ("Expense", "Alimentação",        320m, true),
            ("Expense", "Transportes",         90m, true),
            ("Expense", "Telecomunicações",    45m, true),
            ("Expense", "Educação",            80m, true),
            ("Expense", "Saúde",               50m, false),
        });
        await InsertGoalAsync(conn, sc3, "Fundo Escolar",        600m, 150m, 10);

        // Tiago — Casal jovem
        var sc4 = await InsertScenarioAsync(conn, s4Id, "Apartamento Tiago e Rita", "Coimbra", 2, 500m, (int)ScenarioProfileType.Casal);
        await InsertEntriesAsync(conn, sc4, now, new[]
        {
            ("Income",  "Salário (Tiago)",  1200m, true),
            ("Income",  "Salário (Rita)",   1100m, true),
            ("Expense", "Habitação",          750m, true),
            ("Expense", "Alimentação",        260m, true),
            ("Expense", "Transportes",        120m, true),
            ("Expense", "Telecomunicações",    50m, true),
            ("Expense", "Lazer",              100m, false),
        });
        await InsertGoalAsync(conn, sc4, "Entrada casa própria", 20000m, 3500m, 36);

        // Beatriz — Estudante deslocada
        var sc5 = await InsertScenarioAsync(conn, s5Id, "República em Coimbra", "Coimbra", 4, 300m, (int)ScenarioProfileType.StudentDeslocado);
        await InsertEntriesAsync(conn, sc5, now, new[]
        {
            ("Income",  "Part-time",         400m, true),
            ("Income",  "Apoio Familiar",    350m, true),
            ("Expense", "Habitação",         250m, true),
            ("Expense", "Alimentação",       200m, true),
            ("Expense", "Transportes",        30m, true),
            ("Expense", "Telecomunicações",   20m, true),
            ("Expense", "Livros e material",  40m, false),
        });
        await InsertGoalAsync(conn, sc5, "Intercâmbio Erasmus", 3000m, 800m, 12);

        // Gonçalo — Casal
        var sc6 = await InsertScenarioAsync(conn, s6Id, "Família Martins", "Lisboa", 3, 900m, (int)ScenarioProfileType.Casal);
        await InsertEntriesAsync(conn, sc6, now, new[]
        {
            ("Income",  "Salário",          2100m, true),
            ("Expense", "Habitação",          900m, true),
            ("Expense", "Alimentação",        350m, true),
            ("Expense", "Transportes",        150m, true),
            ("Expense", "Telecomunicações",    60m, true),
            ("Expense", "Educação filhos",    120m, true),
            ("Expense", "Lazer",              100m, false),
        });
        await InsertGoalAsync(conn, sc6, "Fundo de Emergência", 5000m, 1200m, 18);
        await InsertGoalAsync(conn, sc6, "Carro novo",          8000m,  500m, 24);

        // Inês — Estudante deslocada
        var sc7 = await InsertScenarioAsync(conn, s7Id, "Quarto em Aveiro", "Aveiro", 1, 200m, (int)ScenarioProfileType.StudentDeslocado);
        await InsertEntriesAsync(conn, sc7, now, new[]
        {
            ("Income",  "Bolsa de Estudo",   750m, true),
            ("Income",  "Apoio Familiar",    200m, true),
            ("Expense", "Habitação",         350m, true),
            ("Expense", "Alimentação",       220m, true),
            ("Expense", "Transportes",        50m, true),
            ("Expense", "Telecomunicações",   25m, true),
        });
        await InsertGoalAsync(conn, sc7, "Computador novo",     1200m, 400m, 6);

        // Rui — Casal
        var sc8 = await InsertScenarioAsync(conn, s8Id, "Casa dos Pereira", "Porto", 2, 1100m, (int)ScenarioProfileType.Casal);
        await InsertEntriesAsync(conn, sc8, now, new[]
        {
            ("Income",  "Salário (Rui)",    1800m, true),
            ("Income",  "Salário (Ana)",    1600m, true),
            ("Expense", "Crédito Habitação", 780m, true),
            ("Expense", "Alimentação",       300m, true),
            ("Expense", "Transportes",       130m, true),
            ("Expense", "Telecomunicações",   55m, true),
            ("Expense", "Saúde",              60m, false),
            ("Expense", "Lazer",             120m, false),
        });
        await InsertGoalAsync(conn, sc8, "Obras em casa",       6000m, 2000m, 12);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<int> InsertUserAsync(
        MySqlConnection conn, IPasswordHasher<User> hasher,
        string name, string email, string role, string password, string gender, DateTime birthDate)
    {
        var dummy = new User { Name = name, Email = email, Role = role, UserStatus = "Active" };
        var hash  = hasher.HashPassword(dummy, password);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Users (Name, Email, PasswordHash, Role, UserStatus, Gender, BirthDate, CreatedAtUtc)
            VALUES (@n, @e, @h, @r, 'Active', @g, @b, UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@e", email);
        cmd.Parameters.AddWithValue("@h", hash);
        cmd.Parameters.AddWithValue("@r", role);
        cmd.Parameters.AddWithValue("@g", gender);
        cmd.Parameters.AddWithValue("@b", birthDate.Date);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> InsertClassroomAsync(
        MySqlConnection conn, int ownerId, string name, string description, string accessCode)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Classrooms (OwnerId, Name, Description, AccessCode, IsActive, CreatedAtUtc)
            VALUES (@o, @n, @d, @c, 1, UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@d", description);
        cmd.Parameters.AddWithValue("@c", accessCode);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> InsertChallengeAsync(
        MySqlConnection conn, int classroomId, int professorId,
        string title, string description, string accessCode, DateTime deadline)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Challenges (ClassroomId, CreatedByUserId, Title, Description, AccessCode, Deadline, NotifyStudents, CreatedAtUtc, PublishedAtUtc)
            VALUES (@cl, @p, @t, @d, @ac, @dl, 0, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;
        cmd.Parameters.AddWithValue("@cl", classroomId);
        cmd.Parameters.AddWithValue("@p",  professorId);
        cmd.Parameters.AddWithValue("@t",  title);
        cmd.Parameters.AddWithValue("@d",  description);
        cmd.Parameters.AddWithValue("@ac", accessCode);
        cmd.Parameters.AddWithValue("@dl", deadline);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task InsertMembershipsAsync(MySqlConnection conn, int classroomId, params int[] studentIds)
    {
        foreach (var sid in studentIds)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT IGNORE INTO ClassroomMemberships (ClassroomId, StudentId, JoinedAtUtc)
                VALUES (@c, @s, UTC_TIMESTAMP());
                """;
            cmd.Parameters.AddWithValue("@c", classroomId);
            cmd.Parameters.AddWithValue("@s", sid);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertEnrollmentsAsync(MySqlConnection conn, int challengeId, params int[] studentIds)
    {
        foreach (var sid in studentIds)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT IGNORE INTO ChallengeEnrollments (ChallengeId, StudentId, JoinedAtUtc)
                VALUES (@c, @s, UTC_TIMESTAMP());
                """;
            cmd.Parameters.AddWithValue("@c", challengeId);
            cmd.Parameters.AddWithValue("@s", sid);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertSubmissionAsync(
        MySqlConnection conn, int challengeId, int studentId,
        string responseText, decimal? grade, string? feedback, int? gradedBy)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ChallengeSubmissions (ChallengeId, StudentId, ResponseText, Grade, Feedback, ReviewedAtUtc, SubmittedAtUtc, UpdatedAtUtc)
            VALUES (@ch, @s, @r, @g, @f, @ra, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            """;
        cmd.Parameters.AddWithValue("@ch", challengeId);
        cmd.Parameters.AddWithValue("@s",  studentId);
        cmd.Parameters.AddWithValue("@r",  responseText);
        cmd.Parameters.AddWithValue("@g",  grade.HasValue ? (object)grade.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@f",  feedback      ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ra", grade.HasValue ? (object)DateTime.UtcNow : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> InsertScenarioAsync(
        MySqlConnection conn, int ownerId, string name, string location, int members, decimal balance, int profileType)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Scenarios (OwnerId, Name, Location, MembersCount, InitialBalance, ProfileType, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@o, @n, @l, @m, @b, @pt, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;
        cmd.Parameters.AddWithValue("@o",  ownerId);
        cmd.Parameters.AddWithValue("@n",  name);
        cmd.Parameters.AddWithValue("@l",  location);
        cmd.Parameters.AddWithValue("@m",  members);
        cmd.Parameters.AddWithValue("@b",  balance);
        cmd.Parameters.AddWithValue("@pt", profileType);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task InsertEntriesAsync(
        MySqlConnection conn, int scenarioId, DateTime now,
        IEnumerable<(string type, string cat, decimal amount, bool recurring)> entries)
    {
        foreach (var (type, cat, amount, recurring) in entries)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO FinancialEntries (ScenarioId, EntryType, Category, Amount, Month, Year, IsRecurring, CreatedAtUtc)
                VALUES (@s, @t, @c, @a, @mo, @y, @r, UTC_TIMESTAMP());
                """;
            cmd.Parameters.AddWithValue("@s",  scenarioId);
            cmd.Parameters.AddWithValue("@t",  type);
            cmd.Parameters.AddWithValue("@c",  cat);
            cmd.Parameters.AddWithValue("@a",  amount);
            cmd.Parameters.AddWithValue("@mo", now.Month);
            cmd.Parameters.AddWithValue("@y",  now.Year);
            cmd.Parameters.AddWithValue("@r",  recurring ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertGoalAsync(
        MySqlConnection conn, int scenarioId, string description, decimal target, decimal saved, int deadlineMonths)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO SavingsGoals (ScenarioId, Description, TargetAmount, SavedAmount, DeadlineMonths, CreatedAtUtc)
            VALUES (@s, @d, @t, @sv, @dm, UTC_TIMESTAMP());
            """;
        cmd.Parameters.AddWithValue("@s",  scenarioId);
        cmd.Parameters.AddWithValue("@d",  description);
        cmd.Parameters.AddWithValue("@t",  target);
        cmd.Parameters.AddWithValue("@sv", saved);
        cmd.Parameters.AddWithValue("@dm", deadlineMonths);
        await cmd.ExecuteNonQueryAsync();
    }
}
