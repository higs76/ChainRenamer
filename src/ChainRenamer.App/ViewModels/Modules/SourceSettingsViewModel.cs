using System.Collections.Immutable;
using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class SourceSettingsViewModel : ModuleSettingsViewModel
{
    [ObservableProperty] public partial string Folder { get; set; } = "";
    [ObservableProperty] public partial bool IncludeSubfolders { get; set; }
    [ObservableProperty] public partial ImmutableHashSet<FileId> Excluded { get; set; } = [];
    [ObservableProperty] public partial string? LoadError { get; set; }

    /// <summary>Raised once the folder has been listed.</summary>
    public event Action<IReadOnlyList<FileItem>>? FilesLoaded;

    public override string Title => Strings.Module_Source;

    public override IModule ToModule() => new SourceModule(Folder, IncludeSubfolders) { Excluded = Excluded };

    public void SetIncluded(FileId id, bool included) => Excluded = included ? Excluded.Remove(id) : Excluded.Add(id);

    partial void OnIncludeSubfoldersChanged(bool value)
    {
        if (Folder.Length > 0) LoadCommand.Execute(null);
    }

    // [RelayCommand] generates LoadCommand, an AsyncRelayCommand: it cannot run twice at once,
    // and its IsRunning property drives the "Loading…" text.
    [RelayCommand]
    private async Task LoadAsync()
    {
        LoadError = null;
        try
        {
            var files = await FileScanner.ScanAsync(new SourceModule(Folder.Trim(), IncludeSubfolders));
            Excluded = [];
            FilesLoaded?.Invoke(files);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            LoadError = ex switch
            {
                DirectoryNotFoundException or ArgumentException => Strings.Source_NotFound,
                UnauthorizedAccessException => Strings.Source_AccessDenied,
                _ => ex.Message,
            };
        }
    }
}
