using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using PlasmaCAM.ViewModels;

namespace PlasmaCAM.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        Console = App.Services.GetRequiredService<ConsoleViewModel>();

        InitializeComponent();

        Title = "PlasmaCAM";
        SystemBackdrop = new MicaBackdrop();
    }

    public MainViewModel ViewModel { get; }

    public ConsoleViewModel Console { get; }
}
