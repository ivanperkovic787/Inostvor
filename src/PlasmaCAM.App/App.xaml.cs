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
using PlasmaCAM.Import.NetDxf;
using PlasmaCAM.Sdk;
using PlasmaCAM.Sdk.Import;
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

        // Import (M2): ugrađeni netDxf importer registrira se kroz plugin kontrakt —
        // isti put kojim će ići vanjski plugini (Baseline v1.1, §4.5).
        builder.Services.AddSingleton<IPluginHost, PluginHost>();
        builder.Services.AddSingleton<IImportPlugin, NetDxfImportPlugin>();
        builder.Services.AddSingleton<IDxfImporter>(sp => sp.GetRequiredService<IImportPlugin>().CreateImporter());
        builder.Services.AddSingleton<IFilePickerService>(
            new FilePickerService(() => WinRT.Interop.WindowNative.GetWindowHandle(((App)Current)._window!)));

        builder.Services.AddSingleton<ConsoleViewModel>();
        builder.Services.AddSingleton<MainViewModel>();

        _host = builder.Build();
        _host.Start();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation(
            "PlasmaCAM {Version} pokrenut.",
            typeof(App).Assembly.GetName().Version);

        // Inicijalizacija pluginova (za sada samo ugrađeni import plugin).
        var pluginHost = _host.Services.GetRequiredService<IPluginHost>();
        foreach (var plugin in _host.Services.GetServices<IImportPlugin>())
        {
            plugin.Initialize(pluginHost);
        }

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
