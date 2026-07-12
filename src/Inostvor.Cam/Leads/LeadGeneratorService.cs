using Inostvor.Core.Model.Toolpath;
using Inostvor.Sdk.Cam;

namespace Inostvor.Cam.Leads;

/// <summary>
/// Dispečer lead strategija: bira registriranu strategiju po stilu. Nove
/// strategije (Tangential, Loop, Corner, PierceOnScrap — i buduće plugin
/// strategije) dodaju se REGISTRACIJOM, bez izmjene ovog servisa ili jezgre.
/// Nepoznat/neregistriran stil → bez leada (konzervativno, nikad pogrešan lead).
/// </summary>
public sealed class LeadGeneratorService
{
    private readonly Dictionary<LeadStyle, ILeadStrategy> _strategies;

    public LeadGeneratorService(IEnumerable<ILeadStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = [];
        foreach (var strategy in strategies)
        {
            _strategies[strategy.Style] = strategy; // zadnja registracija pobjeđuje (override plugin)
        }
    }

    public IReadOnlyList<CutMove> BuildLeadIn(LeadStyle style, LeadContext context)
        => style != LeadStyle.None && _strategies.TryGetValue(style, out var s) ? s.BuildLeadIn(context) : [];

    public IReadOnlyList<CutMove> BuildLeadOut(LeadStyle style, LeadContext context)
        => style != LeadStyle.None && _strategies.TryGetValue(style, out var s) ? s.BuildLeadOut(context) : [];
}
