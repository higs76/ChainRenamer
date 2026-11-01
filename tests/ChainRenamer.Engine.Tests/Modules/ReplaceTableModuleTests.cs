using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class ReplaceTableModuleTests
{
    private static string Replace(ReplaceTableModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    private static ReplaceTableModule Table(params ReplacePair[] pairs) => new([.. pairs]);

    private static ReplacePair P(string find, string replaceWith, bool enabled = true) => new(find, replaceWith, enabled);

    [Fact]
    public void Applies_every_pair_in_order()
    {
        var table = Table(P("VOSTFR", "VOST"), P("S01", "Saison 1"), P("Saison 1", "Season 1"));

        Assert.Equal("Speedy Season 1 VOST.mkv", Replace(table, "Speedy S01 VOSTFR.mkv"));
    }

    [Fact]
    public void Ignores_case_unless_asked()
    {
        Assert.Equal("Strasse.txt", Replace(Table(P("ß", "ss"), P("STRA", "Stra")), "straße.txt"));
        Assert.Equal("Speedy vostfr.mkv", Replace(Table(P("VOSTFR", "VOST")) with { MatchCase = true }, "Speedy vostfr.mkv"));
    }

    [Fact]
    public void A_disabled_or_empty_line_is_skipped()
    {
        var table = Table(P("a", "b", enabled: false), P("", "x"), P("c", "d"));

        Assert.Equal("add.txt", Replace(table, "acc.txt"));
    }

    [Fact]
    public void Never_touches_the_extension()
    {
        Assert.Equal("photo.jpg", Replace(Table(P("jpg", "png")), "photo.jpg"));
    }

    [Fact]
    public void Reads_a_pasted_table()
    {
        // ToArray: xUnit compares ImmutableArrays as values (by reference), not item by item.
        var pairs = ReplaceTableModule.Parse("ß\tss\r\nVOSTFR → VOST\nS01->Saison 1\n\n[HD]\nœ=oe\n").ToArray();

        Assert.Equal(
            [P("ß", "ss"), P("VOSTFR", "VOST"), P("S01", "Saison 1"), P("[HD]", ""), P("œ", "oe")],
            pairs);
    }

    [Fact]
    public void Keeps_the_spaces_that_belong_to_the_texts()
    {
        Assert.Equal([P(" - ", " ")], ReplaceTableModule.Parse(" - \t ").ToArray());
    }

    [Fact]
    public void Format_is_read_back_by_parse()
    {
        ReplacePair[] pairs = [P("a", "b"), P(" x ", ""), P("→", "->")];

        Assert.Equal(pairs, ReplaceTableModule.Parse(ReplaceTableModule.Format(pairs)).ToArray());
    }

    [Fact]
    public void Two_tables_with_the_same_lines_are_equal()
    {
        Assert.Equal(Table(P("a", "b")), Table(P("a", "b")));
        Assert.NotEqual(Table(P("a", "b")), Table(P("a", "c")));
    }
}
