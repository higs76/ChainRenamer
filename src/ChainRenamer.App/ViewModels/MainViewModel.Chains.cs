using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels;

/// <summary>A chain of the library, as listed in the Chains menu.</summary>
public sealed record ChainEntry(string Name, bool IsFavorite);

// The other half of MainViewModel (a partial class, as in VB.NET): the saved chains and the library.
public partial class MainViewModel
{
    private static readonly string EmptyChainJson = new Chain([]).ToJson();

    private Preferences preferences;
    private ChainLibrary library;
    private string savedJson = EmptyChainJson;      // the chain as last saved or loaded, to detect changes
    private bool showingFavorite;

    /// <summary>The name of the chain in the library, or null for a new chain not saved yet.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(HasName))]
    [NotifyCanExecuteChangedFor(nameof(DeleteChainCommand))]
    public partial string? ChainName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsModified { get; set; }

    [ObservableProperty] public partial bool IsFavorite { get; set; }

    public bool HasName => ChainName is not null;
    public string LibraryFolder => library.Folder;

    /// <summary>The steps after the source: what a .chain file holds.</summary>
    private Chain CurrentChain() => new([.. Steps.Skip(1).Select(s => s.ToStep())]);

    public IReadOnlyList<string> FavoriteChains() => library.Favorites();

