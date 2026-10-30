using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ChainRenamer.App.ViewModels.Modules;

namespace ChainRenamer.App.Views.Modules;

public partial class SourceSettingsView : UserControl
{
    public SourceSettingsView() => InitializeComponent();

    // The folder picker needs the window, which the view model does not know about.
    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SourceSettingsViewModel vm || TopLevel.GetTopLevel(this) is not { } top) return;

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        if (folders is [var folder] && folder.TryGetLocalPath() is { } path)
        {
            vm.Folder = path;
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
