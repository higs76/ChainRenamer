using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels.Modules;

namespace ChainRenamer.App.Views.Modules;

public partial class ListSettingsView : UserControl
{
    private static readonly FilePickerFileType TextFiles = new(Strings.Picker_Text) { Patterns = ["*.txt", "*.tsv", "*.csv"] };

    public ListSettingsView()
    {
        InitializeComponent();
        // A text file dropped anywhere on the panel, even on the text box, becomes the list.
        AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    private ListSettingsViewModel? ViewModel => DataContext as ListSettingsViewModel;

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.TryGetFile()?.TryGetLocalPath() is not { } path || ViewModel is not { } vm) return;
        e.Handled = true;
        vm.Lines = await File.ReadAllTextAsync(path);
    }

    private async void OnLoad(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } top || ViewModel is not { } vm) return;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.ReplaceTable_Load,
            FileTypeFilter = [TextFiles],
        });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) vm.Lines = await File.ReadAllTextAsync(path);
    }
}
