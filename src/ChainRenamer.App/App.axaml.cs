using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChainRenamer.App.ViewModels;
using ChainRenamer.App.Views;

namespace ChainRenamer.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            // A folder given on the command line, or by the Explorer menu ("Rename with ChainRenamer"), is loaded at once.
            if (desktop.Args?.FirstOrDefault(Directory.Exists) is { } folder) vm.OpenFolder(folder);
        }

        base.OnFrameworkInitializationCompleted();
    }
}