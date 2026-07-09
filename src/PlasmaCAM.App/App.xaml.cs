using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using PlasmaCAM.App.Logging;
using PlasmaCAM.App.Services;
using PlasmaCAM.Core.Abstractions;
using PlasmaCAM.Core.Services;
using PlasmaCAM.ViewModels;
using Serilog;

namespace PlasmaCAM.App;

public partial class App : Application
{
    private IHost? _host;
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>DI kontejner — koristi ga isključivo kompozicijski korijen (MainWindow, budući Views).</summary>
    public static IServiceProvider Services => ((App)Current)._host!.Services;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var messenger = WeakReferenceMessenger.Default;

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PlasmaCAM", "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "plasmacam-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .WriteTo.Sink(new ConsolePanelSink(messenger))
            .CreateLogger();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSerilog();

        // OnLaunched se izvršava na UI threadu — DispatcherQueue uhvaćen ovdje je ispravan.
        builder.Services.AddSingleton<IDispatcherService>(
            new DispatcherService(DispatcherQueue.GetForCurrentThread()));
        builder.Services.AddSingleton<IMessenger>(messenger);
        builder.Services.AddSingleton<IUndoService, UndoRedoService>();
        builder.Services.AddSingleton<ConsoleViewModel>();
        builder.Services.AddSingleton<MainViewModel>();

        _host = builder.Build();
        _host.Start();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation(
            "PlasmaCAM {Version} pokrenut (M0 skeleton).",
            typeof(App).Assembly.GetName().Version);

        _window = new MainWindow();
        _window.Closed += (_, _) =>
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
            Log.CloseAndFlush();
        };
        _window.Activate();
    }
}
