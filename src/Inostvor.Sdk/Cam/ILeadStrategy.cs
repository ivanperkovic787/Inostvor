using Inostvor.Core.Model.Geometry;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Sdk.Cam;

/// <summary>Sve što lead strategija smije znati o mjestu ulaska/izlaska.</summary>
/// <param name="AttachPoint">Točka na offsetiranoj putanji gdje lead prianja.</param>
/// <param name="Tangent">Jedinična tangenta putanje u AttachPointu (smjer rezanja).</param>
/// <param name="InwardNormal">Jedinična normala prema materijalu koji OSTAJE (dobra strana).</param>
/// <param name="Contour">Izvorna kontura (vrsta, granice — npr. za PierceOnScrap).</param>
/// <param name="Length">Tražena duljina/polumjer leada. [mm]</param>
/// <param name="FeedRate">Posmak leada. [mm/min]</param>
public sealed record LeadContext(
    Point2 AttachPoint,
    Vector2 Tangent,
    Vector2 InwardNormal,
    Contour Contour,
    double Length,
    double FeedRate);

/// <summary>
/// Jedna strategija leada (Line, Arc, Tangential, Loop, Corner, PierceOnScrap…).
/// Otvoren skup kroz Sdk — nove strategije (uklj. buduće plugine) dodaju se
/// registracijom, bez izmjene CAM jezgre. Lead-in završava TOČNO u AttachPointu;
/// lead-out počinje TOČNO u njemu.
/// </summary>
public interface ILeadStrategy
{
    LeadStyle Style { get; }

    /// <summary>Potezi lead-ina; prvi potez počinje u pierce točki, zadnji završava u AttachPointu.</summary>
    IReadOnlyList<CutMove> BuildLeadIn(LeadContext context);

    /// <summary>Potezi lead-outa; prvi potez počinje u AttachPointu.</summary>
    IReadOnlyList<CutMove> BuildLeadOut(LeadContext context);
}
