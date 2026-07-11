using Microsoft.Extensions.Logging;
using Inostvor.Sdk;

namespace Inostvor.App.Services;

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
