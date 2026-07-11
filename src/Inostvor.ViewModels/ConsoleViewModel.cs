using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Inostvor.Core.Abstractions;
using Inostvor.ViewModels.Messages;

namespace Inostvor.ViewModels;

/// <summary>
/// Output Console panel: prima <see cref="LogMessage"/> poruke preko Messengera
/// (Serilog sink ih šalje s bilo kojeg threada) i marshalira ih na UI thread.
/// Broj redaka je ograničen da dugotrajni rad ne curi memoriju.
/// </summary>
public sealed partial class ConsoleViewModel : ObservableObject, IRecipient<LogMessage>
{
    private const int MaxLines = 2000;

    private readonly IDispatcherService _dispatcher;

    public ConsoleViewModel(IDispatcherService dispatcher, IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(messenger);

        _dispatcher = dispatcher;
        messenger.Register(this);
    }

    public ObservableCollection<LogLine> Lines { get; } = [];

    public void Receive(LogMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _dispatcher.Enqueue(() =>
        {
            Lines.Add(new LogLine(
                message.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                message.Level,
                message.Text));

            if (Lines.Count > MaxLines)
            {
                Lines.RemoveAt(0);
            }
        });
    }

    [RelayCommand]
    private void Clear() => Lines.Clear();
}
