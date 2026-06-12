using dotnet_mvc.Data;
using dotnet_mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc;

public static class SeedData
{
    /// <summary>
    /// Popula a base de dados com dados de demonstração realistas.
    /// Idempotente: não faz nada se o admin de demo já existir.
    /// </summary>
    public static async Task SeedDemoAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        if (await db.Users.AnyAsync(u => u.Email == "admin@dec.pt"))
            return;

        // ── Utilizadores ──────────────────────────────────────────────────────
        var admin = new User
        {
            Name       = "Administrador DEC",
            Email      = "admin@dec.pt",
            Role       = "Admin",
            UserStatus = "Active",
            Gender     = "M",
            BirthDate  = new DateTime(1985, 3, 15)
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        var professor = new User
        {
            Name       = "João Silva",
            Email      = "prof.joao@escola.pt",
            Role       = "Professor",
            UserStatus = "Active",
            Gender     = "M",
            BirthDate  = new DateTime(1978, 9, 20)
        };
        professor.PasswordHash = hasher.HashPassword(professor, "Demo@123");

        var student1 = new User
        {
            Name       = "Maria Santos",
            Email      = "maria.santos@escola.pt",
            Role       = "Student",
            UserStatus = "Active",
            Gender     = "F",
            BirthDate  = new DateTime(2008, 4, 12)
        };
        student1.PasswordHash = hasher.HashPassword(student1, "Demo@123");

        var student2 = new User
        {
            Name       = "Pedro Costa",
            Email      = "pedro.costa@escola.pt",
            Role       = "Student",
            UserStatus = "Active",
            Gender     = "M",
            BirthDate  = new DateTime(2008, 11, 5)
        };
        student2.PasswordHash = hasher.HashPassword(student2, "Demo@123");

        db.Users.AddRange(admin, professor, student1, student2);
        await db.SaveChangesAsync();

        // ── Turma ─────────────────────────────────────────────────────────────
        var classroom = new Classroom
        {
            OwnerId        = professor.Id,
            Name           = "10.º A – TIC",
            Description    = "Turma de Tecnologias de Informação e Comunicação – 10.º Ano",
            AccessCode     = "TIC10A",
            IsActive       = true,
            CreatedAtUtc   = DateTime.UtcNow
        };
        db.Classrooms.Add(classroom);
        await db.SaveChangesAsync();

        // ── Desafio ───────────────────────────────────────────────────────────
        var challenge = new Challenge
        {
            ClassroomId       = classroom.Id,
            CreatedByUserId   = professor.Id,
            Title             = "Gestão do Orçamento Familiar",
            Description       = "Analisa o cenário financeiro da tua família e cria um plano para reduzir as despesas mensais em pelo menos 10%, sem comprometer necessidades básicas. Usa os simuladores disponíveis para justificares as tuas escolhas e apresenta uma estratégia de poupança para os próximos 6 meses.",
            AccessCode        = "DESAFIO1",
            Deadline          = new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc),
            NotifyStudents    = false,
            CreatedAtUtc      = DateTime.UtcNow,
            PublishedAtUtc    = DateTime.UtcNow
        };
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        // ── Membros + inscrições ──────────────────────────────────────────────
        db.ClassroomMemberships.AddRange(
            new ClassroomMembership { ClassroomId = classroom.Id, StudentId = student1.Id, JoinedAtUtc = DateTime.UtcNow },
            new ClassroomMembership { ClassroomId = classroom.Id, StudentId = student2.Id, JoinedAtUtc = DateTime.UtcNow }
        );
        db.ChallengeEnrollments.AddRange(
            new ChallengeEnrollment { ChallengeId = challenge.Id, StudentId = student1.Id, JoinedAtUtc = DateTime.UtcNow },
            new ChallengeEnrollment { ChallengeId = challenge.Id, StudentId = student2.Id, JoinedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // ── Cenário – Maria (Casal) ───────────────────────────────────────────
        var now = DateTime.UtcNow;

        var scenarioMaria = new Scenario
        {
            OwnerId       = student1.Id,
            Name          = "Família Santos",
            Location      = "Porto",
            MembersCount  = 4,
            InitialBalance = 1_200.00m,
            ProfileType   = ScenarioProfileType.Casal,
            CreatedAtUtc  = now,
            UpdatedAtUtc  = now
        };
        db.Scenarios.Add(scenarioMaria);
        await db.SaveChangesAsync();

        db.FinancialEntries.AddRange(
            // Receitas
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Income",  Category = "Salário",           Amount =  1_800m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Income",  Category = "Subsídio",          Amount =    150m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            // Despesas
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Expense", Category = "Habitação",         Amount =    650m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Expense", Category = "Alimentação",       Amount =    280m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Expense", Category = "Transportes",       Amount =     75m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Expense", Category = "Telecomunicações",  Amount =     35m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Expense", Category = "Saúde",             Amount =     40m, Month = now.Month, Year = now.Year, IsRecurring = false },
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Expense", Category = "Educação",          Amount =     60m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioMaria.Id, EntryType = "Expense", Category = "Lazer",             Amount =     90m, Month = now.Month, Year = now.Year, IsRecurring = false }
        );
        db.SavingsGoals.Add(new SavingsGoal
        {
            ScenarioId     = scenarioMaria.Id,
            Description    = "Fundo de Emergência",
            TargetAmount   = 1_000m,
            SavedAmount    = 350m,
            DeadlineMonths = 6,
            CreatedAtUtc   = now
        });

        // ── Cenário – Pedro (Estudante deslocado) ─────────────────────────────
        var scenarioPedro = new Scenario
        {
            OwnerId        = student2.Id,
            Name           = "Apartamento em Lisboa",
            Location       = "Lisboa",
            MembersCount   = 1,
            InitialBalance = 850.00m,
            ProfileType    = ScenarioProfileType.StudentDeslocado,
            CreatedAtUtc   = now,
            UpdatedAtUtc   = now
        };
        db.Scenarios.Add(scenarioPedro);
        await db.SaveChangesAsync();

        db.FinancialEntries.AddRange(
            new FinancialEntry { ScenarioId = scenarioPedro.Id, EntryType = "Income",  Category = "Bolsa de Estudo",   Amount = 600m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioPedro.Id, EntryType = "Income",  Category = "Apoio Familiar",    Amount = 300m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioPedro.Id, EntryType = "Expense", Category = "Habitação",         Amount = 450m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioPedro.Id, EntryType = "Expense", Category = "Alimentação",       Amount = 180m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioPedro.Id, EntryType = "Expense", Category = "Transportes",       Amount =  40m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioPedro.Id, EntryType = "Expense", Category = "Telecomunicações",  Amount =  20m, Month = now.Month, Year = now.Year, IsRecurring = true  },
            new FinancialEntry { ScenarioId = scenarioPedro.Id, EntryType = "Expense", Category = "Material escolar",  Amount =  30m, Month = now.Month, Year = now.Year, IsRecurring = false }
        );
        db.SavingsGoals.Add(new SavingsGoal
        {
            ScenarioId     = scenarioPedro.Id,
            Description    = "Viagem de finalistas",
            TargetAmount   = 500m,
            SavedAmount    = 120m,
            DeadlineMonths = 8,
            CreatedAtUtc   = now
        });

        await db.SaveChangesAsync();
    }
}
