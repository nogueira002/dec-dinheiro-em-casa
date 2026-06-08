using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_mvc.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SP 1 — lista cenários do utilizador
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_GetUserScenarios(IN p_user_id INT)
BEGIN
    SELECT Id, OwnerId, Name, Location, MembersCount, InitialBalance, ProfileType,
           CreatedAtUtc, UpdatedAtUtc
    FROM Scenarios
    WHERE OwnerId = p_user_id
    ORDER BY UpdatedAtUtc DESC, CreatedAtUtc DESC;
END", suppressTransaction: true);

            // SP 2 — lançamentos de um cenário com filtro opcional de tipo
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_GetScenarioEntries(
    IN p_scenario_id INT,
    IN p_owner_id    INT,
    IN p_type_filter VARCHAR(16))
BEGIN
    SELECT fe.Id, fe.ScenarioId, fe.EntryType, fe.Category,
           fe.Amount, fe.Month, fe.Year, fe.IsRecurring, fe.CreatedAtUtc
    FROM FinancialEntries fe
    INNER JOIN Scenarios s ON fe.ScenarioId = s.Id AND s.OwnerId = p_owner_id
    WHERE fe.ScenarioId = p_scenario_id
      AND (p_type_filter IS NULL OR p_type_filter = '' OR fe.EntryType = p_type_filter)
    ORDER BY fe.Year DESC, fe.Month DESC, fe.CreatedAtUtc DESC;
END", suppressTransaction: true);

            // SP 3 — adicionar lançamento (só insere se o utilizador for dono do cenário)
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

            // SP 4 — editar lançamento (só atualiza se o utilizador for dono do cenário)
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

            // SP 5 — apagar lançamento (só apaga se o utilizador for dono do cenário)
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_DeleteFinancialEntry(
    IN p_entry_id INT,
    IN p_owner_id INT)
BEGIN
    DELETE fe FROM FinancialEntries fe
    INNER JOIN Scenarios s ON fe.ScenarioId = s.Id AND s.OwnerId = p_owner_id
    WHERE fe.Id = p_entry_id;
END", suppressTransaction: true);

            // SP 6 — dados rápidos de um cenário (médias mensais + capital poupado)
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_GetScenarioQuickData(
    IN p_scenario_id INT,
    IN p_owner_id    INT)
BEGIN
    DECLARE v_initial_balance DECIMAL(18,2) DEFAULT 0;
    DECLARE v_total_saved     DECIMAL(18,2) DEFAULT 0;
    DECLARE v_capital         DECIMAL(18,2) DEFAULT 0;
    DECLARE v_exists          INT           DEFAULT 0;

    SELECT COUNT(*), COALESCE(MAX(InitialBalance), 0)
    INTO v_exists, v_initial_balance
    FROM Scenarios
    WHERE Id = p_scenario_id AND OwnerId = p_owner_id;

    IF v_exists > 0 THEN
        SELECT COALESCE(SUM(SavedAmount), 0)
        INTO v_total_saved
        FROM SavingsGoals
        WHERE ScenarioId = p_scenario_id;

        SET v_capital = IF(v_total_saved > 0, v_total_saved, v_initial_balance);

        SELECT
            COALESCE(
                (SELECT AVG(s) FROM
                    (SELECT SUM(Amount) AS s FROM FinancialEntries
                     WHERE ScenarioId = p_scenario_id AND EntryType = 'Income'
                     GROUP BY Year, Month) t),
                0) AS rendimento_mensal,
            COALESCE(
                (SELECT AVG(s) FROM
                    (SELECT SUM(Amount) AS s FROM FinancialEntries
                     WHERE ScenarioId = p_scenario_id AND EntryType = 'Expense' AND IsRecurring = 0
                     GROUP BY Year, Month) t),
                0) AS despesas_mensais,
            COALESCE(
                (SELECT AVG(s) FROM
                    (SELECT SUM(Amount) AS s FROM FinancialEntries
                     WHERE ScenarioId = p_scenario_id AND EntryType = 'Expense' AND IsRecurring = 1
                     GROUP BY Year, Month) t),
                0) AS encargos_fixos,
            v_capital AS capital_poupanca;
    END IF;
END", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetScenarioQuickData",  suppressTransaction: true);
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_DeleteFinancialEntry",  suppressTransaction: true);
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateFinancialEntry",  suppressTransaction: true);
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_AddFinancialEntry",     suppressTransaction: true);
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetScenarioEntries",    suppressTransaction: true);
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetUserScenarios",      suppressTransaction: true);
        }
    }
}
