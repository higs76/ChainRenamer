using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class RegexReplaceModuleTests
{
    private static string Replace(RegexReplaceModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    [Fact]
    public void Replacement_can_use_groups()
    {
        var module = new RegexReplaceModule(@"(\d+)x(\d+)", "S$1E$2");

        Assert.Equal("Speedy - S01E10 - Time Attack!.mkv", Replace(module, "Speedy - 01x10 - Time Attack!.mkv"));
    }

    [Fact]
    public void Ignores_case_unless_asked()
    {
        Assert.Equal("x-x.txt", Replace(new("[a-z]+", "x"), "abc-ABC.txt"));
        Assert.Equal("x-ABC.txt", Replace(new("[a-z]+", "x", MatchCase: true), "abc-ABC.txt"));
    }

    [Fact]
    public void Never_touches_the_extension()
    {
        Assert.Equal("x.mkv", Replace(new("[a-z]+", "x"), "Speedy.mkv"));
    }

    [Fact]
    public void Empty_pattern_changes_nothing()
    {
        var input = TestItems.Of("a.txt");

        Assert.Same(input, new RegexReplaceModule("", "x").Apply(input));
    }

    [Fact]
    public void Invalid_pattern_is_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => new RegexReplaceModule("(", "x").Apply(TestItems.Of("a")));
    }
}
