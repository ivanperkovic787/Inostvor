using Microsoft.Extensions.Logging;
using PlasmaCAM.Sdk;

namespace PlasmaCAM.App.Services;

/// <summary>Konkretni host koji aplikacija predaje pluginima pri inicijalizaciji.</summary>
public sealed class PluginHost : IPluginHost
{
    public PluginHost(ILogger<PluginHost> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
    }

    public ILogger Logger { get; }
}
