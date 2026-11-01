using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class FilterModuleTests
{
    private static string[] Filter(FilterModule module, params string[] names) => module.Apply(TestItems.Of(names)).Names();

    private static readonly string[] Episodes = ["Speedy 01.mkv", "Speedy 02.MKV", "speedy 03.mp4", "Notes.txt"];

    [Theory]
    [InlineData(TextTest.Contains, "speedy", new[] { "Speedy 01.mkv", "Speedy 02.MKV", "speedy 03.mp4" })]
    [InlineData(TextTest.StartsWith, "notes", new[] { "Notes.txt" })]
    [InlineData(TextTest.EndsWith, ".mkv", new[] { "Speedy 01.mkv", "Speedy 02.MKV" })]
    [InlineData(TextTest.Wildcard, "*0?.mkv;*.txt", new[] { "Speedy 01.mkv", "Speedy 02.MKV", "Notes.txt" })]
    [InlineData(TextTest.Regex, @"0[23]\.", new[] { "Speedy 02.MKV", "speedy 03.mp4" })]
    public void Keeps_the_names_that_match(TextTest test, string text, string[] expected) =>
        Assert.Equal(expected, Filter(new(FilterOn.Name, test, text), Episodes));

    [Fact]
    public void Can_match_case()
    {
        Assert.Equal(["Speedy 01.mkv", "Speedy 02.MKV"], Filter(new(Test: TextTest.StartsWith, Text: "Speedy", MatchCase: true), Episodes));
        Assert.Equal(["Speedy 01.mkv"], Filter(new(Test: TextTest.Wildcard, Text: "*.mkv", MatchCase: true), Episodes));
    }

    [Fact]
    public void A_wildcard_covers_the_whole_name()
    {
        // Not "contains": *.mkv does not keep a name that only has .mkv in the middle.
        Assert.Equal(["a.mkv"], Filter(new(Test: TextTest.Wildcard, Text: "*.mkv"), "a.mkv", "a.mkv.part"));
        Assert.Equal(["a+b (1).txt"], Filter(new(Test: TextTest.Wildcard, Text: "a+b (?).txt"), "a+b (1).txt", "ab (1).txt"));
    }

    [Fact]
    public void Exclude_removes_what_matches()
    {
        Assert.Equal(["Notes.txt"], Filter(new(Text: "speedy", Exclude: true), Episodes));
    }

    [Theory]
    [InlineData("mkv")]
    [InlineData(".MKV")]
    [InlineData("mkv, mp4")]
    [InlineData("mkv;.mp4 avi")]
    public void Keeps_the_extensions_of_a_list(string list)
    {
        var expected = list.Contains("mp4") ? Episodes[..3] : Episodes[..2];
        Assert.Equal(expected, Filter(new(FilterOn.Extension, Text: list), Episodes));
    }

    [Fact]
    public void A_name_without_extension_is_not_in_any_list()
    {
        Assert.Equal(["a.txt"], Filter(new(FilterOn.Extension, Text: "txt"), "README", "a.txt"));
    }

    [Fact]
    public void An_empty_filter_keeps_everything()
    {
        Assert.Equal(Episodes, Filter(new(), Episodes));
        Assert.Equal(Episodes, Filter(new(Exclude: true), Episodes));
        Assert.Equal(Episodes, Filter(new(FilterOn.Extension, Text: " , "), Episodes));
        Assert.Equal(Episodes, Filter(new(FilterOn.Size), Episodes));
        Assert.Equal(Episodes, Filter(new(FilterOn.Modified), Episodes));
    }

    private static readonly IReadOnlyList<Item> Sized =
    [
        new(FileId.New(), "small.jpg") { Size = 300 * 1024, Modified = new(2024, 5, 16, 23, 59, 0) },
        new(FileId.New(), "medium.jpg") { Size = 2 * 1024 * 1024, Modified = new(2024, 5, 17, 8, 0, 0) },
        new(FileId.New(), "large.mkv") { Size = 3L * 1024 * 1024 * 1024, Modified = new(2024, 5, 18, 0, 0, 0) },
    ];

    [Theory]
    [InlineData(1.0, null, SizeUnit.MB, new[] { "medium.jpg", "large.mkv" })]
    [InlineData(null, 300.0, SizeUnit.KB, new[] { "small.jpg" })]
    [InlineData(1.0, 2.0, SizeUnit.MB, new[] { "medium.jpg" })]
    [InlineData(2.0, 1.0, SizeUnit.MB, new[] { "medium.jpg" })]    // bounds in any order
    [InlineData(2.5, null, SizeUnit.GB, new[] { "large.mkv" })]
    public void Keeps_the_sizes_in_range(double? min, double? max, SizeUnit unit, string[] expected) =>
        Assert.Equal(expected, new FilterModule(FilterOn.Size, Min: min, Max: max, Unit: unit).Apply(Sized).Names());

    [Fact]
    public void Dates_count_whole_days()
    {
        var day = new DateOnly(2024, 5, 17);

        Assert.Equal(["medium.jpg"], new FilterModule(FilterOn.Modified, From: day, To: day).Apply(Sized).Names());
        Assert.Equal(["medium.jpg", "large.mkv"], new FilterModule(FilterOn.Modified, From: day).Apply(Sized).Names());
        Assert.Equal(["small.jpg"], new FilterModule(FilterOn.Modified, From: day, Exclude: true).Apply(Sized).Names());
    }

    [Fact]
    public void Length_counts_the_whole_name()
    {
        Assert.Equal(["abc.txt"], Filter(new(FilterOn.Length, Min: 7, Max: 7), "abc.txt", "ab.txt", "abcd.txt"));
        Assert.Equal(["ab.txt"], Filter(new(FilterOn.Length, Max: 6), "abc.txt", "ab.txt"));
    }

    [Fact]
    public void Keeps_the_order_and_the_items()
    {
        var input = TestItems.Of("b.mkv", "a.txt", "c.mkv");
        var output = new FilterModule(FilterOn.Extension, Text: "mkv").Apply(input);

        Assert.Equal([input[0], input[2]], output);
    }

    [Fact]
    public void An_invalid_regex_throws_so_that_the_step_reports_it()
    {
        Assert.ThrowsAny<ArgumentException>(() => Filter(new(Test: TextTest.Regex, Text: "(unclosed"), Episodes));
    }
}
