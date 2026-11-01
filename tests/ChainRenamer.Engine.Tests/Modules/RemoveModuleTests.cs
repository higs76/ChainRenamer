using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class RemoveModuleTests
{
    private static string Remove(RemoveModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    [Fact]
    public void Removes_the_first_or_last_characters_but_not_the_extension()
    {
        Assert.Equal("Speedy 01.mkv", Remove(new(RemoveWhat.First, Count: 4), "001 Speedy 01.mkv"));
        Assert.Equal("Speedy.mkv", Remove(new(RemoveWhat.Last, Count: 3), "Speedy 01.mkv"));
        Assert.Equal(".mkv", Remove(new(RemoveWhat.First, Count: 50), "Speedy.mkv"));
        Assert.Equal("Speedy.mkv", Remove(new(RemoveWhat.First, Count: 0), "Speedy.mkv"));
    }

    [Theory]
    [InlineData(1, 4, "Speedy 01")]
    [InlineData(4, 1, "Speedy 01")]        // in any order
    [InlineData(-3, -1, "001 Speedy")]     // from the end
    [InlineData(5, -4, "001  01")]    // -4 is the "y" of Speedy
    [InlineData(20, 30, "001 Speedy 01")]  // outside the name: nothing
    public void Removes_a_range_of_positions(int from, int to, string expected) =>
        Assert.Equal(expected + ".mkv", Remove(new(RemoveWhat.Range, From: from, To: to), "001 Speedy 01.mkv"));

    [Fact]
    public void Removes_before_or_after_a_text()
    {
        Assert.Equal("Title.mp3", Remove(new(RemoveWhat.Before, Text: " - "), "Artist - Title.mp3"));
        Assert.Equal(" - Title.mp3", Remove(new(RemoveWhat.Before, Text: " - ", IncludeText: false), "Artist - Title.mp3"));
        Assert.Equal("Speedy 01.mkv", Remove(new(RemoveWhat.After, Text: " ["), "Speedy 01 [1080p] [x265].mkv"));
        Assert.Equal("Show - 01.mkv", Remove(new(RemoveWhat.After, Text: " - ", LastOccurrence: true), "Show - 01 - Title.mkv"));
        Assert.Equal("Show - 01 - Title.mkv", Remove(new(RemoveWhat.After, Text: " ~ "), "Show - 01 - Title.mkv"));
    }

    [Fact]
    public void Removes_every_part_between_two_texts()
    {
        Assert.Equal("Speedy 01  .mkv", Remove(new(RemoveWhat.Between, Text: "[", EndText: "]"), "Speedy 01 [1080p] [x265].mkv"));
        Assert.Equal("Speedy 01 [] [].mkv", Remove(new(RemoveWhat.Between, Text: "[", EndText: "]", IncludeText: false), "Speedy 01 [1080p] [x265].mkv"));
        Assert.Equal("Film  [unclosed.mkv", Remove(new(RemoveWhat.Between, Text: "(", EndText: ")"), "Film (VOSTFR) [unclosed.mkv"));
    }

    [Fact]
    public void Can_match_case()
    {
        Assert.Equal("Show vostfr.mkv", Remove(new(RemoveWhat.After, Text: " VOST", MatchCase: true), "Show vostfr.mkv"));
        Assert.Equal("Show.mkv", Remove(new(RemoveWhat.After, Text: " VOST"), "Show vostfr.mkv"));
    }
}
