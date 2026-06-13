-- ============================================================
-- DEC – Dinheiro em Casa
-- Stored Procedures – Portal .NET (dec_educativa)
-- Executar na base de dados dec_educativa após as migrações
-- ============================================================

DELIMITER $$

-- ============================================================
-- UTILIZADORES / AUTENTICAÇÃO
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetUserByEmail$$
CREATE PROCEDURE sp_GetUserByEmail(IN p_email VARCHAR(256))
BEGIN
    SELECT Id, Name, Gender, BirthDate, Email, ImgUrl,
           PasswordHash, Role, UserStatus, LastLoginAtUtc
    FROM Users
    WHERE Email = p_email
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_CreateUser$$
CREATE PROCEDURE sp_CreateUser(
    IN  p_name          VARCHAR(120),
    IN  p_email         VARCHAR(256),
    IN  p_password_hash VARCHAR(512),
    IN  p_role          VARCHAR(32),
    IN  p_gender        VARCHAR(10),
    OUT p_id            INT,
    OUT p_ha_erro       TINYINT(1),
    OUT p_mensagem      VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = ''; SET p_id = 0;
    IF EXISTS (SELECT 1 FROM Users WHERE Email = p_email) THEN
        SET p_ha_erro = 1;
        SET p_mensagem = 'Este email já está registado.';
    ELSE
        INSERT INTO Users (Name, Email, PasswordHash, Role, Gender, UserStatus)
        VALUES (p_name, p_email, p_password_hash, p_role, NULLIF(p_gender,''), 'Active');
        SET p_id = LAST_INSERT_ID();
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateUserLastLogin$$
CREATE PROCEDURE sp_UpdateUserLastLogin(IN p_user_id INT)
BEGIN
    UPDATE Users SET LastLoginAtUtc = UTC_TIMESTAMP() WHERE Id = p_user_id;
END$$

-- ============================================================
-- CENÁRIOS FAMILIARES
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetScenarioById$$
CREATE PROCEDURE sp_GetScenarioById(IN p_scenario_id INT, IN p_owner_id INT)
BEGIN
    SELECT Id, OwnerId, Name, Location, MembersCount, InitialBalance,
           ProfileType, CreatedAtUtc, UpdatedAtUtc
    FROM Scenarios
    WHERE Id = p_scenario_id AND OwnerId = p_owner_id
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_CreateScenario$$
CREATE PROCEDURE sp_CreateScenario(
    IN  p_owner_id        INT,
    IN  p_name            VARCHAR(120),
    IN  p_location        VARCHAR(120),
    IN  p_members_count   INT,
    IN  p_initial_balance DECIMAL(12,2),
    IN  p_profile_type    INT,
    OUT p_id              INT,
    OUT p_ha_erro         TINYINT(1),
    OUT p_mensagem        VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = ''; SET p_id = 0;
    INSERT INTO Scenarios (OwnerId, Name, Location, MembersCount, InitialBalance,
                           ProfileType, CreatedAtUtc, UpdatedAtUtc)
    VALUES (p_owner_id, p_name, p_location, p_members_count, p_initial_balance,
            p_profile_type, UTC_TIMESTAMP(), UTC_TIMESTAMP());
    SET p_id = LAST_INSERT_ID();
END$$

DROP PROCEDURE IF EXISTS sp_UpdateScenario$$
CREATE PROCEDURE sp_UpdateScenario(
    IN  p_scenario_id     INT,
    IN  p_owner_id        INT,
    IN  p_name            VARCHAR(120),
    IN  p_location        VARCHAR(120),
    IN  p_members_count   INT,
    IN  p_initial_balance DECIMAL(12,2),
    IN  p_profile_type    INT,
    OUT p_ha_erro         TINYINT(1),
    OUT p_mensagem        VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Scenarios WHERE Id = p_scenario_id AND OwnerId = p_owner_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Cenário não encontrado ou acesso negado.';
    ELSE
        UPDATE Scenarios
        SET Name = p_name, Location = p_location, MembersCount = p_members_count,
            InitialBalance = p_initial_balance, ProfileType = p_profile_type,
            UpdatedAtUtc = UTC_TIMESTAMP()
        WHERE Id = p_scenario_id AND OwnerId = p_owner_id;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteScenario$$
CREATE PROCEDURE sp_DeleteScenario(
    IN  p_scenario_id INT,
    IN  p_owner_id    INT,
    OUT p_ha_erro     TINYINT(1),
    OUT p_mensagem    VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Scenarios WHERE Id = p_scenario_id AND OwnerId = p_owner_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Cenário não encontrado ou acesso negado.';
    ELSE
        DELETE FROM Scenarios WHERE Id = p_scenario_id AND OwnerId = p_owner_id;
    END IF;
END$$

-- ============================================================
-- OBJETIVOS DE POUPANÇA
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetScenarioGoals$$
CREATE PROCEDURE sp_GetScenarioGoals(IN p_scenario_id INT, IN p_owner_id INT)
BEGIN
    SELECT sg.Id, sg.ScenarioId, sg.Description, sg.TargetAmount,
           sg.SavedAmount, sg.DeadlineMonths, sg.CreatedAtUtc
    FROM SavingsGoals sg
    INNER JOIN Scenarios s ON sg.ScenarioId = s.Id AND s.OwnerId = p_owner_id
    WHERE sg.ScenarioId = p_scenario_id
    ORDER BY sg.CreatedAtUtc ASC;
END$$

DROP PROCEDURE IF EXISTS sp_CreateSavingsGoal$$
CREATE PROCEDURE sp_CreateSavingsGoal(
    IN  p_scenario_id     INT,
    IN  p_owner_id        INT,
    IN  p_description     VARCHAR(200),
    IN  p_target_amount   DECIMAL(12,2),
    IN  p_deadline_months INT,
    OUT p_id              INT,
    OUT p_ha_erro         TINYINT(1),
    OUT p_mensagem        VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = ''; SET p_id = 0;
    IF NOT EXISTS (SELECT 1 FROM Scenarios WHERE Id = p_scenario_id AND OwnerId = p_owner_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Cenário não encontrado ou acesso negado.';
    ELSE
        INSERT INTO SavingsGoals (ScenarioId, Description, TargetAmount, SavedAmount, DeadlineMonths, CreatedAtUtc)
        VALUES (p_scenario_id, p_description, p_target_amount, 0, p_deadline_months, UTC_TIMESTAMP());
        SET p_id = LAST_INSERT_ID();
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_AddSavingsToGoal$$
CREATE PROCEDURE sp_AddSavingsToGoal(
    IN  p_goal_id  INT,
    IN  p_owner_id INT,
    IN  p_amount   DECIMAL(12,2),
    OUT p_ha_erro  TINYINT(1),
    OUT p_mensagem VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (
        SELECT 1 FROM SavingsGoals sg
        INNER JOIN Scenarios s ON sg.ScenarioId = s.Id AND s.OwnerId = p_owner_id
        WHERE sg.Id = p_goal_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Objetivo não encontrado ou acesso negado.';
    ELSE
        UPDATE SavingsGoals sg
        INNER JOIN Scenarios s ON sg.ScenarioId = s.Id AND s.OwnerId = p_owner_id
        SET sg.SavedAmount = LEAST(sg.SavedAmount + p_amount, sg.TargetAmount)
        WHERE sg.Id = p_goal_id;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteSavingsGoal$$
CREATE PROCEDURE sp_DeleteSavingsGoal(
    IN  p_goal_id  INT,
    IN  p_owner_id INT,
    OUT p_ha_erro  TINYINT(1),
    OUT p_mensagem VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (
        SELECT 1 FROM SavingsGoals sg
        INNER JOIN Scenarios s ON sg.ScenarioId = s.Id AND s.OwnerId = p_owner_id
        WHERE sg.Id = p_goal_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Objetivo não encontrado ou acesso negado.';
    ELSE
        DELETE sg FROM SavingsGoals sg
        INNER JOIN Scenarios s ON sg.ScenarioId = s.Id AND s.OwnerId = p_owner_id
        WHERE sg.Id = p_goal_id;
    END IF;
END$$

-- ============================================================
-- HISTÓRICO DE SIMULAÇÕES
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetSimulationHistory$$
CREATE PROCEDURE sp_GetSimulationHistory(IN p_user_id INT, IN p_limit INT)
BEGIN
    SELECT Id, UserId, Strategy, RendimentoMensal, DespesasMensais, Meses,
           TaxaEsforco, TaxaEsforcoClassificacao, MontanteFinal, PoupancaAcumulada, CreatedAtUtc
    FROM SimulationHistories
    WHERE UserId = p_user_id
    ORDER BY CreatedAtUtc DESC
    LIMIT p_limit;
END$$

DROP PROCEDURE IF EXISTS sp_SaveSimulation$$
CREATE PROCEDURE sp_SaveSimulation(
    IN  p_user_id             INT,
    IN  p_strategy            VARCHAR(32),
    IN  p_rendimento_mensal   DECIMAL(12,2),
    IN  p_despesas_mensais    DECIMAL(12,2),
    IN  p_meses               INT,
    IN  p_taxa_esforco        DECIMAL(6,3),
    IN  p_taxa_esforco_class  VARCHAR(80),
    IN  p_montante_final      DECIMAL(14,2),
    IN  p_poupanca_acumulada  DECIMAL(14,2),
    OUT p_id                  INT)
BEGIN
    INSERT INTO SimulationHistories (
        UserId, Strategy, RendimentoMensal, DespesasMensais, Meses,
        TaxaEsforco, TaxaEsforcoClassificacao, MontanteFinal, PoupancaAcumulada, CreatedAtUtc)
    VALUES (
        p_user_id, p_strategy, p_rendimento_mensal, p_despesas_mensais, p_meses,
        p_taxa_esforco, p_taxa_esforco_class, p_montante_final, p_poupanca_acumulada,
        UTC_TIMESTAMP());
    SET p_id = LAST_INSERT_ID();
END$$

-- ============================================================
-- DESAFIOS DO ALUNO
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetStudentChallenges$$
CREATE PROCEDURE sp_GetStudentChallenges(IN p_student_id INT)
BEGIN
    SELECT c.Id, c.Title, c.Description, c.Deadline,
           COALESCE(cls.Name,'') AS ClassroomName,
           COALESCE(u.Name,'')   AS ProfessorName,
           (SELECT COUNT(*) FROM ChallengeSubmissions
            WHERE ChallengeId = c.Id AND StudentId = p_student_id)                AS HasSubmitted,
           (SELECT Grade FROM ChallengeSubmissions
            WHERE ChallengeId = c.Id AND StudentId = p_student_id LIMIT 1)        AS Grade,
           (SELECT SubmittedAtUtc FROM ChallengeSubmissions
            WHERE ChallengeId = c.Id AND StudentId = p_student_id LIMIT 1)        AS SubmittedAtUtc
    FROM ChallengeEnrollments ce
    INNER JOIN Challenges c   ON ce.ChallengeId = c.Id AND c.PublishedAtUtc IS NOT NULL
    LEFT  JOIN Classrooms cls ON c.ClassroomId  = cls.Id
    LEFT  JOIN Users u        ON c.CreatedByUserId = u.Id
    WHERE ce.StudentId = p_student_id
    ORDER BY c.Deadline ASC, c.Title ASC;
END$$

DROP PROCEDURE IF EXISTS sp_GetStudentChallengeDetails$$
CREATE PROCEDURE sp_GetStudentChallengeDetails(IN p_student_id INT, IN p_challenge_id INT)
BEGIN
    SELECT c.Id, c.Title, c.Description, c.Deadline,
           COALESCE(cls.Name,'') AS ClassroomName,
           COALESCE(u.Name,'')   AS ProfessorName,
           cs.Id                 AS SubmissionId,
           cs.ResponseText,
           cs.SubmittedAtUtc,
           cs.UpdatedAtUtc,
           cs.Grade,
           cs.Feedback,
           cs.ReviewedAtUtc
    FROM ChallengeEnrollments ce
    INNER JOIN Challenges c   ON ce.ChallengeId = c.Id AND c.PublishedAtUtc IS NOT NULL
    LEFT  JOIN Classrooms cls ON c.ClassroomId  = cls.Id
    LEFT  JOIN Users u        ON c.CreatedByUserId = u.Id
    LEFT  JOIN ChallengeSubmissions cs
               ON cs.ChallengeId = c.Id AND cs.StudentId = p_student_id
    WHERE ce.StudentId = p_student_id AND c.Id = p_challenge_id
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_SaveStudentSubmission$$
CREATE PROCEDURE sp_SaveStudentSubmission(
    IN  p_student_id    INT,
    IN  p_challenge_id  INT,
    IN  p_response_text MEDIUMTEXT,
    OUT p_ha_erro       TINYINT(1),
    OUT p_mensagem      VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM ChallengeEnrollments
                   WHERE ChallengeId = p_challenge_id AND StudentId = p_student_id) THEN
        SET p_ha_erro = 1;
        SET p_mensagem = 'Não estás inscrito neste desafio.';
    ELSEIF (SELECT COALESCE(Deadline,'9999-12-31') FROM Challenges WHERE Id = p_challenge_id) < CURDATE() THEN
        SET p_ha_erro = 2;
        SET p_mensagem = 'O prazo deste desafio já terminou. Já não é possível submeter ou editar a resposta.';
    ELSEIF EXISTS (SELECT 1 FROM ChallengeSubmissions
                   WHERE ChallengeId = p_challenge_id AND StudentId = p_student_id
                     AND Grade IS NOT NULL) THEN
        SET p_ha_erro = 3;
        SET p_mensagem = 'Esta submissão já foi avaliada e deixou de poder ser alterada.';
    ELSE
        INSERT INTO ChallengeSubmissions
            (ChallengeId, StudentId, ResponseText, SubmittedAtUtc, UpdatedAtUtc)
        VALUES (p_challenge_id, p_student_id, p_response_text, UTC_TIMESTAMP(), UTC_TIMESTAMP())
        ON DUPLICATE KEY UPDATE
            ResponseText = p_response_text, UpdatedAtUtc = UTC_TIMESTAMP();
    END IF;
END$$

-- ============================================================
-- DASHBOARD DO ALUNO
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetStudentDashboardSummary$$
CREATE PROCEDURE sp_GetStudentDashboardSummary(IN p_user_id INT, IN p_month INT, IN p_year INT)
BEGIN
    SELECT s.Id AS ScenarioId, s.Name AS ScenarioName, s.InitialBalance,
        COALESCE((SELECT SUM(SavedAmount) FROM SavingsGoals WHERE ScenarioId = s.Id), 0) AS TotalSaved,
        COALESCE((SELECT SUM(Amount) FROM FinancialEntries
                  WHERE ScenarioId = s.Id AND EntryType = 'Income'
                    AND Month = p_month AND Year = p_year), 0) AS MonthIncome,
        COALESCE((SELECT SUM(Amount) FROM FinancialEntries
                  WHERE ScenarioId = s.Id AND EntryType = 'Expense'
                    AND Month = p_month AND Year = p_year), 0) AS MonthExpense
    FROM Scenarios s
    WHERE s.OwnerId = p_user_id
    ORDER BY s.UpdatedAtUtc DESC
    LIMIT 1;
END$$

-- ============================================================
-- TURMAS
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetProfessorClassrooms$$
CREATE PROCEDURE sp_GetProfessorClassrooms(IN p_professor_id INT)
BEGIN
    SELECT cr.Id, cr.Name, cr.Description, cr.AccessCode, cr.IsActive, cr.CreatedAtUtc,
        (SELECT COUNT(*) FROM ClassroomMemberships WHERE ClassroomId = cr.Id)
            AS StudentCount,
        (SELECT COUNT(*) FROM Challenges
         WHERE ClassroomId = cr.Id AND PublishedAtUtc IS NOT NULL)
            AS TotalChallenges,
        (SELECT COUNT(*) FROM Challenges
         WHERE ClassroomId = cr.Id AND PublishedAtUtc IS NOT NULL AND Deadline >= CURDATE())
            AS ActiveChallenges,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id WHERE c.ClassroomId = cr.Id)
            AS TotalSubmissions,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id
         WHERE c.ClassroomId = cr.Id AND cs.Grade IS NOT NULL)
            AS ReviewedSubmissions
    FROM Classrooms cr
    WHERE cr.OwnerId = p_professor_id
    ORDER BY cr.CreatedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetAllClassrooms$$
CREATE PROCEDURE sp_GetAllClassrooms()
BEGIN
    SELECT cr.Id, cr.Name, cr.Description, cr.AccessCode, cr.IsActive, cr.CreatedAtUtc,
        u.Name AS OwnerName,
        (SELECT COUNT(*) FROM ClassroomMemberships WHERE ClassroomId = cr.Id)
            AS StudentCount,
        (SELECT COUNT(*) FROM Challenges
         WHERE ClassroomId = cr.Id AND PublishedAtUtc IS NOT NULL)
            AS TotalChallenges,
        (SELECT COUNT(*) FROM Challenges
         WHERE ClassroomId = cr.Id AND PublishedAtUtc IS NOT NULL AND Deadline >= CURDATE())
            AS ActiveChallenges,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id WHERE c.ClassroomId = cr.Id)
            AS TotalSubmissions,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id
         WHERE c.ClassroomId = cr.Id AND cs.Grade IS NOT NULL)
            AS ReviewedSubmissions
    FROM Classrooms cr
    INNER JOIN Users u ON cr.OwnerId = u.Id
    ORDER BY cr.CreatedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetClassroomMembers$$
CREATE PROCEDURE sp_GetClassroomMembers(IN p_classroom_id INT)
BEGIN
    SELECT u.Id AS StudentId, u.Name, u.Email, cm.JoinedAtUtc,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id
         WHERE c.ClassroomId = p_classroom_id AND cs.StudentId = u.Id)            AS Submitted,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id
         WHERE c.ClassroomId = p_classroom_id AND cs.StudentId = u.Id
           AND cs.Grade IS NOT NULL)                                               AS Reviewed,
        (SELECT COALESCE(AVG(cs.Grade), 0) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id
         WHERE c.ClassroomId = p_classroom_id AND cs.StudentId = u.Id
           AND cs.Grade IS NOT NULL)                                               AS AverageGrade,
        (SELECT COUNT(*) FROM Challenges
         WHERE ClassroomId = p_classroom_id AND PublishedAtUtc IS NOT NULL)        AS TotalChallenges
    FROM ClassroomMemberships cm
    INNER JOIN Users u ON cm.StudentId = u.Id
    WHERE cm.ClassroomId = p_classroom_id
    ORDER BY u.Name;
END$$

DROP PROCEDURE IF EXISTS sp_GetClassroomChallenges$$
CREATE PROCEDURE sp_GetClassroomChallenges(IN p_classroom_id INT)
BEGIN
    SELECT c.Id, c.Title, c.AccessCode, c.Deadline,
        (c.Deadline IS NOT NULL AND c.Deadline < NOW()) AS IsExpired,
        (SELECT COUNT(*) FROM ClassroomMemberships WHERE ClassroomId = p_classroom_id)
            AS ClassroomMemberCount,
        (SELECT COUNT(*) FROM ChallengeEnrollments WHERE ChallengeId = c.Id)
            AS DirectEnrollCount
    FROM Challenges c
    WHERE c.ClassroomId = p_classroom_id AND c.PublishedAtUtc IS NOT NULL
    ORDER BY c.Deadline ASC, c.Title ASC;
END$$

DROP PROCEDURE IF EXISTS sp_GetClassroomOptions$$
CREATE PROCEDURE sp_GetClassroomOptions(IN p_professor_id INT, IN p_only_active TINYINT(1))
BEGIN
    SELECT Id, Name FROM Classrooms
    WHERE (p_professor_id = 0 OR OwnerId = p_professor_id)
      AND (p_only_active  = 0 OR IsActive = 1)
    ORDER BY Name;
END$$

DROP PROCEDURE IF EXISTS sp_CreateClassroom$$
CREATE PROCEDURE sp_CreateClassroom(
    IN  p_owner_id    INT,
    IN  p_name        VARCHAR(120),
    IN  p_description TEXT,
    IN  p_access_code VARCHAR(16),
    OUT p_id          INT,
    OUT p_ha_erro     TINYINT(1),
    OUT p_mensagem    VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = ''; SET p_id = 0;
    IF EXISTS (SELECT 1 FROM Classrooms WHERE AccessCode = p_access_code) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Código de acesso já existe. Tenta novamente.';
    ELSE
        INSERT INTO Classrooms (OwnerId, Name, Description, AccessCode, IsActive, CreatedAtUtc)
        VALUES (p_owner_id, p_name, NULLIF(p_description,''), p_access_code, 1, UTC_TIMESTAMP());
        SET p_id = LAST_INSERT_ID();
    END IF;
END$$

-- ============================================================
-- DESAFIOS
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetProfessorChallenges$$
CREATE PROCEDURE sp_GetProfessorChallenges(IN p_professor_id INT)
BEGIN
    SELECT c.Id, c.Title, c.Description, c.AccessCode, c.CreatedAtUtc, c.PublishedAtUtc, c.Deadline,
        COALESCE(cls.Name,'') AS ClassroomName,
        CASE WHEN c.ClassroomId IS NOT NULL
             THEN (SELECT COUNT(*) FROM ClassroomMemberships WHERE ClassroomId = c.ClassroomId) +
                  (SELECT COUNT(*) FROM ChallengeEnrollments   WHERE ChallengeId = c.Id)
             ELSE 0 END AS AudienceCount
    FROM Challenges c
    LEFT JOIN Classrooms cls ON c.ClassroomId = cls.Id
    WHERE c.CreatedByUserId = p_professor_id
    ORDER BY c.CreatedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetAllChallenges$$
CREATE PROCEDURE sp_GetAllChallenges()
BEGIN
    SELECT c.Id, c.Title, c.Description, c.AccessCode, c.CreatedAtUtc, c.PublishedAtUtc, c.Deadline,
        COALESCE(cls.Name,'') AS ClassroomName,
        u.Name AS CreatedByName,
        CASE WHEN c.ClassroomId IS NOT NULL
             THEN (SELECT COUNT(*) FROM ClassroomMemberships WHERE ClassroomId = c.ClassroomId) +
                  (SELECT COUNT(*) FROM ChallengeEnrollments   WHERE ChallengeId = c.Id)
             ELSE 0 END AS AudienceCount
    FROM Challenges c
    LEFT JOIN Classrooms cls ON c.ClassroomId = cls.Id
    INNER JOIN Users u ON c.CreatedByUserId = u.Id
    ORDER BY c.CreatedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetChallengeOptions$$
CREATE PROCEDURE sp_GetChallengeOptions(IN p_professor_id INT, IN p_only_draft TINYINT(1))
BEGIN
    SELECT Id, Title, AccessCode FROM Challenges
    WHERE (p_professor_id = 0 OR CreatedByUserId = p_professor_id)
      AND (p_only_draft   = 0 OR PublishedAtUtc IS NULL OR ClassroomId IS NULL OR Deadline IS NULL)
    ORDER BY CreatedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetChallengeForEdit$$
CREATE PROCEDURE sp_GetChallengeForEdit(IN p_professor_id INT, IN p_challenge_id INT)
BEGIN
    SELECT Id, Title, Description, AccessCode, Deadline, ClassroomId,
           NotifyStudents, PublishedAtUtc, CreatedAtUtc
    FROM Challenges
    WHERE Id = p_challenge_id AND CreatedByUserId = p_professor_id
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_CreateChallenge$$
CREATE PROCEDURE sp_CreateChallenge(
    IN  p_professor_id INT,
    IN  p_title        VARCHAR(160),
    IN  p_description  TEXT,
    IN  p_access_code  VARCHAR(16),
    OUT p_id           INT,
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = ''; SET p_id = 0;
    IF EXISTS (SELECT 1 FROM Challenges WHERE AccessCode = p_access_code) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Código de acesso já existe.';
    ELSE
        INSERT INTO Challenges (CreatedByUserId, Title, Description, AccessCode,
                                NotifyStudents, CreatedAtUtc)
        VALUES (p_professor_id, p_title, p_description, p_access_code, 0, UTC_TIMESTAMP());
        SET p_id = LAST_INSERT_ID();
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateChallenge$$
CREATE PROCEDURE sp_UpdateChallenge(
    IN  p_challenge_id INT,
    IN  p_professor_id INT,
    IN  p_title        VARCHAR(160),
    IN  p_description  TEXT,
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Challenges
                   WHERE Id = p_challenge_id AND CreatedByUserId = p_professor_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Desafio não encontrado ou acesso negado.';
    ELSE
        UPDATE Challenges SET Title = p_title, Description = p_description
        WHERE Id = p_challenge_id AND CreatedByUserId = p_professor_id;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_AssignChallenge$$
CREATE PROCEDURE sp_AssignChallenge(
    IN  p_challenge_id INT,
    IN  p_professor_id INT,
    IN  p_classroom_id INT,
    IN  p_deadline     DATETIME,
    IN  p_access_code  VARCHAR(16),
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Challenges
                   WHERE Id = p_challenge_id AND CreatedByUserId = p_professor_id) THEN
        SET p_ha_erro = 1;
        SET p_mensagem = 'O desafio selecionado não existe ou já não está disponível.';
    ELSEIF EXISTS (SELECT 1 FROM Challenges
                   WHERE Id = p_challenge_id AND PublishedAtUtc IS NOT NULL
                     AND ClassroomId IS NOT NULL) THEN
        SET p_ha_erro = 2;
        SET p_mensagem = 'Este desafio já foi atribuído a uma turma.';
    ELSEIF NOT EXISTS (SELECT 1 FROM Classrooms WHERE Id = p_classroom_id AND IsActive = 1) THEN
        SET p_ha_erro = 3;
        SET p_mensagem = 'A turma selecionada não existe ou já não está disponível.';
    ELSE
        UPDATE Challenges
        SET ClassroomId = p_classroom_id, Deadline = p_deadline,
            AccessCode = p_access_code, PublishedAtUtc = UTC_TIMESTAMP()
        WHERE Id = p_challenge_id AND CreatedByUserId = p_professor_id;
        INSERT IGNORE INTO ChallengeEnrollments (ChallengeId, StudentId, JoinedAtUtc)
        SELECT p_challenge_id, StudentId, UTC_TIMESTAMP()
        FROM ClassroomMemberships WHERE ClassroomId = p_classroom_id;
    END IF;
END$$

-- ============================================================
-- SUBMISSÕES / AVALIAÇÕES
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetProfessorSubmissions$$
CREATE PROCEDURE sp_GetProfessorSubmissions(IN p_professor_id INT,
                                             IN p_challenge_id INT,
                                             IN p_classroom_id INT)
BEGIN
    SELECT cs.Id, cs.ChallengeId, c.Title AS ChallengeTitle,
           COALESCE(cls.Name,'') AS ClassroomName,
           cs.StudentId, u.Name AS StudentName,
           cs.SubmittedAtUtc, cs.UpdatedAtUtc, cs.Grade
    FROM ChallengeSubmissions cs
    INNER JOIN Challenges c ON cs.ChallengeId = c.Id
        AND c.CreatedByUserId = p_professor_id AND c.PublishedAtUtc IS NOT NULL
    LEFT  JOIN Classrooms cls ON c.ClassroomId = cls.Id
    INNER JOIN Users u ON cs.StudentId = u.Id
    WHERE (p_challenge_id = 0 OR cs.ChallengeId  = p_challenge_id)
      AND (p_classroom_id = 0 OR c.ClassroomId   = p_classroom_id)
    ORDER BY cs.SubmittedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetAllSubmissions$$
CREATE PROCEDURE sp_GetAllSubmissions(IN p_challenge_id INT, IN p_classroom_id INT)
BEGIN
    SELECT cs.Id, cs.ChallengeId, c.Title AS ChallengeTitle,
           COALESCE(cls.Name,'') AS ClassroomName,
           cs.StudentId, u.Name AS StudentName,
           cs.SubmittedAtUtc, cs.UpdatedAtUtc, cs.Grade
    FROM ChallengeSubmissions cs
    INNER JOIN Challenges c   ON cs.ChallengeId = c.Id AND c.PublishedAtUtc IS NOT NULL
    LEFT  JOIN Classrooms cls ON c.ClassroomId  = cls.Id
    INNER JOIN Users u        ON cs.StudentId   = u.Id
    WHERE (p_challenge_id = 0 OR cs.ChallengeId = p_challenge_id)
      AND (p_classroom_id = 0 OR c.ClassroomId  = p_classroom_id)
    ORDER BY cs.SubmittedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetSubmissionForReview$$
CREATE PROCEDURE sp_GetSubmissionForReview(IN p_professor_id INT, IN p_submission_id INT)
BEGIN
    SELECT cs.Id, cs.ChallengeId, c.Title AS ChallengeTitle,
           COALESCE(cls.Name,'') AS ClassroomName,
           cs.StudentId, u.Name AS StudentName,
           cs.ResponseText, cs.SubmittedAtUtc, cs.UpdatedAtUtc,
           cs.Grade, cs.Feedback
    FROM ChallengeSubmissions cs
    INNER JOIN Challenges c   ON cs.ChallengeId = c.Id AND c.CreatedByUserId = p_professor_id
    LEFT  JOIN Classrooms cls ON c.ClassroomId  = cls.Id
    INNER JOIN Users u        ON cs.StudentId   = u.Id
    WHERE cs.Id = p_submission_id
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_ReviewSubmission$$
CREATE PROCEDURE sp_ReviewSubmission(
    IN  p_professor_id  INT,
    IN  p_submission_id INT,
    IN  p_grade         DECIMAL(4,1),
    IN  p_feedback      TEXT,
    OUT p_ha_erro       TINYINT(1),
    OUT p_mensagem      VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (
        SELECT 1 FROM ChallengeSubmissions cs
        INNER JOIN Challenges c ON cs.ChallengeId = c.Id AND c.CreatedByUserId = p_professor_id
        WHERE cs.Id = p_submission_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Submissão não encontrada ou acesso negado.';
    ELSE
        UPDATE ChallengeSubmissions
        SET Grade = p_grade, Feedback = NULLIF(p_feedback,''),
            ReviewedAtUtc = UTC_TIMESTAMP(), UpdatedAtUtc = UTC_TIMESTAMP()
        WHERE Id = p_submission_id;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_RemoveStudentFromClassroom$$
CREATE PROCEDURE sp_RemoveStudentFromClassroom(
    IN  p_professor_id INT,
    IN  p_classroom_id INT,
    IN  p_student_id   INT,
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Classrooms
                   WHERE Id = p_classroom_id AND OwnerId = p_professor_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Turma não encontrada ou acesso negado.';
    ELSE
        DELETE FROM ClassroomMemberships
        WHERE ClassroomId = p_classroom_id AND StudentId = p_student_id;
    END IF;
END$$

-- ============================================================
-- ALUNOS / PROGRESSO
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetProfessorStudents$$
CREATE PROCEDURE sp_GetProfessorStudents(IN p_professor_id INT, IN p_classroom_id INT)
BEGIN
    SELECT u.Id AS StudentId, u.Name, u.Email, cm.JoinedAtUtc,
           cr.Id AS ClassroomId, cr.Name AS ClassroomName,
        (SELECT COUNT(*) FROM Challenges
         WHERE ClassroomId = cr.Id AND PublishedAtUtc IS NOT NULL)                  AS TotalChallenges,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs2
         INNER JOIN Challenges c2 ON cs2.ChallengeId = c2.Id
         WHERE c2.ClassroomId = cr.Id AND cs2.StudentId = u.Id)                    AS Submitted,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs2
         INNER JOIN Challenges c2 ON cs2.ChallengeId = c2.Id
         WHERE c2.ClassroomId = cr.Id AND cs2.StudentId = u.Id
           AND cs2.Grade IS NOT NULL)                                               AS Reviewed,
        (SELECT COALESCE(AVG(cs2.Grade),0) FROM ChallengeSubmissions cs2
         INNER JOIN Challenges c2 ON cs2.ChallengeId = c2.Id
         WHERE c2.ClassroomId = cr.Id AND cs2.StudentId = u.Id
           AND cs2.Grade IS NOT NULL)                                               AS AverageGrade
    FROM ClassroomMemberships cm
    INNER JOIN Users u       ON cm.StudentId    = u.Id
    INNER JOIN Classrooms cr ON cm.ClassroomId  = cr.Id AND cr.OwnerId = p_professor_id
    WHERE (p_classroom_id = 0 OR cr.Id = p_classroom_id)
    ORDER BY u.Name, cr.Name;
END$$

DROP PROCEDURE IF EXISTS sp_GetAllStudents$$
CREATE PROCEDURE sp_GetAllStudents(IN p_classroom_id INT)
BEGIN
    SELECT u.Id AS StudentId, u.Name, u.Email, cm.JoinedAtUtc,
           cr.Id AS ClassroomId, cr.Name AS ClassroomName,
        (SELECT COUNT(*) FROM Challenges
         WHERE ClassroomId = cr.Id AND PublishedAtUtc IS NOT NULL)                  AS TotalChallenges,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs2
         INNER JOIN Challenges c2 ON cs2.ChallengeId = c2.Id
         WHERE c2.ClassroomId = cr.Id AND cs2.StudentId = u.Id)                    AS Submitted,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs2
         INNER JOIN Challenges c2 ON cs2.ChallengeId = c2.Id
         WHERE c2.ClassroomId = cr.Id AND cs2.StudentId = u.Id
           AND cs2.Grade IS NOT NULL)                                               AS Reviewed,
        (SELECT COALESCE(AVG(cs2.Grade),0) FROM ChallengeSubmissions cs2
         INNER JOIN Challenges c2 ON cs2.ChallengeId = c2.Id
         WHERE c2.ClassroomId = cr.Id AND cs2.StudentId = u.Id
           AND cs2.Grade IS NOT NULL)                                               AS AverageGrade
    FROM ClassroomMemberships cm
    INNER JOIN Users u       ON cm.StudentId    = u.Id
    INNER JOIN Classrooms cr ON cm.ClassroomId  = cr.Id
    WHERE (p_classroom_id = 0 OR cr.Id = p_classroom_id)
    ORDER BY u.Name, cr.Name;
END$$

-- ============================================================
-- CÓDIGO DE ACESSO (JOIN)
-- ============================================================

DROP PROCEDURE IF EXISTS sp_FindByAccessCode$$
CREATE PROCEDURE sp_FindByAccessCode(IN p_code VARCHAR(16))
BEGIN
    SELECT 'Classroom' AS TargetType, Id, Name AS TargetName,
           NULL AS ClassroomId, Name AS ClassroomName
    FROM Classrooms WHERE AccessCode = p_code AND IsActive = 1
    UNION ALL
    SELECT 'Challenge', c.Id, c.Title,
           c.ClassroomId,
           (SELECT Name FROM Classrooms WHERE Id = c.ClassroomId)
    FROM Challenges c
    WHERE c.AccessCode = p_code AND c.PublishedAtUtc IS NOT NULL
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_JoinClassroom$$
CREATE PROCEDURE sp_JoinClassroom(
    IN  p_student_id   INT,
    IN  p_classroom_id INT,
    OUT p_created      TINYINT(1),
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = ''; SET p_created = 0;
    IF EXISTS (SELECT 1 FROM ClassroomMemberships
               WHERE ClassroomId = p_classroom_id AND StudentId = p_student_id) THEN
        SET p_mensagem = 'Já és membro desta turma.';
    ELSE
        INSERT INTO ClassroomMemberships (ClassroomId, StudentId, JoinedAtUtc)
        VALUES (p_classroom_id, p_student_id, UTC_TIMESTAMP());
        INSERT IGNORE INTO ChallengeEnrollments (ChallengeId, StudentId, JoinedAtUtc)
        SELECT Id, p_student_id, UTC_TIMESTAMP()
        FROM Challenges WHERE ClassroomId = p_classroom_id AND PublishedAtUtc IS NOT NULL;
        SET p_created = 1;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_JoinChallenge$$
CREATE PROCEDURE sp_JoinChallenge(
    IN  p_student_id   INT,
    IN  p_challenge_id INT,
    OUT p_created      TINYINT(1),
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    DECLARE v_classroom_id INT DEFAULT NULL;
    SET p_ha_erro = 0; SET p_mensagem = ''; SET p_created = 0;
    SELECT ClassroomId INTO v_classroom_id FROM Challenges WHERE Id = p_challenge_id LIMIT 1;
    IF v_classroom_id IS NOT NULL AND EXISTS (
        SELECT 1 FROM ClassroomMemberships
        WHERE ClassroomId = v_classroom_id AND StudentId = p_student_id) THEN
        SET p_mensagem = 'Já tens acesso a este desafio pela turma.';
    ELSEIF EXISTS (SELECT 1 FROM ChallengeEnrollments
                   WHERE ChallengeId = p_challenge_id AND StudentId = p_student_id) THEN
        SET p_mensagem = 'Já estás inscrito neste desafio.';
    ELSE
        INSERT INTO ChallengeEnrollments (ChallengeId, StudentId, JoinedAtUtc)
        VALUES (p_challenge_id, p_student_id, UTC_TIMESTAMP());
        SET p_created = 1;
    END IF;
END$$

-- ============================================================
-- DASHBOARD DO PROFESSOR
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetProfessorDashboard$$
CREATE PROCEDURE sp_GetProfessorDashboard(IN p_professor_id INT)
BEGIN
    SELECT
        (SELECT COUNT(*) FROM Classrooms
         WHERE OwnerId = p_professor_id AND IsActive = 1)                             AS ActiveClassrooms,
        (SELECT COUNT(*) FROM Challenges
         WHERE CreatedByUserId = p_professor_id AND PublishedAtUtc IS NOT NULL
           AND (Deadline IS NULL OR Deadline > UTC_TIMESTAMP()))                      AS ActiveChallenges,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id
         WHERE c.CreatedByUserId = p_professor_id)                                    AS TotalSubmissions,
        (SELECT COUNT(*) FROM ChallengeSubmissions cs
         INNER JOIN Challenges c ON cs.ChallengeId = c.Id
         WHERE c.CreatedByUserId = p_professor_id AND cs.Grade IS NULL)               AS PendingReviews,
        (SELECT COUNT(DISTINCT cm.StudentId) FROM ClassroomMemberships cm
         INNER JOIN Classrooms cr ON cm.ClassroomId = cr.Id
         WHERE cr.OwnerId = p_professor_id)                                           AS TotalStudents;
END$$

DROP PROCEDURE IF EXISTS sp_GetRecentProfessorSubmissions$$
CREATE PROCEDURE sp_GetRecentProfessorSubmissions(IN p_professor_id INT, IN p_limit INT)
BEGIN
    SELECT cs.SubmittedAtUtc, cs.Grade,
           u.Name  AS StudentName,
           c.Title AS ChallengeTitle,
           COALESCE(cls.Name,'') AS ClassroomName
    FROM ChallengeSubmissions cs
    INNER JOIN Challenges c   ON cs.ChallengeId = c.Id AND c.CreatedByUserId = p_professor_id
    LEFT  JOIN Classrooms cls ON c.ClassroomId  = cls.Id
    INNER JOIN Users u        ON cs.StudentId   = u.Id
    ORDER BY cs.SubmittedAtUtc DESC
    LIMIT p_limit;
END$$

-- ============================================================
-- ADMIN
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetAdminDashboard$$
CREATE PROCEDURE sp_GetAdminDashboard()
BEGIN
    SELECT
        (SELECT COUNT(*) FROM Users WHERE Role = 'Student')                      AS TotalStudents,
        (SELECT COUNT(*) FROM Users WHERE Role = 'Professor')                    AS TotalProfessors,
        (SELECT COUNT(*) FROM Users WHERE Role = 'Admin')                        AS TotalAdmins,
        (SELECT COUNT(*) FROM Users WHERE UserStatus = 'Active')                 AS ActiveUsers,
        (SELECT COUNT(*) FROM Users)                                              AS TotalUsers,
        (SELECT COUNT(*) FROM Classrooms)                                        AS TotalClassrooms,
        (SELECT COUNT(*) FROM Challenges WHERE PublishedAtUtc IS NOT NULL)       AS TotalChallenges,
        (SELECT COUNT(*) FROM ChallengeSubmissions)                              AS TotalSubmissions,
        (SELECT COUNT(*) FROM ChallengeSubmissions WHERE Grade IS NULL)          AS PendingReviews;
END$$

DROP PROCEDURE IF EXISTS sp_GetUsers$$
CREATE PROCEDURE sp_GetUsers(IN p_search VARCHAR(256),
                              IN p_role   VARCHAR(32),
                              IN p_status VARCHAR(32))
BEGIN
    SELECT Id, Name, Email, Role, UserStatus, LastLoginAtUtc
    FROM Users
    WHERE (p_search = '' OR Name  LIKE CONCAT('%', p_search, '%')
                         OR Email LIKE CONCAT('%', p_search, '%'))
      AND (p_role   = '' OR Role       = p_role)
      AND (p_status = '' OR UserStatus = p_status)
    ORDER BY Name;
END$$

DROP PROCEDURE IF EXISTS sp_ToggleUserStatus$$
CREATE PROCEDURE sp_ToggleUserStatus(IN  p_user_id    INT,
                                     OUT p_new_status VARCHAR(32))
BEGIN
    SELECT UserStatus INTO p_new_status FROM Users WHERE Id = p_user_id LIMIT 1;
    SET p_new_status = IF(p_new_status = 'Active', 'Inactive', 'Active');
    UPDATE Users SET UserStatus = p_new_status WHERE Id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS sp_EditUserRole$$
CREATE PROCEDURE sp_EditUserRole(
    IN  p_user_id  INT,
    IN  p_new_role VARCHAR(32),
    OUT p_ha_erro  TINYINT(1),
    OUT p_mensagem VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = p_user_id) THEN
        SET p_ha_erro = 1; SET p_mensagem = 'Utilizador não encontrado.';
    ELSE
        UPDATE Users SET Role = p_new_role WHERE Id = p_user_id;
    END IF;
END$$

-- ============================================================
-- SEED DE DADOS DE DEMONSTRAÇÃO
-- ============================================================

DROP PROCEDURE IF EXISTS sp_SeedUser$$
CREATE PROCEDURE sp_SeedUser(
    IN  p_name          VARCHAR(120),
    IN  p_email         VARCHAR(256),
    IN  p_password_hash VARCHAR(512),
    IN  p_role          VARCHAR(32),
    IN  p_gender        VARCHAR(10),
    OUT p_id            INT)
BEGIN
    IF EXISTS (SELECT 1 FROM Users WHERE Email = p_email) THEN
        SELECT Id INTO p_id FROM Users WHERE Email = p_email LIMIT 1;
    ELSE
        INSERT INTO Users (Name, Email, PasswordHash, Role, Gender, UserStatus)
        VALUES (p_name, p_email, p_password_hash, p_role, NULLIF(p_gender,''), 'Active');
        SET p_id = LAST_INSERT_ID();
    END IF;
END$$

-- ============================================================
-- CENÁRIOS — listagem e dados rápidos
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetUserScenarios$$
CREATE PROCEDURE sp_GetUserScenarios(IN p_user_id INT)
BEGIN
    SELECT Id, Name, Location, MembersCount, InitialBalance,
           ProfileType, CreatedAtUtc, UpdatedAtUtc
    FROM Scenarios
    WHERE OwnerId = p_user_id
    ORDER BY UpdatedAtUtc DESC;
END$$

DROP PROCEDURE IF EXISTS sp_GetScenarioQuickData$$
CREATE PROCEDURE sp_GetScenarioQuickData(IN p_scenario_id INT, IN p_owner_id INT)
BEGIN
    SELECT
        COALESCE(SUM(CASE WHEN fe.EntryType = 'Income'  THEN fe.Amount ELSE 0 END), 0) AS rendimento_mensal,
        COALESCE(SUM(CASE WHEN fe.EntryType = 'Expense' THEN fe.Amount ELSE 0 END), 0) AS despesas_mensais,
        COALESCE(SUM(CASE WHEN fe.EntryType = 'Expense' AND fe.IsRecurring = 1 THEN fe.Amount ELSE 0 END), 0) AS encargos_fixos,
        COALESCE((SELECT SUM(sg.SavedAmount) FROM SavingsGoals sg WHERE sg.ScenarioId = p_scenario_id), 0) AS capital_poupanca
    FROM FinancialEntries fe
    WHERE fe.ScenarioId = p_scenario_id
      AND fe.ScenarioId IN (SELECT Id FROM Scenarios WHERE Id = p_scenario_id AND OwnerId = p_owner_id);
END$$

-- ============================================================
-- LANÇAMENTOS FINANCEIROS
-- ============================================================

DROP PROCEDURE IF EXISTS sp_GetScenarioEntries$$
CREATE PROCEDURE sp_GetScenarioEntries(IN p_scenario_id INT, IN p_owner_id INT, IN p_type_filter VARCHAR(16))
BEGIN
    SELECT fe.Id, fe.EntryType, fe.Category, fe.Amount, fe.Month, fe.Year, fe.IsRecurring
    FROM FinancialEntries fe
    INNER JOIN Scenarios s ON s.Id = fe.ScenarioId
    WHERE fe.ScenarioId = p_scenario_id
      AND s.OwnerId = p_owner_id
      AND (p_type_filter = '' OR fe.EntryType = p_type_filter)
    ORDER BY fe.Year DESC, fe.Month DESC, fe.Id DESC;
END$$

DROP PROCEDURE IF EXISTS sp_AddFinancialEntry$$
CREATE PROCEDURE sp_AddFinancialEntry(
    IN  p_scenario_id  INT,
    IN  p_owner_id     INT,
    IN  p_entry_type   VARCHAR(16),
    IN  p_category     VARCHAR(80),
    IN  p_amount       DECIMAL(12,2),
    IN  p_month        INT,
    IN  p_year         INT,
    IN  p_is_recurring TINYINT(1),
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Scenarios WHERE Id = p_scenario_id AND OwnerId = p_owner_id) THEN
        SET p_ha_erro = 1;
        SET p_mensagem = 'Cenário não encontrado ou sem permissão.';
    ELSE
        INSERT INTO FinancialEntries (ScenarioId, EntryType, Category, Amount, Month, Year, IsRecurring)
        VALUES (p_scenario_id, p_entry_type, p_category, p_amount, p_month, p_year, p_is_recurring);
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateFinancialEntry$$
CREATE PROCEDURE sp_UpdateFinancialEntry(
    IN  p_entry_id     INT,
    IN  p_owner_id     INT,
    IN  p_entry_type   VARCHAR(16),
    IN  p_category     VARCHAR(80),
    IN  p_amount       DECIMAL(12,2),
    IN  p_month        INT,
    IN  p_year         INT,
    IN  p_is_recurring TINYINT(1),
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (
        SELECT 1 FROM FinancialEntries fe
        INNER JOIN Scenarios s ON s.Id = fe.ScenarioId
        WHERE fe.Id = p_entry_id AND s.OwnerId = p_owner_id
    ) THEN
        SET p_ha_erro = 1;
        SET p_mensagem = 'Lançamento não encontrado ou sem permissão.';
    ELSE
        UPDATE FinancialEntries
        SET EntryType = p_entry_type, Category = p_category, Amount = p_amount,
            Month = p_month, Year = p_year, IsRecurring = p_is_recurring
        WHERE Id = p_entry_id;
    END IF;
END$$

DROP PROCEDURE IF EXISTS sp_DeleteFinancialEntry$$
CREATE PROCEDURE sp_DeleteFinancialEntry(
    IN  p_entry_id  INT,
    IN  p_owner_id  INT,
    OUT p_ha_erro   TINYINT(1),
    OUT p_mensagem  VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (
        SELECT 1 FROM FinancialEntries fe
        INNER JOIN Scenarios s ON s.Id = fe.ScenarioId
        WHERE fe.Id = p_entry_id AND s.OwnerId = p_owner_id
    ) THEN
        SET p_ha_erro = 1;
        SET p_mensagem = 'Lançamento não encontrado ou sem permissão.';
    ELSE
        DELETE FROM FinancialEntries WHERE Id = p_entry_id;
    END IF;
END$$
    
    
    
-- Perfil do aluno 
DROP PROCEDURE IF EXISTS sp_GetUserProfile$$
CREATE PROCEDURE sp_GetUserProfile(IN p_user_id INT)
BEGIN
    SELECT Id, Name, Email, Gender, BirthDate
    FROM Users
    WHERE Id = p_user_id
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS sp_UpdateUserProfile$$
CREATE PROCEDURE sp_UpdateUserProfile(
    IN  p_user_id       INT,
    IN  p_name          VARCHAR(120),
    IN  p_email         VARCHAR(256),
    IN  p_gender        VARCHAR(10),
    IN  p_birthdate     DATE,
    IN  p_password_hash VARCHAR(512),
    OUT p_ha_erro       TINYINT(1),
    OUT p_mensagem      VARCHAR(500))
BEGIN
    SET p_ha_erro = 0; SET p_mensagem = '';
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = p_user_id) THEN
        SET p_ha_erro = 1;
        SET p_mensagem = 'Utilizador não encontrado.';
    ELSEIF p_email != '' AND EXISTS (SELECT 1 FROM Users WHERE Email = p_email AND Id != p_user_id) THEN
        SET p_ha_erro = 2;
        SET p_mensagem = 'Este email já está em uso por outra conta.';
    ELSE
        UPDATE Users
        SET Name         = p_name,
            Email        = IF(p_email != '', p_email, Email),
            Gender       = NULLIF(p_gender, ''),
            BirthDate    = p_birthdate,
            PasswordHash = IF(p_password_hash != '', p_password_hash, PasswordHash)
        WHERE Id = p_user_id;
    END IF;
END$$

DELIMITER ;
