-- Schema da base de dados educativa do portal DEC
-- Executado automaticamente pelo MySQL na primeira inicialização do container

SET NAMES utf8mb4;
SET time_zone = '+00:00';

-- ── Utilizadores ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Users (
    Id            INT AUTO_INCREMENT PRIMARY KEY,
    Name          VARCHAR(120)  NOT NULL,
    Email         VARCHAR(256)  NOT NULL,
    PasswordHash  VARCHAR(512)  NOT NULL,
    Role          VARCHAR(32)   NOT NULL DEFAULT 'Student',
    UserStatus    VARCHAR(32)   NOT NULL DEFAULT 'Active',
    Gender        VARCHAR(10)   NULL,
    BirthDate     DATE          NULL,
    ImgUrl        VARCHAR(500)  NULL,
    LastLoginAtUtc DATETIME     NULL,
    CreatedAtUtc  DATETIME      NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UNIQUE KEY UQ_Users_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Cenários financeiros ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Scenarios (
    Id             INT AUTO_INCREMENT PRIMARY KEY,
    OwnerId        INT            NOT NULL,
    Name           VARCHAR(120)   NOT NULL,
    Location       VARCHAR(120)   NOT NULL DEFAULT '',
    MembersCount   INT            NOT NULL DEFAULT 1,
    InitialBalance DECIMAL(12,2)  NOT NULL DEFAULT 0.00,
    ProfileType    INT            NOT NULL DEFAULT 1,
    CreatedAtUtc   DATETIME       NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAtUtc   DATETIME       NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CONSTRAINT FK_Scenarios_Users FOREIGN KEY (OwnerId) REFERENCES Users(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Lançamentos financeiros ───────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS FinancialEntries (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    ScenarioId  INT           NOT NULL,
    EntryType   VARCHAR(16)   NOT NULL,
    Category    VARCHAR(80)   NOT NULL,
    Amount      DECIMAL(12,2) NOT NULL,
    Month       INT           NOT NULL,
    Year        INT           NOT NULL,
    IsRecurring TINYINT(1)    NOT NULL DEFAULT 0,
    CreatedAtUtc DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CONSTRAINT FK_FinancialEntries_Scenarios FOREIGN KEY (ScenarioId) REFERENCES Scenarios(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Objetivos de poupança ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS SavingsGoals (
    Id             INT AUTO_INCREMENT PRIMARY KEY,
    ScenarioId     INT           NOT NULL,
    Description    VARCHAR(200)  NOT NULL,
    TargetAmount   DECIMAL(12,2) NOT NULL,
    SavedAmount    DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    DeadlineMonths INT           NOT NULL DEFAULT 12,
    CreatedAtUtc   DATETIME      NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CONSTRAINT FK_SavingsGoals_Scenarios FOREIGN KEY (ScenarioId) REFERENCES Scenarios(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Histórico de simulações ───────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS SimulationHistories (
    Id                       INT AUTO_INCREMENT PRIMARY KEY,
    UserId                   INT           NOT NULL,
    Strategy                 VARCHAR(32)   NOT NULL,
    RendimentoMensal         DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    DespesasMensais          DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    Meses                    INT           NOT NULL DEFAULT 12,
    TaxaEsforco              DECIMAL(6,3)  NULL,
    TaxaEsforcoClassificacao VARCHAR(80)   NULL,
    MontanteFinal            DECIMAL(14,2) NULL,
    PoupancaAcumulada        DECIMAL(14,2) NULL,
    CreatedAtUtc             DATETIME      NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CONSTRAINT FK_SimulationHistories_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Turmas ────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Classrooms (
    Id           INT AUTO_INCREMENT PRIMARY KEY,
    OwnerId      INT           NOT NULL,
    Name         VARCHAR(120)  NOT NULL,
    Description  VARCHAR(500)  NULL,
    AccessCode   VARCHAR(16)   NOT NULL,
    IsActive     TINYINT(1)    NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME      NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UNIQUE KEY UQ_Classrooms_AccessCode (AccessCode),
    CONSTRAINT FK_Classrooms_Users FOREIGN KEY (OwnerId) REFERENCES Users(Id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Membros das turmas ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ClassroomMemberships (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    ClassroomId INT      NOT NULL,
    StudentId   INT      NOT NULL,
    JoinedAtUtc DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UNIQUE KEY UQ_ClassroomMemberships (ClassroomId, StudentId),
    CONSTRAINT FK_ClassroomMemberships_Classrooms FOREIGN KEY (ClassroomId) REFERENCES Classrooms(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ClassroomMemberships_Users      FOREIGN KEY (StudentId)   REFERENCES Users(Id)      ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Desafios ──────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Challenges (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    ClassroomId     INT           NULL,
    CreatedByUserId INT           NOT NULL,
    Title           VARCHAR(160)  NOT NULL,
    Description     MEDIUMTEXT    NULL,
    AccessCode      VARCHAR(16)   NOT NULL,
    Deadline        DATETIME      NULL,
    NotifyStudents  TINYINT(1)    NOT NULL DEFAULT 0,
    CreatedAtUtc    DATETIME      NOT NULL DEFAULT (UTC_TIMESTAMP()),
    PublishedAtUtc  DATETIME      NULL,
    UNIQUE KEY UQ_Challenges_AccessCode (AccessCode),
    CONSTRAINT FK_Challenges_Classrooms FOREIGN KEY (ClassroomId)     REFERENCES Classrooms(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Challenges_Users      FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)      ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Inscrições em desafios ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ChallengeEnrollments (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    ChallengeId INT      NOT NULL,
    StudentId   INT      NOT NULL,
    JoinedAtUtc DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UNIQUE KEY UQ_ChallengeEnrollments (ChallengeId, StudentId),
    CONSTRAINT FK_ChallengeEnrollments_Challenges FOREIGN KEY (ChallengeId) REFERENCES Challenges(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ChallengeEnrollments_Users      FOREIGN KEY (StudentId)   REFERENCES Users(Id)      ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ── Submissões de desafios ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ChallengeSubmissions (
    Id            INT AUTO_INCREMENT PRIMARY KEY,
    ChallengeId   INT           NOT NULL,
    StudentId     INT           NOT NULL,
    ResponseText  MEDIUMTEXT    NOT NULL,
    Grade         DECIMAL(4,1)  NULL,
    Feedback      TEXT          NULL,
    SubmittedAtUtc DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAtUtc  DATETIME      NOT NULL DEFAULT (UTC_TIMESTAMP()),
    ReviewedAtUtc DATETIME      NULL,
    UNIQUE KEY UQ_ChallengeSubmissions (ChallengeId, StudentId),
    CONSTRAINT FK_ChallengeSubmissions_Challenges FOREIGN KEY (ChallengeId) REFERENCES Challenges(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ChallengeSubmissions_Users      FOREIGN KEY (StudentId)   REFERENCES Users(Id)      ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
