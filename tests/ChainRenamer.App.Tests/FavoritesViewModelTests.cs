using ChainRenamer.App.ViewModels;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;

namespace ChainRenamer.App.Tests;

public sealed class FavoritesViewModelTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "chainrenamer-tests", Guid.NewGuid().ToString("N"));
    private readonly ChainLibrary library;

    public FavoritesViewModelTests()
    {
        library = new ChainLibrary(root);
        foreach (var name in new[] { "Anime", "Films", "Photos" })
            library.SaveAsync(name, new Chain([new(StepId.New(), new ReplaceModule(".", " "))])).Wait();
        library.SaveFavorites([new("Photos"), Favorite.Folder("Series", new Favorite("Anime"))]);
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static string Show(IEnumerable<Favorite> favorites) =>
        string.Join(", ", favorites.Select(f => f.IsFolder ? $"{f.Name}[{Show(f.Items!)}]" : f.Name));

    private string Saved => Show(library.Favorites());

    private static FavoriteNode Find(FavoritesViewModel vm, string name) =>
        vm.Roots.SelectMany(n => n.SelfAndDescendants()).First(n => n.Name == name);

    [Fact]
    public void A_new_folder_goes_into_the_selected_folder_else_after_the_selection()
    {
        var vm = new FavoritesViewModel(library);

        vm.Selected = Find(vm, "Series");
        vm.NewFolderCommand.Execute(null);
        vm.Selected!.Name = "Japan";
        vm.Selected = Find(vm, "Photos");
        vm.NewFolderCommand.Execute(null);
        vm.Selected!.Name = "Holidays";

        Assert.Equal("Photos, Holidays[], Series[Anime, Japan[]]", Saved);     // saved at once
    }

    [Fact]
    public void Moves_into_a_folder_and_back()
    {
        var vm = new FavoritesViewModel(library);
        vm.Selected = Find(vm, "Photos");

        Assert.Equal(["Series"], vm.Destinations().Select(d => d.Label));
        vm.MoveTo(Find(vm, "Series"));
        Assert.Equal("Series[Anime, Photos]", Saved);

        Assert.Equal([null], vm.Destinations().Select(d => d.Folder));      // the top level only
        vm.MoveTo(null);
        vm.MoveUpCommand.Execute(null);
        Assert.Equal("Photos, Series[Anime]", Saved);
    }

    [Fact]
    public void A_folder_cannot_go_into_itself()
    {
        var vm = new FavoritesViewModel(library);
        vm.Selected = Find(vm, "Series");
        vm.NewFolderCommand.Execute(null);      // Series > New folder
        vm.Selected = Find(vm, "Series");

        Assert.Empty(vm.Destinations());
    }

    [Fact]
    public void Adds_the_chains_that_are_not_favorites_yet()
    {
        var vm = new FavoritesViewModel(library);

        Assert.Equal(["Films"], vm.AddableChains());
        vm.Selected = Find(vm, "Anime");
        vm.AddChain("Films");
        Assert.Equal("Photos, Series[Anime, Films]", Saved);
        Assert.Empty(vm.AddableChains());
    }

    [Fact]
    public void Removing_a_favorite_keeps_the_chain()
    {
        var vm = new FavoritesViewModel(library);
        vm.Selected = Find(vm, "Series");

        vm.RemoveCommand.Execute(null);

        Assert.Equal("Photos", Saved);
        Assert.Contains("Anime", library.Names());
    }
}
