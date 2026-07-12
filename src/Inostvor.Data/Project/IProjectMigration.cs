using System.Text.Json.Nodes;

namespace Inostvor.Data.Project;

/// <summary>
/// Jedan korak migracije formata projekta: JSON verzije <see cref="FromVersion"/>
/// → JSON verzije FromVersion+1. Migracije rade NAD JSON-om (ne nad tipovima) —
/// stari format ne mora imati živuće C# tipove da bi se otvorio. Kompatibilnost
/// se NIKAD ne razbija: lanac migracija samo raste.
/// </summary>
public interface IProjectMigration
{
    int FromVersion { get; }

    JsonNode Migrate(JsonNode projectJson);
}
