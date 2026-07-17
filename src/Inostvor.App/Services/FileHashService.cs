using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Project;

namespace Inostvor.App.Services;

/// <summary>Produkcijski hash datoteke — delegira na CacheKey.HashFile (ADR-006).</summary>
public sealed class FileHashService : IFileHashService
{
    public string HashFile(string path) => CacheKey.HashFile(path);
}
