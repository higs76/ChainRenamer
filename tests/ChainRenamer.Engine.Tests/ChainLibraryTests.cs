using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests;

public sealed class ChainLibraryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "chainrenamer-tests", Guid.NewGuid().ToString("N"));
    private readonly ChainLibrary library;

    public ChainLibraryTests() => library = new ChainLibrary(Path.Combine(root, "library"));

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private static Chain ChainOf(string find) => new([new(StepId.New(), new ReplaceModule(find, "x"))]);

    private static string FindOf(Chain chain) => ((ReplaceModule)chain.Steps[0].Module).Find;

    [Fact]
    public void An_unknown_folder_is_an_empty_library()
    {
        Assert.Empty(library.Names());
        Assert.Empty(library.Favorites());
    }

    [Fact]
    public async Task Lists_saved_chains_in_natural_order()
    {
        foreach (var name in new[] { "Series 10", "Series 9", "Photos" }) await library.SaveAsync(name, ChainOf(name));

        Assert.Equal(["Photos", "Series 9", "Series 10"], library.Names());
        Assert.Equal("Series 9", FindOf(await library.LoadAsync("Series 9")));
    }

    [Fact]
    public async Task Saving_again_replaces_the_chain()
    {
        await library.SaveAsync("Series", ChainOf("a"));
        await library.SaveAsync("Series", ChainOf("b"));

        Assert.Equal(["Series"], library.Names());
        Assert.Equal("b", FindOf(await library.LoadAsync("Series")));
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("what?")]
    [InlineData("CON")]
    [InlineData("")]
    [InlineData("trailing ")]
    public async Task Refuses_names_that_are_not_file_names(string name)
    {
        Assert.False(ChainLibrary.IsValidName(name));
        await Assert.ThrowsAsync<ArgumentException>(() => library.SaveAsync(name, ChainOf("a")));
    }

    /// <summary>The tree in one line: "Anime, Series[Films, Empty[]]".</summary>
    private static string Show(IEnumerable<Favorite> favorites) =>
        string.Join(", ", favorites.Select(f => f.IsFolder ? $"{f.Name}[{Show(f.Items!)}]" : f.Name));

    private async Task SaveChains(params string[] names)
    {
        foreach (var name in names) await library.SaveAsync(name, ChainOf(name));
    }

    [Fact]
    public async Task Favorites_follow_the_chains()
    {
        await SaveChains("Series", "Photos", "Music");

        library.SetFavorite("Series", true);
        library.SetFavorite("Photos", true);
        library.SetFavorite("Music", true);
        library.SetFavorite("Photos", false);
        library.SetFavorite("series", true);    // already there
        Assert.Equal("Series, Music", Show(library.Favorites()));    // in the order they were added, as in a browser
        Assert.True(library.IsFavorite("series"));

        library.Delete("Series");
        Assert.Equal("Music", Show(library.Favorites()));
        Assert.Equal(["Music", "Photos"], library.Names());
    }

    [Fact]
    public async Task Favorites_can_be_sorted_into_folders()
    {
        await SaveChains("Anime", "Films", "Photos");

        library.SaveFavorites([Favorite.Folder("Series", Favorite.Folder("Japan", new Favorite("Anime")), new Favorite("Films")), Favorite.Folder("Empty"), new("Photos")]);

        Assert.Equal("Series[Japan[Anime], Films], Empty[], Photos", Show(library.Favorites()));
        Assert.True(library.IsFavorite("Anime"));

        library.SetFavorite("Anime", false);
        Assert.Equal("Series[Japan[], Films], Empty[], Photos", Show(library.Favorites()));
    }

    [Fact]
    public async Task A_deleted_chain_leaves_its_folders()
    {
        await SaveChains("Anime", "Films");
        library.SaveFavorites([Favorite.Folder("Series", new Favorite("Anime"), new Favorite("Films"), new Favorite("Gone"))]);

        library.Delete("Anime");

        Assert.Equal("Series[Films]", Show(library.Favorites()));    // "Gone" no longer exists either
    }

    [Fact]
    public async Task Reads_the_favorites_of_version_0_7()
    {
        await SaveChains("Series", "Photos");
        Directory.CreateDirectory(library.Folder);
        await File.WriteAllTextAsync(Path.Combine(library.Folder, "favorites.json"), """["Series","Photos","Gone"]""");

        Assert.Equal("Series, Photos", Show(library.Favorites()));
    }

    [Fact]
    public async Task Damaged_favorites_are_no_favorites()
    {
        await SaveChains("Series");
        Directory.CreateDirectory(library.Folder);
        await File.WriteAllTextAsync(Path.Combine(library.Folder, "favorites.json"), "{ broken");

        Assert.Empty(library.Favorites());
        library.SetFavorite("Series", true);
        Assert.Equal("Series", Show(library.Favorites()));
    }

    [Fact]
    public async Task Free_name_adds_a_number()
    {
        await library.SaveAsync("Series", ChainOf("a"));
        await library.SaveAsync("Series (2)", ChainOf("a"));

        Assert.Equal("Photos", library.FreeName("Photos"));
        Assert.Equal("Series (3)", library.FreeName("Series"));
    }

    [Fact]
    public async Task Imports_a_chain_file_under_a_free_name()
    {
        var file = Path.Combine(root, "from a friend", "Series.chain");
        await ChainOf("friend").SaveAsync(file);
        await library.SaveAsync("Series", ChainOf("mine"));

        Assert.Equal("Series (2)", await library.ImportAsync(file));
        Assert.Equal("friend", FindOf(await library.LoadAsync("Series (2)")));
        Assert.Equal("mine", FindOf(await library.LoadAsync("Series")));
    }

    [Fact]
    public async Task Importing_an_invalid_file_imports_nothing()
    {
        var file = Path.Combine(root, "broken.chain");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(file, "not a chain");

        await Assert.ThrowsAsync<ChainFormatException>(() => library.ImportAsync(file));
        Assert.Empty(library.Names());
    }

    [Fact]
    public async Task Export_all_then_import_all_restores_the_library()
    {
        await library.SaveAsync("Series", ChainOf("a"));
        await library.SaveAsync("Photos", ChainOf("b"));
        library.SaveFavorites([Favorite.Folder("Video", new Favorite("Series")), new("Photos")]);
        var zip = Path.Combine(root, "backup.zip");
        library.ExportAll(zip);

        var reinstalled = new ChainLibrary(Path.Combine(root, "new pc"));
        await reinstalled.SaveAsync("Series", ChainOf("already here"));
        reinstalled.SaveFavorites([Favorite.Folder("video", new Favorite("Series"))]);
        var imported = await reinstalled.ImportAllAsync(zip);

        Assert.Equal(["Photos", "Series (2)"], imported.Order());
        Assert.Equal(["Photos", "Series", "Series (2)"], reinstalled.Names());
        Assert.Equal("video[Series, Series (2)], Photos", Show(reinstalled.Favorites()));    // folders of the same name merged
        Assert.Equal("a", FindOf(await reinstalled.LoadAsync("Series (2)")));
    }

    [Fact]
    public async Task Copy_to_another_folder_keeps_what_is_there()
    {
        await library.SaveAsync("Series", ChainOf("a"));
        await library.SaveAsync("Photos", ChainOf("b"));
        library.SaveFavorites([new("Series"), Favorite.Folder("Mine", new Favorite("Photos"), new Favorite("Series"))]);
        var nas = new ChainLibrary(Path.Combine(root, "nas"));
        await nas.SaveAsync("Series", ChainOf("on the nas"));
        nas.SetFavorite("Series", true);

        library.CopyTo(nas.Folder);

        Assert.Equal(["Photos", "Series"], nas.Names());
        Assert.Equal("on the nas", FindOf(await nas.LoadAsync("Series")));
        Assert.Equal("Series, Mine[Photos, Series]", Show(nas.Favorites()));    // both lists of favorites, "Series" once at the top
    }
}
