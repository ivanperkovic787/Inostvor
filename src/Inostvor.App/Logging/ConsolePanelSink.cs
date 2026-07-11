using CommunityToolkit.Mvvm.Messaging;
using Inostvor.ViewModels.Messages;
using Serilog.Core;
using Serilog.Events;

namespace Inostvor.App.Logging;

/// <summary>
/// Serilog sink koji svaku log poruku prosljeđuje Messengerom u Output Console panel.
/// Emit se može dogoditi na bilo kojem threadu — marshaliranje na UI thread radi ConsoleViewModel.
/// </summary>
public sealed class ConsolePanelSink : ILogEventSink
{
    private readonly IMessenger _messenger;

    public ConsolePanelSink(IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(messenger);
        _messenger = messenger;
    }

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        _messenger.Send(new LogMessage(
            logEvent.Timestamp,
            logEvent.Level.ToString().ToUpperInvariant(),
            logEvent.RenderMessage()));
    }
}
