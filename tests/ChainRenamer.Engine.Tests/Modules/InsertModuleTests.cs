using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class InsertModuleTests
{
    private static string Insert(InsertModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    [Theory]
    [InlineData(InsertAt.Start, "[HD] Speedy 01.mkv")]
    [InlineData(InsertAt.End, "Speedy 01[HD] .mkv")]
    public void Inserts_at_the_start_or_before_the_extension(InsertAt where, string expected) =>
        Assert.Equal(expected, Insert(new("[HD] ", where), "Speedy 01.mkv"));

    [Theory]
    [InlineData(0, "-Speedy01.mkv")]
    [InlineData(6, "Speedy-01.mkv")]
    [InlineData(-2, "Speedy-01.mkv")]
    [InlineData(50, "Speedy01-.mkv")]
    [InlineData(-50, "-Speedy01.mkv")]
    public void Inserts_after_a_number_of_characters(int position, string expected) =>
        Assert.Equal(expected, Insert(new("-", InsertAt.Position, position), "Speedy01.mkv"));

    [Fact]
    public void Inserts_before_or_after_a_text()
    {
        Assert.Equal("Speedy S01E03 - Title.mkv", Insert(new("S01", InsertAt.Before, Anchor: "e03"), "Speedy E03 - Title.mkv"));
        Assert.Equal("Speedy E03 - Title.mkv", Insert(new("S01", InsertAt.Before, Anchor: "e03", MatchCase: true), "Speedy E03 - Title.mkv"));
        Assert.Equal("Artist - (Live) Title.mp3", Insert(new("(Live) ", InsertAt.After, Anchor: " - "), "Artist - Title.mp3"));
        Assert.Equal("Title.mp3", Insert(new("x", InsertAt.After, Anchor: " - "), "Title.mp3"));
    }

    [Fact]
    public void An_empty_text_changes_nothing() =>
        Assert.Equal("Speedy.mkv", Insert(new("", InsertAt.Start), "Speedy.mkv"));
}
