using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class ListModuleTests
{
    private static readonly string[] Episodes = ["Speedy 01.mkv", "Speedy 02.mkv", "Speedy 03.mkv"];

    private static string[] Apply(ListModule module, params string[] names) => module.Apply(TestItems.Of(names)).Names();

    [Fact]
    public void Line_n_becomes_the_name_of_file_n()
    {
        Assert.Equal(["The First Lap.mkv", "Rain on the Track.mkv", "A New Rival.mkv"],
            Apply(new("The First Lap\r\nRain on the Track\nA New Rival\n\n"), Episodes));
    }

    [Fact]
    public void Missing_or_empty_lines_keep_the_name()
    {
        Assert.Equal(["One.mkv", "Speedy 02.mkv", "Speedy 03.mkv"], Apply(new("One"), Episodes));
        Assert.Equal(["One.mkv", "Speedy 02.mkv", "Three.mkv"], Apply(new("One\n\nThree\nFour"), Episodes));
        Assert.Equal(Episodes, Apply(new(), Episodes));
    }

    [Fact]
    public void Inserts_line_n_in_the_name_of_file_n()
    {
        Assert.Equal(["Speedy 01 - The First Lap.mkv", "Speedy 02 - Rain on the Track.mkv", "Speedy 03.mkv"],
            Apply(new(" - The First Lap\n - Rain on the Track", ListMode.Insert), Episodes));
        Assert.Equal(["Speedy S1 01.mkv"], Apply(new("S1 ", ListMode.Insert, InsertAt.After, Anchor: "Speedy "), "Speedy 01.mkv"));
    }

    [Fact]
    public void Lines_may_hold_variables()
    {
        Assert.Equal(["01 - Start.mkv", "02 - Race.mkv"], Apply(new("{n:2} - Start\n{n:2} - Race"), "a.mkv", "b.mkv"));
    }

    [Fact]
    public void Pairs_find_the_files_by_their_name()
    {
        var list = new ListModule("Speedy 03.mkv\tFinal.mkv\nspeedy 01 → Start\nUnknown → X", ListMode.Pairs);

        Assert.Equal(["Start.mkv", "Speedy 02.mkv", "Final.mkv"], Apply(list, Episodes));
        Assert.Equal(["Unknown"], list.Unmatched(TestItems.Of(Episodes)));
    }

    [Fact]
    public void Pairs_can_match_case()
    {
        var list = new ListModule("speedy 01\tStart", ListMode.Pairs, MatchCase: true);

        Assert.Equal(Episodes, Apply(list, Episodes));
        Assert.Equal(["speedy 01"], list.Unmatched(TestItems.Of(Episodes)));
    }
}
