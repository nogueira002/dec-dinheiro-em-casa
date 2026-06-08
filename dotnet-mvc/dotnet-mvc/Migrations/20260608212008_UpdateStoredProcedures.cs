using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_mvc.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recriar sp_AddFinancialEntry com OUT params para deteção de erro (padrão do professor)
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_AddFinancialEntry", suppressTransaction: true);
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_AddFinancialEntry(
    IN  p_scenario_id  INT,
    IN  p_owner_id     INT,
    IN  p_entry_type   VARCHAR(16),
    IN  p_category     VARCHAR(255),
    IN  p_amount       DECIMAL(18,2),
    IN  p_month        INT,
    IN  p_year         INT,
    IN  p_is_recurring TINYINT(1),
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro  = 0;
    SET p_mensagem = '';

    IF NOT EXISTS (SELECT 1 FROM Scenarios WHERE Id = p_scenario_id AND OwnerId = p_owner_id) THEN
        SET p_ha_erro  = 1;
        SET p_mensagem = 'Cenário não encontrado ou acesso negado';
    ELSE
        INSERT INTO FinancialEntries
            (ScenarioId, EntryType, Category, Amount, Month, Year, IsRecurring, CreatedAtUtc)
        VALUES
            (p_scenario_id, p_entry_type, p_category, p_amount,
             p_month, p_year, p_is_recurring, UTC_TIMESTAMP());
    END IF;
END", suppressTransaction: true);

            // Recriar sp_UpdateFinancialEntry com OUT params para deteção de erro
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateFinancialEntry", suppressTransaction: true);
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_UpdateFinancialEntry(
    IN  p_entry_id     INT,
    IN  p_owner_id     INT,
    IN  p_entry_type   VARCHAR(16),
    IN  p_category     VARCHAR(255),
    IN  p_amount       DECIMAL(18,2),
    IN  p_month        INT,
    IN  p_year         INT,
    IN  p_is_recurring TINYINT(1),
    OUT p_ha_erro      TINYINT(1),
    OUT p_mensagem     VARCHAR(500))
BEGIN
    SET p_ha_erro  = 0;
    SET p_mensagem = '';

    IF NOT EXISTS (
        SELECT 1 FROM FinancialEntries fe
        INNER JOIN Scenarios s ON fe.ScenarioId = s.Id
        WHERE fe.Id = p_entry_id AND s.OwnerId = p_owner_id) THEN
        SET p_ha_erro  = 1;
        SET p_mensagem = 'Entrada não encontrada ou acesso negado';
    ELSE
        UPDATE FinancialEntries fe
        INNER JOIN Scenarios s ON fe.ScenarioId = s.Id AND s.OwnerId = p_owner_id
        SET fe.EntryType   = p_entry_type,
            fe.Category    = p_category,
            fe.Amount      = p_amount,
            fe.Month       = p_month,
            fe.Year        = p_year,
            fe.IsRecurring = p_is_recurring
        WHERE fe.Id = p_entry_id;
    END IF;
END", suppressTransaction: true);

            // Recriar sp_DeleteFinancialEntry com OUT params para deteção de erro
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_DeleteFinancialEntry", suppressTransaction: true);
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_DeleteFinancialEntry(
    IN  p_entry_id  INT,
    IN  p_owner_id  INT,
    OUT p_ha_erro   TINYINT(1),
    OUT p_mensagem  VARCHAR(500))
BEGIN
    SET p_ha_erro  = 0;
    SET p_mensagem = '';

    IF NOT EXISTS (
        SELECT 1 FROM FinancialEntries fe
        INNER JOIN Scenarios s ON fe.ScenarioId = s.Id
        WHERE fe.Id = p_entry_id AND s.OwnerId = p_owner_id) THEN
        SET p_ha_erro  = 1;
        SET p_mensagem = 'Entrada não encontrada ou acesso negado';
    ELSE
        DELETE fe FROM FinancialEntries fe
        INNER JOIN Scenarios s ON fe.ScenarioId = s.Id AND s.OwnerId = p_owner_id
        WHERE fe.Id = p_entry_id;
    END IF;
END", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaurar versões anteriores sem OUT params
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_AddFinancialEntry", suppressTransaction: true);
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_AddFinancialEntry(
    IN p_scenario_id  INT,
    IN p_owner_id     INT,
    IN p_entry_type   VARCHAR(16),
    IN p_category     VARCHAR(255),
    IN p_amount       DECIMAL(18,2),
    IN p_month        INT,
    IN p_year         INT,
    IN p_is_recurring TINYINT(1))
BEGIN
    INSERT INTO FinancialEntries
        (ScenarioId, EntryType, Category, Amount, Month, Year, IsRecurring, CreatedAtUtc)
    SELECT p_scenario_id, p_entry_type, p_category, p_amount,
           p_month, p_year, p_is_recurring, UTC_TIMESTAMP()
    FROM Scenarios
    WHERE Id = p_scenario_id AND OwnerId = p_owner_id
    LIMIT 1;
END", suppressTransaction: true);

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateFinancialEntry", suppressTransaction: true);
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_UpdateFinancialEntry(
    IN p_entry_id     INT,
    IN p_owner_id     INT,
    IN p_entry_type   VARCHAR(16),
    IN p_category     VARCHAR(255),
    IN p_amount       DECIMAL(18,2),
    IN p_month        INT,
    IN p_year         INT,
    IN p_is_recurring TINYINT(1))
BEGIN
    UPDATE FinancialEntries fe
    INNER JOIN Scenarios s ON fe.ScenarioId = s.Id AND s.OwnerId = p_owner_id
    SET fe.EntryType   = p_entry_type,
        fe.Category    = p_category,
        fe.Amount      = p_amount,
        fe.Month       = p_month,
        fe.Year        = p_year,
        fe.IsRecurring = p_is_recurring
    WHERE fe.Id = p_entry_id;
END", suppressTransaction: true);

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_DeleteFinancialEntry", suppressTransaction: true);
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_DeleteFinancialEntry(
    IN p_entry_id INT,
    IN p_owner_id INT)
BEGIN
    DELETE fe FROM FinancialEntries fe
    INNER JOIN Scenarios s ON fe.ScenarioId = s.Id AND s.OwnerId = p_owner_id
    WHERE fe.Id = p_entry_id;
END", suppressTransaction: true);
        }
    }
}
