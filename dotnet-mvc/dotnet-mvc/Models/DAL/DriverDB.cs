using MySqlConnector;
using System.Data;

namespace dotnet_mvc.Models.DAL;

// Classe base para acesso à base de dados via stored procedures
// Todas as classes CRUD herdam desta para reutilizar a gestão de ligação.
public class DriverDB
{
    // Connection string definida uma vez no arranque da aplicação (Program.cs)
    public static string ConnectionString { get; set; } = "";

    // Executa uma SP que retorna dados e devolve o resultado num DataTable
    protected async Task<DataTable> GetSelectAsync(MySqlCommand cmd, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        cmd.Connection = conn;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var dt = new DataTable();
        for (int i = 0; i < reader.FieldCount; i++)
            dt.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
        while (await reader.ReadAsync(ct))
        {
            var row = dt.NewRow();
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            dt.Rows.Add(row);
        }
        return dt;
    }

    // Executa uma SP de INSERT/UPDATE/DELETE e devolve o número de linhas afetadas
    protected async Task<int> ExecuteNonQueryAsync(MySqlCommand cmd, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        cmd.Connection = conn;
        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
