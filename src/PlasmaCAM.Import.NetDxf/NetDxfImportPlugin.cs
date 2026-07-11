using Microsoft.Extensions.Logging;
using PlasmaCAM.Core.Abstractions;
using PlasmaCAM.Core.Model.Import;
using PlasmaCAM.Sdk;
using PlasmaCAM.Sdk.Import;

namespace PlasmaCAM.Import.NetDxf;

/// <summary>Registracija netDxf importera kroz plugin kontrakt (isti put kao budući vanjski plugini).</summary>
public sealed class NetDxfImportPlugin : IImportPlugin
{
    private ILogger? _logger;

    public string Id => "plasmacam.import.netdxf";

    public string Name => "DXF import (netDxf)";

    public Version Version => new(1, 0, 0);

    public IReadOnlyList<string> FileExtensions { get; } = [".dxf"];

    public void Initialize(IPluginHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _logger = host.Logger;
        _logger.LogInformation("Import plugin registriran: {Name} ({Id})", Name, Id);
    }

    public IDxfImporter CreateImporter() => new NetDxfImporter(ImportSettings.Default);
}
