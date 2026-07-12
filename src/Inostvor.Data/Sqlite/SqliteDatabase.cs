using Microsoft.Data.Sqlite;

namespace Inostvor.Data.Sqlite;

/// <summary>
/// Jedna otvorena SQLite konekcija + shema (ADR-005: SQLite je APLIKACIJSKA
/// razina — biblioteke profila/tehnologija, postavke, nedavni projekti;
/// korisnikov PROJEKT nikad ne živi samo ovdje). Jedna trajna konekcija
/// podržava i ":memory:" u testovima.
/// </summary>
public sealed class SqliteDatabase : IDisposable
{
    public SqliteDatabase(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        Connection = new SqliteConnection(connectionString);
        Connection.Open();
        EnsureSchema();
    }

    public SqliteConnection Connection { get; }

    private void EnsureSchema()
    {
        using var command = Connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_version (version INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS machine_profiles (
                name TEXT PRIMARY KEY,
                json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS technologies (
                id   TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS recent_projects (
                path      TEXT PRIMARY KEY,
                opened_at TEXT NOT NULL);
            INSERT INTO schema_version (version)
                SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM schema_version);
            """;
        command.ExecuteNonQuery();
    }

    public void Dispose() => Connection.Dispose();
}
