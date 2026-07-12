using Inostvor.Sdk.Post;

namespace Inostvor.Post;

/// <summary>Katalog iz DI registracija; rezolucija po id-u (Ordinal).</summary>
public sealed class PostProcessorCatalog : IPostProcessorCatalog
{
    private readonly Dictionary<string, IPostProcessorPlugin> _byId;

    public PostProcessorCatalog(IEnumerable<IPostProcessorPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        Plugins = plugins.ToList();
        _byId = new Dictionary<string, IPostProcessorPlugin>(StringComparer.Ordinal);
        foreach (var plugin in Plugins)
        {
            _byId[plugin.Id] = plugin;
        }
    }

    public IReadOnlyList<IPostProcessorPlugin> Plugins { get; }

    public IPostProcessorPlugin? Find(string id)
        => id is not null && _byId.TryGetValue(id, out var plugin) ? plugin : null;
}
