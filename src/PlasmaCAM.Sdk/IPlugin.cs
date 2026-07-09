namespace PlasmaCAM.Sdk;

/// <summary>
/// Bazni kontrakt svakog plugina. Specijalizirana sučelja (IImportPlugin u M2,
/// IToolpathPlugin u M5, IPostProcessorPlugin u M7, IValidationRule u M3) dolaze
/// s modulom koji ih prvi konzumira — ugrađene implementacije registriraju se
/// kroz ISTE kontrakte kao budući vanjski plugini (Baseline v1.1, §4.5).
/// </summary>
public interface IPlugin
{
    /// <summary>Globalno jedinstven, stabilan identifikator (npr. "plasmacam.import.netdxf").</summary>
    string Id { get; }

    /// <summary>Naziv prikazan korisniku.</summary>
    string Name { get; }

    Version Version { get; }

    /// <summary>Poziva se jednom pri učitavanju; plugin ovdje dobiva pristup servisima hosta.</summary>
    void Initialize(IPluginHost host);
}
