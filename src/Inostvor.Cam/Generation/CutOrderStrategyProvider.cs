using Inostvor.Core.Abstractions;

namespace Inostvor.Cam.Generation;

/// <summary>
/// Rezolucija strategije po Id-u iz DI registracija. Nepoznat Id → "bottom-to-top"
/// (konzervativni default), nikad iznimka — posao se uvijek može izrezati.
/// </summary>
public sealed class CutOrderStrategyProvider : ICutOrderStrategyProvider
{
    private readonly Dictionary<string, ICutOrderStrategy> _strategies;
    private readonly ICutOrderStrategy _fallback;

    public CutOrderStrategyProvider(IEnumerable<ICutOrderStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = new Dictionary<string, ICutOrderStrategy>(StringComparer.Ordinal);
        foreach (var strategy in strategies)
        {
            _strategies[strategy.Id] = strategy;
        }

        _fallback = _strategies.TryGetValue("bottom-to-top", out var fallback)
            ? fallback
            : _strategies.Values.First();
    }

    public ICutOrderStrategy Resolve(string strategyId)
        => strategyId is not null && _strategies.TryGetValue(strategyId, out var strategy) ? strategy : _fallback;
}
