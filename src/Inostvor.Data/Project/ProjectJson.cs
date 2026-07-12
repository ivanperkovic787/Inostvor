using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inostvor.Data.Project;

/// <summary>
/// Jedine JSON opcije za format projekta — FIKSIRANE (enumi kao stringovi,
/// uvlačenje, ignoriranje null): stabilnost formata kroz godine je ugovor.
/// </summary>
internal static class ProjectJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };
}
