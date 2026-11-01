using System.Reflection;
using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests;

public class ChainTests
{
    private static Chain Sample() => new(
    [
        new(StepId.New(), new SortModule(Descending: true)),
        new(StepId.New(), new ReplaceModule(".", " ", MatchCase: true, Occurrence.Last), "Points"),
        new(StepId.New(), new RegexReplaceModule(@"(\d+)x(\d+)", "S$1E$2"), IsEnabled: false),
    ]);

    private static Chain RoundTrip(Chain chain) => Chain.FromJson(chain.ToJson());

    [Fact]
    public void Round_trip_keeps_modules_names_and_states()
    {
        var loaded = RoundTrip(Sample());

        Assert.Equal(Sample().Steps.Select(s => (s.Module, s.Name, s.IsEnabled)), loaded.Steps.Select(s => (s.Module, s.Name, s.IsEnabled)));
    }

    [Fact]
    public void Every_setting_of_every_module_survives_a_round_trip()
    {
        // Settings that are not the defaults, so that a forgotten one shows.
        IModule[] modules =
        [
            new CaseModule(TextCase.Sentence, SmallWords.French, "TV, iPhone", RomanNumerals: false, KeepAcronyms: false, SpaceBeforeCapitals: true),
            new RemoveModule(RemoveWhat.Between, 2, 3, -1, "[", "]", IncludeText: false, LastOccurrence: true, MatchCase: true),
            new CleanModule(false, false, true, true, true, true, true),
            new InsertModule("x", InsertAt.After, 3, "a", MatchCase: true),
            new NumberingModule(5, 2, 3, NumberStyle.UpperRoman, InsertAt.End, 4, "b", "_", ResetPerFolder: true),
            new ExtensionModule(ExtensionAction.Add, "gif"),
        ];

        var loaded = RoundTrip(new Chain([.. modules.Select(m => new Step(StepId.New(), m))]));

        Assert.Equal(modules, loaded.Steps.Select(s => s.Module));
    }

    [Fact]
    public void Loading_gives_new_step_ids()
    {
        var chain = Sample();

        Assert.DoesNotContain(RoundTrip(chain).Steps, s => chain.Steps.Any(o => o.Id == s.Id));
    }

    [Fact]
    public void Json_is_versioned_and_readable()
    {
        var json = Sample().ToJson();

        Assert.Contains("\"version\": 1", json);
        Assert.Contains("\"type\": \"Replace\"", json);
        Assert.Contains("\"name\": \"Points\"", json);
        Assert.Contains("\"occurrence\": \"last\"", json);
        Assert.Contains("\"replaceWith\": \" \"", json);
    }

    [Fact]
    public void Json_keeps_text_as_typed()
    {
        var json = new Chain([new(StepId.New(), new RegexReplaceModule(@"(\d+)'é", "S$1"), "L'épisode")]).ToJson();

        Assert.Contains("""
            "name": "L'épisode"
            """, json);
        Assert.Contains("""
            "pattern": "(\\d+)'é"
            """, json);
    }

    [Fact]
    public void Session_data_is_not_saved()
    {
        var file = FileId.New();
        var chain = new Chain(
        [
            new(StepId.New(), new SortModule { ManualOrder = [file] }),
            new(StepId.New(), new ReplaceModule("a", "b")) { Skipped = [file] },
        ]);

        var loaded = RoundTrip(chain);

        Assert.Empty(((SortModule)loaded.Steps[0].Module).ManualOrder);
        Assert.Empty(loaded.Steps[1].Skipped);
    }

    [Fact]
    public void Missing_values_take_their_default()
    {
        var chain = Chain.FromJson("""
            { "version": 1, "steps": [
                { "type": "Replace", "settings": { "find": "a", "replaceWith": "b" } },
                { "type": "sort" } ] }
            """);

        Assert.Equal(new ReplaceModule("a", "b", MatchCase: false, Occurrence.All), chain.Steps[0].Module);
        Assert.True(chain.Steps[0].IsEnabled);
        Assert.Null(chain.Steps[0].Name);
        Assert.Equal(new SortModule(), chain.Steps[1].Module);
    }

    [Fact]
    public void Unknown_settings_are_ignored()
    {
        var chain = Chain.FromJson("""{ "version": 1, "steps": [ { "type": "Sort", "settings": { "descending": true, "later": 3 } } ] }""");

        Assert.Equal(new SortModule(Descending: true), chain.Steps[0].Module);
    }

    [Theory]
    [InlineData("""{ "version": 2, "steps": [] }""", ChainError.UnsupportedVersion, "2")]
    [InlineData("""{ "version": 1, "steps": [ { "type": "Teleport" } ] }""", ChainError.UnknownModule, "Teleport")]
    [InlineData("""{ "version": 1, "steps": [ { "type": "Replace", "settings": { "find": "a" } } ] }""", ChainError.InvalidSettings, "Replace")]
    [InlineData("""{ "version": 1, "steps": [ { "type": "Replace", "settings": { "find": null, "replaceWith": "" } } ] }""", ChainError.InvalidSettings, "Replace")]
    [InlineData("""{ "version": 1, "steps": [ { "type": "Replace", "settings": { "find": "a", "replaceWith": "", "occurrence": "often" } } ] }""", ChainError.InvalidSettings, "Replace")]
    [InlineData("""{ "version": 1 }""", ChainError.InvalidFile, null)]
    public void Bad_files_give_an_error_code(string json, ChainError error, string? detail)
    {
        var ex = Assert.Throws<ChainFormatException>(() => Chain.FromJson(json));

        Assert.Equal(error, ex.Error);
        if (detail is not null) Assert.Equal(detail, ex.Detail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("null")]
    [InlineData("""{ "steps": [] }""")]
    public void Not_a_chain_is_an_invalid_file(string json) =>
        Assert.Equal(ChainError.InvalidFile, Assert.Throws<ChainFormatException>(() => Chain.FromJson(json)).Error);

    [Fact]
    public void Every_module_except_the_source_can_be_saved()
    {
        var modules = typeof(IModule).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IModule).IsAssignableFrom(t) && t != typeof(SourceModule));

        Assert.All(modules, t => Assert.NotNull(t.GetCustomAttribute<ModuleAttribute>()));
    }

    [Fact]
    public void Module_names_are_unique()
    {
        var names = typeof(IModule).Assembly.GetTypes().Select(t => t.GetCustomAttribute<ModuleAttribute>()?.Name).OfType<string>().ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task Saves_and_loads_a_file()
    {
        var folder = Path.Combine(Path.GetTempPath(), "chainrenamer-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "library", "series.chain");
        try
        {
            await Sample().SaveAsync(path);
            await Sample().SaveAsync(path);     // overwriting is fine

            var loaded = await Chain.LoadAsync(path);

            Assert.Equal(Sample().Steps.Select(s => s.Module), loaded.Steps.Select(s => s.Module));
            Assert.Equal(["series.chain"], Directory.GetFiles(Path.GetDirectoryName(path)!).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
