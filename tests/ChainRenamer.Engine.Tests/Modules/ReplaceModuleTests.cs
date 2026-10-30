using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class ReplaceModuleTests
{
    private static string Replace(ReplaceModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    [Fact]
    public void Replaces_every_occurrence_ignoring_case_by_default()
    {
        Assert.Equal("a_b_c.txt", Replace(new(" ", "_"), "a b c.txt"));
        Assert.Equal("Episode-Episode.mkv", Replace(new("ep", "Episode"), "EP-ep.mkv"));
    }

    [Fact]
    public void Can_match_case()
    {
        Assert.Equal("Episode-ep.mkv", Replace(new("EP", "Episode", MatchCase: true), "EP-ep.mkv"));
    }

    [Fact]
    public void Keeps_the_extension_by_default()
    {
        Assert.Equal("photo-mkv.mkv", Replace(new(".", "-"), "photo.mkv.mkv"));
    }

    [Fact]
    public void Can_work_on_the_extension_only()
    {
        Assert.Equal("jpeg.jpg", Replace(new("jpeg", "jpg", Part: NamePart.Extension), "jpeg.jpeg"));
    }

    [Fact]
    public void Can_work_on_the_whole_name()
    {
        Assert.Equal("photo-mkv-mkv", Replace(new(".", "-", Part: NamePart.Whole), "photo.mkv.mkv"));
    }

    [Fact]
    public void Regex_replacement_can_use_groups()
    {
        var module = new ReplaceModule(@"(\d+)x(\d+)", "S$1E$2", UseRegex: true);

        Assert.Equal("Speedy - S01E10 - Time Attack!.mkv", Replace(module, "Speedy - 01x10 - Time Attack!.mkv"));
    }

    [Fact]
    public void Regex_ignores_case_unless_asked()
    {
        Assert.Equal("x-x.txt", Replace(new("[a-z]+", "x", UseRegex: true), "abc-ABC.txt"));
        Assert.Equal("x-ABC.txt", Replace(new("[a-z]+", "x", UseRegex: true, MatchCase: true), "abc-ABC.txt"));
    }

    [Fact]
    public void Text_mode_takes_regex_characters_literally()
    {
        Assert.Equal("a.b(1)", Replace(new("[1]", "(1)", Part: NamePart.Whole), "a.b[1]"));
    }

    [Fact]
    public void Empty_search_changes_nothing()
    {
        var input = TestItems.Of("a.txt");

        Assert.Same(input, new ReplaceModule("", "x").Apply(input));
    }

    [Fact]
    public void Keeps_ids_and_order()
    {
        var input = TestItems.Of("b.txt", "a.txt");

        var output = new ReplaceModule("txt", "md", Part: NamePart.Extension).Apply(input);

        Assert.Equal(["b.md", "a.md"], output.Names());
        Assert.Equal(input.Select(i => i.FileId), output.Select(i => i.FileId));
    }

    [Fact]
    public void Invalid_regex_is_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ReplaceModule("(", "x", UseRegex: true).Apply(TestItems.Of("a")));
    }
}
