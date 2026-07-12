using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Inostvor.App.Logging;
using Inostvor.App.Services;
using Inostvor.Core.Abstractions;
using Inostvor.Cam.Fitting;
using Inostvor.Cam.Generation;
using Inostvor.Cam.Leads;
using Inostvor.Cam.Offset;
using Inostvor.Core.Services;
using Inostvor.Geometry.Contours;
using Inostvor.Geometry.Rules;
using Inostvor.Geometry.Validation;
using Inostvor.Import.NetDxf;
using Inostvor.Post;
using Inostvor.Post.Plugins;
using Inostvor.Sdk;
using Inostvor.Sdk.Import;
using Inostvor.Sdk.Cam;
using Inostvor.Sdk.Post;
using Inostvor.Sdk.Validation;
using Inostvor.ViewModels;
using Serilog;

namespace Inostvor.App;

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
            "Inostvor", "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "inostvor-.log"),
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

        // Geometrija (M3): detekcija kontura, klasifikacija, validacija.
        // Ugrađena pravila registriraju se kroz isti IValidationRule kontrakt kao buduća plugin pravila.
        builder.Services.AddSingleton<IContourBuilder, ContourBuilder>();
        builder.Services.AddSingleton<IContourClassifier, ContourClassifier>();
        builder.Services.AddSingleton<IValidationRule, OpenContourRule>();
        builder.Services.AddSingleton<IValidationRule, JoinedGapsRule>();
        builder.Services.AddSingleton<IValidationRule, SelfIntersectionRule>();
        builder.Services.AddSingleton<IValidationRule, DuplicateGeometryRule>();
        builder.Services.AddSingleton<IValidationRule>(_ => new ZeroLengthSegmentRule());
        builder.Services.AddSingleton<IToolpathValidator, ToolpathValidator>();
        builder.Services.AddSingleton<IGeometryPipeline, GeometryPipeline>();

        // CAM (M5): modularne operacije kroz sučelja — svaka zamjenjiva bez izmjene jezgre.
        builder.Services.AddSingleton<IKerfOffsetService, KerfOffsetService>();
        builder.Services.AddSingleton<IArcFitter, ArcFitter>();
        builder.Services.AddSingleton<ILeadStrategy, LineLeadStrategy>();
        builder.Services.AddSingleton<ILeadStrategy, ArcLeadStrategy>();
        builder.Services.AddSingleton<LeadGeneratorService>();
        builder.Services.AddSingleton<IOvercutService, OvercutService>();
        builder.Services.AddSingleton<ICutOrderStrategy, DefaultCutOrderStrategy>();
        builder.Services.AddSingleton<ICutOrderStrategy, NearestNeighborCutOrderStrategy>();
        builder.Services.AddSingleton<ICutOrderStrategyProvider, CutOrderStrategyProvider>();
        builder.Services.AddSingleton<IToolpathGenerator, ToolpathGenerator>();

        // Postprocesori (M7, ADR-004): SVI kao ravnopravni pluginovi kroz IPostProcessorPlugin.
        builder.Services.AddSingleton<IPostProcessorPlugin, Mach3PostPlugin>();
        builder.Services.AddSingleton<IPostProcessorPlugin, Ec300PostPlugin>();
        builder.Services.AddSingleton<IPostProcessorCatalog, PostProcessorCatalog>();
        builder.Services.AddSingleton<IFileSaveService>(
            new FileSaveService(() => WinRT.Interop.WindowNative.GetWindowHandle(((App)Current)._window!)));

        builder.Services.AddSingleton<ConsoleViewModel>();
        builder.Services.AddSingleton<MainViewModel>();

        _host = builder.Build();
        _host.Start();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation(
            "Inostvor {Version} pokrenut.",
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
