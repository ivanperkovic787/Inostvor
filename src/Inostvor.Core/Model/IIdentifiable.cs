namespace Inostvor.Core.Model;

/// <summary>
/// Trajni objekt sa STABILNIM identitetom (ADR-006). Id se dodjeljuje pri
/// stvaranju i NIKAD se ne mijenja — preimenovanje, uređivanje ili kopiranje
/// između računala ne smiju razbiti reference. Sve trajne reference (projekt →
/// profil, projekt → tehnologija) idu preko Id-a, ne imena.
/// </summary>
public interface IIdentifiable
{
    Guid Id { get; }
}
