using Npgsql;
using System.Data;
using System.Text;

public class DatabaseService
{
    private readonly string _connectionString;
    public DatabaseService(string connectionString) => _connectionString = connectionString;

    public string GetSchemaContext()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        var sb = new StringBuilder();
        string query = "SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_schema = 'public';";
        using var cmd = new NpgsqlCommand(query, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            sb.AppendLine($"Table: {reader["table_name"]}, Column: {reader["column_name"]}, Type: {reader["data_type"]}");
        return sb.ToString();
    }

    public string ExecuteQuery(string sql)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);
            var results = new List<string>();
            foreach (DataRow row in dt.Rows)
                results.Add(string.Join(" | ", row.ItemArray));
            return results.Count > 0 ? string.Join("\n", results) : "No records found.";
        }
        catch (Exception ex) { return $"SQL Error: {ex.Message}"; }
    }
}