using System.Collections.ObjectModel;
using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels;

/// <summary>A favorite in the tree of the "Organize favorites" window: a chain, or a folder.</summary>
public partial class FavoriteNode : ViewModelBase
{
    public FavoriteNode(Favorite favorite, FavoriteNode? parent)
    {
        Name = favorite.Name;
        IsFolder = favorite.IsFolder;
        Parent = parent;
        Items = [.. (favorite.Items ?? []).Select(f => new FavoriteNode(f, this))];
    }

    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial bool IsExpanded { get; set; } = true;

    public bool IsFolder { get; }
    public FavoriteNode? Parent { get; set; }
    public ObservableCollection<FavoriteNode> Items { get; }

    public Favorite ToFavorite() => IsFolder
        ? Favorite.Folder(string.IsNullOrWhiteSpace(Name) ? Strings.Favorites_NewFolder : Name.Trim(), Items.Select(n => n.ToFavorite()))
        : new(Name);

    public IEnumerable<FavoriteNode> SelfAndDescendants() => Items.SelectMany(n => n.SelfAndDescendants()).Prepend(this);
}

/// <summary>A folder the selected favorite can move to (null: the top level), indented by its depth.</summary>
public sealed record FavoriteDestination(string Label, FavoriteNode? Folder);

/// <summary>
/// The "Organize favorites" window. Every change is saved at once, as in a browser.
/// The chains themselves are never touched: a favorite is only a shortcut to a chain of the library.
/// </summary>
public partial class FavoritesViewModel : ViewModelBase
{
    private readonly ChainLibrary library;

    public FavoritesViewModel(ChainLibrary library)
    {
        this.library = library;
        Roots = [.. library.Favorites().Select(f => new FavoriteNode(f, null))];
    }

    public ObservableCollection<FavoriteNode> Roots { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRename))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand), nameof(MoveUpCommand), nameof(MoveDownCommand))]
    public partial FavoriteNode? Selected { get; set; }

    /// <summary>Only folders are renamed here: the name of a chain is the name of its file.</summary>
    public bool CanRename => Selected is { IsFolder: true };

    /// <summary>Why the last change could not be saved (the library is on a NAS that went away…).</summary>
    [ObservableProperty] public partial string? Error { get; set; }

    private bool HasSelection => Selected is not null;

    private ObservableCollection<FavoriteNode> SiblingsOf(FavoriteNode node) => node.Parent?.Items ?? Roots;

    // ───────────── Commands ─────────────

    [RelayCommand]
    private void NewFolder() => Place(new FavoriteNode(Favorite.Folder(Strings.Favorites_NewFolder), null));

    /// <summary>The chains of the library that are not favorites yet.</summary>
    public IReadOnlyList<string> AddableChains()
    {
        var favorites = Roots.SelectMany(n => n.SelfAndDescendants()).Where(n => !n.IsFolder).Select(n => n.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. library.Names().Where(n => !favorites.Contains(n))];
    }

    public void AddChain(string name) => Place(new FavoriteNode(new Favorite(name), null));

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove()
    {
        var node = Selected!;
        SiblingsOf(node).Remove(node);
        Selected = null;
        Save();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveUp() => Shift(-1);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveDown() => Shift(+1);

    /// <summary>The folders the selection can go to: not into itself, nor where it already is.</summary>
    public IReadOnlyList<FavoriteDestination> Destinations()
    {
        if (Selected is not { } node) return [];
        var inside = node.SelfAndDescendants().ToHashSet();
        var list = new List<FavoriteDestination>();
        if (node.Parent is not null) list.Add(new(Strings.Favorites_TopLevel, null));
        Add(Roots, 0);
        return list;

        void Add(IEnumerable<FavoriteNode> nodes, int depth)
        {
            foreach (var folder in nodes.Where(n => n.IsFolder && !inside.Contains(n)))
            {
                if (folder != node.Parent) list.Add(new(new string(' ', 4 * depth) + folder.Name, folder));
                Add(folder.Items, depth + 1);
            }
        }
    }

    /// <summary>Moves the selection at the end of <paramref name="folder"/>, or of the top level.</summary>
    public void MoveTo(FavoriteNode? folder)
    {
        if (Selected is not { } node) return;
        SiblingsOf(node).Remove(node);
        node.Parent = folder;
        (folder?.Items ?? Roots).Add(node);
        if (folder is not null) folder.IsExpanded = true;
        Selected = node;
        Save();
    }

    // ───────────── Helpers ─────────────

    /// <summary>A new favorite goes into the selected folder, else after the selection, else at the end.</summary>
    private void Place(FavoriteNode node)
    {
        switch (Selected)
        {
            case { IsFolder: true } folder:
                node.Parent = folder;
                folder.Items.Add(node);
                folder.IsExpanded = true;
                break;
            case { } selected:
                node.Parent = selected.Parent;
                SiblingsOf(selected).Insert(SiblingsOf(selected).IndexOf(selected) + 1, node);
                break;
            default:
                Roots.Add(node);
                break;
        }
        Selected = node;
        Save();
    }

    private void Shift(int offset)
    {
        var node = Selected!;
        var siblings = SiblingsOf(node);
        int to = siblings.IndexOf(node) + offset;
        if (to < 0 || to >= siblings.Count) return;

        siblings.Move(siblings.IndexOf(node), to);
        Selected = node;
        Save();
    }

    /// <summary>Renaming a folder: saved as it is typed, as the other changes.</summary>
    partial void OnSelectedChanged(FavoriteNode? oldValue, FavoriteNode? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= OnNodeChanged;
        if (newValue is not null) newValue.PropertyChanged += OnNodeChanged;
    }

    private void OnNodeChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FavoriteNode.Name)) Save();
    }

    private void Save()
    {
        try
        {
            library.SaveFavorites(Roots.Select(n => n.ToFavorite()));
            Error = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Error = string.Format(Strings.Chain_IoError, ex.Message);
        }
    }
}
