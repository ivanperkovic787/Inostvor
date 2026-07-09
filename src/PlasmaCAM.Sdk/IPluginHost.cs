using Microsoft.Extensions.Logging;

namespace PlasmaCAM.Sdk;

/// <summary>
/// Ono što aplikacija nudi pluginu. Namjerno minimalan u M0 — proširuje se
/// s registracijskim točkama kako moduli budu donosili svoje kontrakte.
/// Svako proširenje je aditivno (bez breaking changea za postojeće plugine).
/// </summary>
public interface IPluginHost
{
    /// <summary>Logger namijenjen pluginu; poruke završavaju u log datoteci i Output Console panelu.</summary>
    ILogger Logger { get; }
}
