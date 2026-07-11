namespace Inostvor.ViewModels.Messages;

/// <summary>
/// Poruka koju Serilog sink (u App sloju) šalje kroz Messenger prema Output Console panelu.
/// ViewModels sloj definira kontrakt, App sloj ga puni — smjer ovisnosti ostaje ispravan.
/// </summary>
public sealed record LogMessage(DateTimeOffset Timestamp, string Level, string Text);
