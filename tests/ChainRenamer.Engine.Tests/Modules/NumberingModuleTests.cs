using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class NumberingModuleTests
{
    private static string[] Number(NumberingModule module, params string[] names) => module.Apply(TestItems.Of(names)).Names();

    [Fact]
    public void Numbers_the_files_in_their_order_at_the_start_by_default() =>
        Assert.Equal(["01 - b.jpg", "02 - a.jpg", "03 - c.jpg"], Number(new(), "b.jpg", "a.jpg", "c.jpg"));

    [Fact]
    public void Start_step_digits_and_separator()
    {
        Assert.Equal(["010_a.jpg", "015_b.jpg"], Number(new(Start: 10, Step: 5, Digits: 3, Separator: "_"), "a.jpg", "b.jpg"));
        Assert.Equal(["3 a.jpg", "2 b.jpg", "1 c.jpg"], Number(new(Start: 3, Step: -1, Digits: 1, Separator: " "), "a.jpg", "b.jpg", "c.jpg"));
    }

    [Fact]
    public void Can_go_at_the_end_or_after_a_text()
    {
        Assert.Equal(["Holiday 1.jpg", "Holiday 2.jpg"], Number(new(Digits: 1, Where: InsertAt.End, Separator: " "), "Holiday.jpg", "Holiday.jpg"));
        Assert.Equal(["Speedy E01 - Start.mkv", "Speedy E02 - End.mkv"], Number(new(Where: InsertAt.After, Anchor: "Speedy E"), "Speedy E - Start.mkv", "Speedy E - End.mkv"));
    }

    [Theory]
    [InlineData(NumberStyle.LowerLetters, "a", "b", "z", "aa", "ab")]
    [InlineData(NumberStyle.UpperLetters, "A", "B", "Z", "AA", "AB")]
    [InlineData(NumberStyle.UpperRoman, "I", "II", "XXVI", "XXVII", "XXVIII")]
    [InlineData(NumberStyle.LowerRoman, "i", "ii", "xxvi", "xxvii", "xxviii")]
    public void Letters_and_roman_numerals(NumberStyle style, params string[] expected)
    {
        var names = Enumerable.Range(1, 28).Select(i => $"f{i}.txt").ToArray();
        var numbers = new NumberingModule(Style: style, Separator: " ").Apply(TestItems.Of(names)).Names().Select(n => n.Split(' ')[0]).ToArray();

        Assert.Equal(expected, new[] { 0, 1, 25, 26, 27 }.Select(i => numbers[i]));
    }

    [Fact]
    public void Can_start_again_in_each_folder()
    {
        var items = new[] { ("S1", "a.mkv"), ("S1", "b.mkv"), ("S2", "c.mkv"), ("S2", "d.mkv") }
            .Select(f => new Item(FileId.New(), f.Item2) { Folder = f.Item1 }).ToList();

        Assert.Equal(["01 - a.mkv", "02 - b.mkv", "01 - c.mkv", "02 - d.mkv"], new NumberingModule(ResetPerFolder: true).Apply(items).Names());
        Assert.Equal(["01 - a.mkv", "02 - b.mkv", "03 - c.mkv", "04 - d.mkv"], new NumberingModule().Apply(items).Names());
    }

    [Fact]
    public void The_pipeline_gives_each_item_its_folder()
    {
        var pipeline = new Pipeline(new SourceModule(@"C:\Series"));
        pipeline.SetFiles([FileItem.FromPath(@"C:\Series\S1\a.mkv"), FileItem.FromPath(@"C:\Series\S2\b.mkv")]);
        pipeline.Add(new NumberingModule(ResetPerFolder: true));

        Assert.Equal(["01 - a.mkv", "01 - b.mkv"], pipeline.Output.Names());
    }
}
