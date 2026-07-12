using System.Globalization;
using System.Text.Json;
using Inostvor.Core.Abstractions;
using Inostvor.Core.Model.Library;
using Inostvor.Core.Model.Machines;
using Inostvor.Data.Project;

namespace Inostvor.Data;

/// <summary>Bundle za prijenos među računalima: profili + tehnologije u jednoj JSON datoteci.</summary>
public sealed record SettingsBundle(
    int BundleVersion,
    IReadOnlyList<MachineProfile> MachineProfiles,
    IReadOnlyList<TechnologyEntry> Technologies);

/// <summary>Izvoz/uvoz postavki; uvoz zamjenjuje zapise istog identiteta (ime/Id).</summary>
public sealed class SettingsPortService : ISettingsPortService
{
    private readonly IMachineProfileRepository _machines;
    private readonly ITechnologyRepository _technologies;

    public SettingsPortService(IMachineProfileRepository machines, ITechnologyRepository technologies)
    {
        ArgumentNullException.ThrowIfNull(machines);
        ArgumentNullException.ThrowIfNull(technologies);
        _machines = machines;
        _technologies = technologies;
    }

    public string ExportToJson()
    {
        var bundle = new SettingsBundle(1, _machines.GetAll(), _technologies.GetAll());
        return JsonSerializer.Serialize(bundle, ProjectJson.Options);
    }

    public string ImportFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var bundle = JsonSerializer.Deserialize<SettingsBundle>(json, ProjectJson.Options)
            ?? throw new InvalidDataException("Neispravan bundle postavki.");

        foreach (var profile in bundle.MachineProfiles)
        {
            _machines.Save(profile);
        }

        foreach (var technology in bundle.Technologies)
        {
            _technologies.Save(technology);
        }

        return string.Format(CultureInfo.InvariantCulture,
            "Uvezeno: {0} profila strojeva, {1} tehnologija.",
            bundle.MachineProfiles.Count, bundle.Technologies.Count);
    }
}
