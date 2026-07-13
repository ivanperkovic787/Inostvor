using System.Text.Json;
using Dapper;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Library;
using Inostvor.Core.Model.Machines;
using Inostvor.Data.Project;

namespace Inostvor.Data.Sqlite;

/// <summary>Profili strojeva kao JSON redci (shema se ne mijenja s poljima profila).</summary>
public sealed class MachineProfileRepository : IMachineProfileRepository
{
    private readonly SqliteDatabase _db;

    public MachineProfileRepository(SqliteDatabase db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public IReadOnlyList<MachineProfile> GetAll()
        => _db.Connection
            .Query<string>("SELECT json FROM machine_profiles ORDER BY name")
            .Select(json => JsonSerializer.Deserialize<MachineProfile>(json, ProjectJson.Options)!)
            .ToList();

    public void Save(MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _db.Connection.Execute(
            "INSERT INTO machine_profiles (id, name, json) VALUES (@Id, @Name, @Json) " +
            "ON CONFLICT(id) DO UPDATE SET name = @Name, json = @Json",
            new
            {
                Id = profile.Id.ToString("D"),
                profile.Name,
                Json = JsonSerializer.Serialize(profile, ProjectJson.Options),
            });
    }

    /// <summary>Brisanje po STABILNOM Id-u (ADR-006) — preimenovanje ne razbija referencu.</summary>
    public void Delete(Guid id)
        => _db.Connection.Execute("DELETE FROM machine_profiles WHERE id = @id", new { id = id.ToString("D") });
}

/// <summary>Biblioteka tehnologija.</summary>
public sealed class TechnologyRepository : ITechnologyRepository
{
    private readonly SqliteDatabase _db;

    public TechnologyRepository(SqliteDatabase db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public IReadOnlyList<TechnologyEntry> GetAll()
        => _db.Connection
            .Query<string>("SELECT json FROM technologies ORDER BY name")
            .Select(json => JsonSerializer.Deserialize<TechnologyEntry>(json, ProjectJson.Options)!)
            .ToList();

    public void Save(TechnologyEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _db.Connection.Execute(
            "INSERT INTO technologies (id, name, json) VALUES (@Id, @Name, @Json) " +
            "ON CONFLICT(id) DO UPDATE SET name = @Name, json = @Json",
            new
            {
                Id = entry.Id.ToString("D"),
                entry.Name,
                Json = JsonSerializer.Serialize(entry, ProjectJson.Options),
            });
    }

    public void Delete(Guid id)
        => _db.Connection.Execute("DELETE FROM technologies WHERE id = @id", new { id = id.ToString("D") });
}

/// <summary>Key-value postavke.</summary>
public sealed class SettingsRepository : ISettingsRepository
{
    private readonly SqliteDatabase _db;

    public SettingsRepository(SqliteDatabase db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public string? GetValue(string key)
        => _db.Connection.QuerySingleOrDefault<string>(
            "SELECT value FROM settings WHERE key = @key", new { key });

    public void SetValue(string key, string value)
        => _db.Connection.Execute(
            "INSERT INTO settings (key, value) VALUES (@key, @value) " +
            "ON CONFLICT(key) DO UPDATE SET value = @value",
            new { key, value });
}
