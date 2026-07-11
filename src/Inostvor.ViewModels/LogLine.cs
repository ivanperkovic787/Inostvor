namespace Inostvor.ViewModels;

/// <summary>Jedan formatirani redak u Output Console panelu.</summary>
public sealed record LogLine(string Timestamp, string Level, string Text);
