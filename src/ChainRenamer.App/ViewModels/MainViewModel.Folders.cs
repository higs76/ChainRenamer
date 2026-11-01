using ChainRenamer.App.Resources;
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
