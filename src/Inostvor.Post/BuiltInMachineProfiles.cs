using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;

namespace Inostvor.Post;

/// <summary>
/// Ugrađeni profili strojeva — demonstracija odvajanja pojmova: JEDAN dijalekt,
/// VIŠE strojeva. UC300ETH namjerno NIJE poseban postprocesor nego Mach3 profil
/// (ADR-004: kontroler govori čisti Mach3 dijalekt). M8 donosi persistenciju i
/// korisničko uređivanje profila.
/// </summary>
public static class BuiltInMachineProfiles
{
    public static MachineProfile Mach3Plasma { get; } = new()
    {
        Name = "Mach3 Plasma",
        PostProcessorId = "inostvor.post.mach3",
        Process = CutProcess.Plasma,
    };

    public static MachineProfile Mach3Router { get; } = new()
    {
        Name = "Mach3 Router",
        PostProcessorId = "inostvor.post.mach3",
        Process = CutProcess.Router,
        SafeZ = 20.0,
        PierceHeight = 5.0,
        CutHeight = -3.0, // dubina prolaza
        DefaultTechnology = TechnologySettings.Default with
        {
            Process = CutProcess.Router,
            KerfWidth = 6.0,     // promjer glodala
            PierceTime = 2.0,    // spin-up vretena
            FeedRate = 1200.0,
        },
    };

    public static MachineProfile Ec300Plasma { get; } = new()
    {
        Name = "EC300 Plasma (900x1200)",
        PostProcessorId = "inostvor.post.ec300",
        Process = CutProcess.Plasma,
        TableWidth = 900,
        TableHeight = 1200,
    };

    public static MachineProfile Uc300EthPlasma { get; } = new()
    {
        Name = "UC300ETH Plasma",
        PostProcessorId = "inostvor.post.mach3", // čisti Mach3 dijalekt — nije poseban post!
        Process = CutProcess.Plasma,
    };

    public static IReadOnlyList<MachineProfile> All { get; } =
        [Mach3Plasma, Mach3Router, Ec300Plasma, Uc300EthPlasma];
}
