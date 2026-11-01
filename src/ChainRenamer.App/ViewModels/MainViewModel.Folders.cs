using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels;

// The folders: opening one (command line, Explorer menu, drop on the window), the recent ones, the Explorer menu.
public partial class MainViewModel
{
    private const int MaxRecentFolders = 10;

    /// <summary>The folders loaded last, most recent first, for the menu of the source.</summary>
    public IReadOnlyList<string> RecentFolders => preferences.RecentFolders ?? [];

    public bool HasRecentFolders => RecentFolders.Count > 0;

    /// <summary>The "Recent" menu of the source: each entry opens its folder.</summary>
    public IReadOnlyList<MenuEntry> RecentFolderEntries => [.. RecentFolders.Select(f => new MenuEntry(f, new RelayCommand(() => OpenFolder(f))))];

    /// <summary>Loads a folder in the source and shows it: from the command line, the Explorer menu or a drop.</summary>
    public void OpenFolder(string folder)
    {
        source.Folder = folder;
        SelectedStep = Steps[0];
        source.LoadCommand.Execute(null);
    }

    // ───────────── Metadata ─────────────

    /// <summary>The metadata of the folder is being read: renaming waits, since names may still change.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool IsReadingMetadata { get; set; }

    private CancellationTokenSource? metadataReading;

    /// <summary>
    /// Reads the shooting dates and music tags after the names are shown, then computes the preview again.
    /// Loading another folder cancels it.
    /// </summary>
    private async Task ReadMetadataAsync(IReadOnlyList<FileItem> files)
    {
        metadataReading?.Cancel();
        if (!files.Any(f => MetadataReader.CanRead(f.FullPath))) return;

        var reading = metadataReading = new CancellationTokenSource();
        IsReadingMetadata = true;
        try
        {
            var progress = new Progress<int>(n =>
            {
                if (n % 25 == 0 && !reading.IsCancellationRequested) SetStatus(string.Format(Strings.Metadata_Reading, n, files.Count));
            });
            var read = await MetadataReader.ReadAsync(files, progress, reading.Token);
            if (reading.IsCancellationRequested) return;
            pipeline.SetFiles(read);
            Refresh();
            SetStatus(string.Format(Strings.Metadata_Done, read.Count(f => !f.Metadata.IsEmpty)));
        }
        catch (OperationCanceledException)
        {
            // Another folder was loaded meanwhile.
        }
        finally
        {
            if (metadataReading == reading) IsReadingMetadata = false;
        }
    }

    private void Remember(string folder)
    {
        if (folder.Length == 0) return;
        preferences = preferences with
        {
            RecentFolders = [folder, .. RecentFolders.Where(f => !f.Equals(folder, StringComparison.OrdinalIgnoreCase)).Take(MaxRecentFolders - 1)],
        };
        preferences.Save();
        OnPropertyChanged(nameof(RecentFolders));
        OnPropertyChanged(nameof(RecentFolderEntries));
        OnPropertyChanged(nameof(HasRecentFolders));
    }

    /// <summary>The chain column shows only the number and icon of each step, to leave room for the preview.</summary>
    public bool ChainCollapsed
    {
        get => preferences.ChainCollapsed;
        set
        {
            if (value == preferences.ChainCollapsed) return;
            preferences = preferences with { ChainCollapsed = value };
            preferences.Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ChainToggle));
            OnPropertyChanged(nameof(AddLabel));
            OnPropertyChanged(nameof(ShowChainHint));
        }
    }

    /// <summary>The arrow of the button that collapses or expands the chain column.</summary>
    public string ChainToggle => ChainCollapsed ? "›" : "‹";

    public string AddLabel => ChainCollapsed ? "+" : Strings.AddModule;

    /// <summary>The reminder that steps can be dragged: as soon as there are two, and not in the collapsed column.</summary>
    public bool ShowChainHint => CanReorder && !ChainCollapsed;

    [RelayCommand]
    private void ToggleChain() => ChainCollapsed = !ChainCollapsed;

    // ───────────── Explorer menu ─────────────

    public bool CanUseExplorerMenu => OperatingSystem.IsWindows();

    /// <summary>"Rename with ChainRenamer" in the Explorer menu of folders, for this program.</summary>
    public bool ExplorerMenuInstalled
    {
        get => OperatingSystem.IsWindows() && ExplorerMenu.IsInstalled;
        set
        {
            if (!OperatingSystem.IsWindows() || value == ExplorerMenuInstalled) return;
            try
            {
                if (value) ExplorerMenu.Install(Strings.ExplorerMenu_Entry);
                else ExplorerMenu.Uninstall();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                SetStatus(string.Format(Strings.ExplorerMenu_Failed, ex.Message), error: true);
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExplorerMenuElsewhere));
        }
    }

    /// <summary>The entry starts another copy of ChainRenamer (moved, or another version): <see cref="RepairExplorerMenu"/> fixes it.</summary>
    public bool ExplorerMenuElsewhere =>
        OperatingSystem.IsWindows() && ExplorerMenu.Command() is { } command && Environment.ProcessPath is { } exe
        && !command.Contains(exe, StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private void RepairExplorerMenu()
    {
        if (!OperatingSystem.IsWindows()) return;
        ExplorerMenu.Install(Strings.ExplorerMenu_Entry);
        OnPropertyChanged(nameof(ExplorerMenuElsewhere));
    }
}
