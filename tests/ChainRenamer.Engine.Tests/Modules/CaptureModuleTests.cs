using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class CaptureModuleTests
{
    private const string SeasonEpisode = @"(?<season>\d+)x(?<episode>\d+)";

    [Fact]
    public void Captures_named_groups_without_changing_the_name()
    {
        var item = new CaptureModule(SeasonEpisode).Apply(TestItems.Of("Speedy 1x03 Rival.mkv"))[0];

        Assert.Equal("Speedy 1x03 Rival.mkv", item.Name);
        Assert.Equal("1", item.Variables["season"]);
        Assert.Equal("03", item.Variables["Episode"]);     // names ignore case
    }

    [Fact]
    public void Following_steps_use_the_variables()
    {
        var captured = new CaptureModule(SeasonEpisode).Apply(TestItems.Of("Speedy 1x03 Rival.mkv"));
        var renamed = new TemplateModule("S{season}E{episode}").Apply(captured);

        Assert.Equal(["S1E03.mkv"], renamed.Names());
    }

    [Fact]
    public void A_name_that_does_not_match_gets_no_variable()
    {
        var items = new CaptureModule(SeasonEpisode).Apply(TestItems.Of("Trailer.mkv"));

        Assert.Empty(items[0].Variables);
        Assert.Equal(["S{season}.mkv"], new TemplateModule("S{season}").Apply(items).Names());
    }

    [Fact]
    public void A_later_capture_overwrites_a_variable()
    {
        var items = new CaptureModule("(?<x>a)").Apply(TestItems.Of("ab.txt"));
        items = new CaptureModule("(?<x>b)").Apply(items);

        Assert.Equal("b", items[0].Variables["x"]);
    }

    [Fact]
    public void Lists_the_variables_it_creates()
    {
        Assert.Equal(["season", "episode"], new CaptureModule(SeasonEpisode).VariableNames());
        Assert.Empty(new CaptureModule("(?<unclosed").VariableNames());
        Assert.Empty(new CaptureModule(@"(\d+)").VariableNames());
    }

    [Fact]
    public void Numbering_can_start_again_for_each_value_of_a_variable()
    {
        var items = new CaptureModule(@"S(?<season>\d+)").Apply(TestItems.Of("S1 a.mkv", "S1 b.mkv", "S2 c.mkv", "S2 d.mkv"));

        Assert.Equal(["S1 a - 01.mkv", "S1 b - 02.mkv", "S2 c - 01.mkv", "S2 d - 02.mkv"],
            new NumberingModule(Where: InsertAt.End, ResetOn: "season").Apply(items).Names());
        Assert.Equal(["S1 a - 01.mkv", "S1 b - 02.mkv", "S2 c - 01.mkv", "S2 d - 02.mkv"],
            new NumberingModule(Where: InsertAt.End, ResetOn: "{season}").Apply(items).Names());
    }

    [Fact]
    public void Numbering_can_start_again_each_year()
    {
        IReadOnlyList<Item> photos =
        [
            new(FileId.New(), "a.jpg") { Modified = new(2023, 12, 31) },
            new(FileId.New(), "b.jpg") { Modified = new(2024, 1, 1) },
            new(FileId.New(), "c.jpg") { Modified = new(2024, 6, 1) },
        ];

        Assert.Equal(["01 - a.jpg", "01 - b.jpg", "02 - c.jpg"], new NumberingModule(ResetOn: "{date:yyyy}").Apply(photos).Names());
    }
}
