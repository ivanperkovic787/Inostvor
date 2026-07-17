using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Core.Model.Project;

/// <summary>
/// Računanje ključa valjanosti cachea (ADR-006). Cache je valjan ISKLJUČIVO ako
/// se hash SVIH ulaza podudara i ako je PipelineVersion identičan.
/// </summary>
public static class CacheKey
{
    /// <summary>
    /// Verzija CAM cjevovoda — POVEĆATI pri svakoj promjeni koja može promijeniti
    /// izlaz (kerf, arc fitting, leadovi, redoslijed, IR). Time se svi postojeći
    /// cachevi automatski odbacuju; NIJE potrebna migracija projekata.
    /// </summary>
    public const int PipelineVersion = 2; // v2: ArcFitter sagitta provjera (luk između rijetkih točaka)

    /// <summary>SHA-256 datoteke (hex, mala slova).</summary>
    public static string HashFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    /// <summary>
    /// Hash svih ulaza: hashevi DXF izvora (redoslijedom u projektu) + tehnologija.
    /// Tehnologija ulazi kao njena kanonska tekstualna reprezentacija (record →
    /// deterministički ToString je nepouzdan, pa se polja ispisuju eksplicitno).
    /// </summary>
    public static string ComputeInputHash(IReadOnlyList<ProjectDxfSource> sources, TechnologySettings technology)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(technology);

        var builder = new StringBuilder();
        foreach (var source in sources)
        {
            builder.Append(source.FileName).Append('|').Append(source.Sha256).Append('\n');
        }

        builder.Append(Canonical(technology));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private static string Canonical(TechnologySettings t) => string.Create(CultureInfo.InvariantCulture,
        $"""
         process={t.Process}
         kerf={t.KerfWidth:R}
         feed={t.FeedRate:R}
         rapid={t.RapidRate:R}
         pierce={t.PierceTime:R}
         leadIn={t.LeadInStyle}:{t.LeadInLength:R}
         leadOut={t.LeadOutStyle}:{t.LeadOutLength:R}
         overcut={t.OvercutLength:R}
         arcFit={t.EnableArcFitting}:{t.ArcFittingTolerance:R}
         offsetTol={t.OffsetTessellationTolerance:R}
         cutOrder={t.CutOrderStrategyId}
         """);
}