    public IReadOnlyList<ChainEntry> SavedChains()
    {
        var favorites = library.Favorites().ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. library.Names().Select(name => new ChainEntry(name, favorites.Contains(name)))];
    }

    // ───────────── New, open, save ─────────────

    [RelayCommand]
    private void NewChain() => Guard(() =>
    {
        Show(null, new Chain([]));
        return Task.CompletedTask;
    });

    public void OpenChain(string name) => Guard(async () =>
    {
        Chain? chain = null;
        if (await Try(async () => chain = await library.LoadAsync(name)))
        {
            Show(name, chain!);
            SetStatus(string.Format(Strings.Chain_Loaded, name));
        }
    });

    /// <summary>Replaces the steps by those of <paramref name="chain"/>. The source and its files stay.</summary>
    private void Show(string? name, Chain chain)
    {
        foreach (var step in Steps.Skip(1).ToList())
        {
            pipeline.Remove(step.Id);
            Steps.Remove(step);
        }
        foreach (var step in chain.Steps)
            if (ModuleCatalog.SettingsOf(step.Module) is { } settings) AddStep(settings, step.Name, step.IsEnabled);

        SelectedStep = Steps[^1];
        Saved(name, chain);
        Refresh();
    }

    private void Saved(string? name, Chain chain)
    {
        ChainName = name;
        savedJson = chain.ToJson();
        showingFavorite = true;     // showing the state of the chain, not a change by the user
        IsFavorite = name is not null && library.IsFavorite(name);
        showingFavorite = false;
    }

    /// <summary>Ctrl+S: a chain that has no name yet asks for one.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (ChainName is { } name) await SaveToAsync(name);
        else SaveAs();
    }

    private async Task SaveToAsync(string name)
    {
        var chain = CurrentChain();
        if (!await Try(() => library.SaveAsync(name, chain))) return;

        Saved(name, chain);
        IsModified = false;
        SetStatus(string.Format(Strings.Chain_Saved, name));
    }

    // "Save as": the name is typed in the top bar.
    [ObservableProperty] public partial bool IsNaming { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameProblem), nameof(SaveAsLabel))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmSaveAsCommand))]
    public partial string NameInput { get; set; } = "";

    private string TypedName => NameInput.Trim();
    private bool CanSaveAs => ChainLibrary.IsValidName(TypedName);
    private bool ReplacesAnother => !TypedName.Equals(ChainName, StringComparison.OrdinalIgnoreCase) && library.Contains(TypedName);

    /// <summary>Why the typed name is a problem: not a file name, or the name of another chain.</summary>
    public string? NameProblem =>
        TypedName.Length == 0 ? null
        : !CanSaveAs ? Strings.SaveAs_Invalid
        : ReplacesAnother ? string.Format(Strings.SaveAs_Exists, TypedName)
        : null;

    public string SaveAsLabel => CanSaveAs && ReplacesAnother ? Strings.SaveAs_Replace : Strings.SaveAs_Save;

    [RelayCommand]
    private void SaveAs()
    {
        NameInput = ChainName ?? "";
        IsNaming = true;
    }

    [RelayCommand(CanExecute = nameof(CanSaveAs))]
    private async Task ConfirmSaveAsAsync()
    {
        IsNaming = false;
        await SaveToAsync(TypedName);
    }

    [RelayCommand]
    private void CancelSaveAs() => IsNaming = false;

    // ───────────── Favorites and deletion ─────────────

    partial void OnIsFavoriteChanged(bool value)
    {
        if (!showingFavorite && ChainName is { } name) _ = Try(() => library.SetFavorite(name, value));
    }

    [RelayCommand(CanExecute = nameof(HasName))]
    private void DeleteChain()
    {
        var name = ChainName!;
        Confirm(string.Format(Strings.Chain_DeleteConfirm, name), Strings.Chain_DeleteYes, async () =>
        {
            if (!await Try(() => library.Delete(name))) return;

            // The steps stay on screen, as a new chain not saved yet.
            Saved(null, new Chain([]));
            Refresh();
            SetStatus(string.Format(Strings.Chain_Deleted, name));
        });
    }

    // ───────────── Import, export, library location (the view picks the files) ─────────────

    /// <summary>Imports .chain files and library backups (.zip). A single imported chain is opened.</summary>
    public async Task ImportAsync(IEnumerable<string> files)
    {
        var names = new List<string>();
        bool ok = true;
        foreach (var file in files)
        {
            ok &= await Try(async () => names.AddRange(Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                ? await library.ImportAllAsync(file)
                : [await library.ImportAsync(file)]));
        }
        if (!ok) return;    // the status shows the error

        if (names.Count == 1) OpenChain(names[0]);
        else SetStatus(string.Format(Strings.Import_Done, names.Count));
    }

    /// <summary>Exports the chain as it is on screen, saved or not.</summary>
    public async Task ExportAsync(string file)
    {
        if (await Try(() => CurrentChain().SaveAsync(file))) SetStatus(string.Format(Strings.Export_Done, file));
    }

    public async Task ExportAllAsync(string zip)
    {
        if (await Try(() => library.ExportAll(zip))) SetStatus(string.Format(Strings.Export_Done, zip));
    }

    /// <summary>Moves the library: its chains are copied into the new folder, which then becomes the library.</summary>
    public async Task MoveLibraryAsync(string folder)
    {
        if (!await Try(() => library.CopyTo(folder))) return;

        library = new ChainLibrary(folder);
        preferences = preferences with { LibraryFolder = folder };
        preferences.Save();
        OnPropertyChanged(nameof(LibraryFolder));
        SetStatus(string.Format(Strings.Library_Moved, folder));
    }

    // ───────────── Helpers ─────────────

    /// <summary>Runs <paramref name="action"/> once the user agrees to lose the unsaved changes, if there are any.</summary>
    private void Guard(Func<Task> action)
    {
        if (IsModified) Confirm(string.Format(Strings.Chain_Discard, ChainName ?? Strings.Chain_Untitled), Strings.Chain_DiscardYes, action);
        else _ = action();
    }

    /// <summary>Closing the window: the same question about unsaved changes.</summary>
    public void CloseAfterGuard(Action close) => Guard(() =>
    {
        close();
        return Task.CompletedTask;
    });

    /// <summary>Runs a library operation: a failure becomes the status message. Returns whether it succeeded.</summary>
    private async Task<bool> Try(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (ChainFormatException ex)
        {
            SetStatus(Describe(ex), error: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            SetStatus(string.Format(Strings.Chain_IoError, ex.Message), error: true);
        }
        return false;
    }

    private Task<bool> Try(Action action) => Try(() =>
    {
        action();
        return Task.CompletedTask;
    });

    private static string Describe(ChainFormatException ex) => string.Format(ex.Error switch
    {
        ChainError.UnsupportedVersion => Strings.ChainError_UnsupportedVersion,
        ChainError.UnknownModule => Strings.ChainError_UnknownModule,
        ChainError.InvalidSettings => Strings.ChainError_InvalidSettings,
        _ => Strings.ChainError_InvalidFile,
    }, ex.Detail);
}
