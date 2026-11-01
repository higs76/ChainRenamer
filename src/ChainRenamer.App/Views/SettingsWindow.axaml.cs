using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChainRenamer.App.Views;

/// <summary>The settings, saved as soon as they change. The library and the restart are the main window's business.</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();

    private MainWindow? Main => Owner as MainWindow;

    private void OnOpenLibrary(object? sender, RoutedEventArgs e) => Main?.OpenLibrary();

    private void OnMoveLibrary(object? sender, RoutedEventArgs e) => Main?.MoveLibrary(this);

    private void OnRestart(object? sender, RoutedEventArgs e)
    {
        var main = Main;
        Close();
        main?.Restart();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
