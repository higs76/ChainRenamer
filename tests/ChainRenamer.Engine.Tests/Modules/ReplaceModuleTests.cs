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
    public void Can_replace_the_first_occurrence_only()
    {
        Assert.Equal("a_b c.txt", Replace(new(" ", "_", Occurrence: Occurrence.First), "a b c.txt"));
        Assert.Equal("x-EP-ep.mkv", Replace(new("ep", "x", Occurrence: Occurrence.First), "EP-EP-ep.mkv"));
    }

    [Fact]
    public void Can_replace_the_last_occurrence_only()
    {
        Assert.Equal("a b_c.txt", Replace(new(" ", "_", Occurrence: Occurrence.Last), "a b c.txt"));
        Assert.Equal("EP-x-ep.mkv", Replace(new("EP", "x", MatchCase: true, Occurrence: Occurrence.Last), "EP-EP-ep.mkv"));
    }

    [Fact]
    public void First_or_last_occurrence_not_found_changes_nothing()
    {
        Assert.Equal("abc.txt", Replace(new("z", "x", Occurrence: Occurrence.First), "abc.txt"));
        Assert.Equal("abc.txt", Replace(new("z", "x", Occurrence: Occurrence.Last), "abc.txt"));
    }

    [Fact]
    public void Never_touches_the_extension()
    {
        Assert.Equal("photo-mkv.mkv", Replace(new(".", "-"), "photo.mkv.mkv"));
        Assert.Equal("jpg.jpeg", Replace(new("jpeg", "jpg", Occurrence: Occurrence.Last), "jpeg.jpeg"));
    }

    [Fact]
    public void Takes_regex_characters_literally()
    {
        Assert.Equal("a(1).txt", Replace(new("[1]", "(1)"), "a[1].txt"));
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

        var output = new ReplaceModule("a", "c").Apply(input);

        Assert.Equal(["b.txt", "c.txt"], output.Names());
        Assert.Equal(input.Select(i => i.FileId), output.Select(i => i.FileId));
    }
}
