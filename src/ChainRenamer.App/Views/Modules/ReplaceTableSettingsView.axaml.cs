using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels.Modules;

namespace ChainRenamer.App.Views.Modules;

public partial class ReplaceTableSettingsView : UserControl
{
    private static readonly FilePickerFileType TextFiles = new(Strings.Picker_Text) { Patterns = ["*.txt", "*.tsv", "*.csv"] };

    public ReplaceTableSettingsView() => InitializeComponent();

    private ReplaceTableSettingsViewModel? ViewModel => DataContext as ReplaceTableSettingsViewModel;

    private async void OnPaste(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard && await clipboard.TryGetTextAsync() is { Length: > 0 } text)
            ViewModel?.Import(text);
    }

    private async void OnLoad(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } top || ViewModel is not { } vm) return;
        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.ReplaceTable_Load,
            FileTypeFilter = [TextFiles],
        });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) vm.Import(await File.ReadAllTextAsync(path));
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } top || ViewModel is not { } vm) return;
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.ReplaceTable_Save,
            SuggestedFileName = Strings.Module_ReplaceTable + ".txt",
            FileTypeChoices = [TextFiles],
        });
        if (file?.TryGetLocalPath() is { } path) await File.WriteAllTextAsync(path, vm.Export());
    }
}
