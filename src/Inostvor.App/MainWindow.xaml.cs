using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Inostvor.ViewModels;

namespace Inostvor.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        Console = App.Services.GetRequiredService<ConsoleViewModel>();

        InitializeComponent();

        Title = "Inostvor";
        SystemBackdrop = new MicaBackdrop();
    }

    public MainViewModel ViewModel { get; }

    public ConsoleViewModel Console { get; }
}
