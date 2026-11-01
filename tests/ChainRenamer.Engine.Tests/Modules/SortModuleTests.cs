using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class SortModuleTests
{
    [Fact]
    public void Sorts_numbers_by_value()
    {
        var input = TestItems.Of("Speedy - 01x10.mkv", "Speedy - 01x02.mkv", "Speedy - 01x01.mkv");

        Assert.Equal(["Speedy - 01x01.mkv", "Speedy - 01x02.mkv", "Speedy - 01x10.mkv"], new SortModule().Apply(input).Names());
    }

    [Fact]
    public void Sorts_numbers_of_different_lengths()
    {
        var input = TestItems.Of("ep 10.txt", "ep 9.txt", "ep 100.txt", "ep 1.txt");

        Assert.Equal(["ep 1.txt", "ep 9.txt", "ep 10.txt", "ep 100.txt"], new SortModule().Apply(input).Names());
    }

    [Fact]
    public void Ignores_case()
    {
        var input = TestItems.Of("b.txt", "A.txt", "c.txt");

        Assert.Equal(["A.txt", "b.txt", "c.txt"], new SortModule().Apply(input).Names());
    }

    [Fact]
    public void Can_sort_descending()
    {
        var input = TestItems.Of("ep 2", "ep 10", "ep 1");

        Assert.Equal(["ep 10", "ep 2", "ep 1"], new SortModule(Descending: true).Apply(input).Names());
    }

    [Fact]
    public void Manual_order_overrides_the_natural_order()
    {
        // speedy 14 is in fact episode 3: the user puts it before speedy 4.
        var input = TestItems.Of("speedy 1", "speedy 2", "speedy 4", "speedy 5", "speedy 14");
        var natural = new SortModule().Apply(input);

        var order = SortModule.Move(natural, natural[4].FileId, -2);
        var output = new SortModule { ManualOrder = order }.Apply(input);

        Assert.Equal(["speedy 1", "speedy 2", "speedy 14", "speedy 4", "speedy 5"], output.Names());
    }

    [Fact]
    public void Files_missing_from_the_manual_order_follow_in_natural_order()
    {
        var input = TestItems.Of("c", "a", "b", "z");
        var module = new SortModule { ManualOrder = [input[2].FileId, input[0].FileId] };

        Assert.Equal(["b", "c", "a", "z"], module.Apply(input).Names());
    }

    [Fact]
    public void Manual_order_ignores_files_no_longer_present()
    {
        var input = TestItems.Of("a", "b");
        var module = new SortModule { ManualOrder = [FileId.New(), input[1].FileId, input[0].FileId] };

        Assert.Equal(["b", "a"], module.Apply(input).Names());
    }

    [Theory]
    [InlineData(1, -1, new[] { "b", "a", "c" })]
    [InlineData(0, 1, new[] { "b", "a", "c" })]
    [InlineData(0, -1, new[] { "a", "b", "c" })]
    [InlineData(2, 5, new[] { "a", "b", "c" })]
    [InlineData(2, -2, new[] { "c", "a", "b" })]
    public void Move_shifts_one_file_and_stays_within_bounds(int index, int offset, string[] expected)
    {
        var items = TestItems.Of("a", "b", "c");
        var names = items.ToDictionary(i => i.FileId, i => i.Name);

        var order = SortModule.Move(items, items[index].FileId, offset);

        Assert.Equal(expected, order.Select(id => names[id]));
    }

    [Fact]
    public void Keeps_the_order_of_equal_names()
    {
        var input = TestItems.Of("same", "SAME", "Same");

        Assert.Equal(input.Select(i => i.FileId), new SortModule().Apply(input).Select(i => i.FileId));
    }

    private static readonly IReadOnlyList<Item> Photos =
    [
        new(FileId.New(), "b.jpg") { Size = 300, Modified = new(2024, 3, 1), Metadata = Taken("2020-06-01T10:00:00") },
        new(FileId.New(), "a.jpg") { Size = 100, Modified = new(2024, 1, 1) },     // no shooting date: its modification date
        new(FileId.New(), "c.jpg") { Size = 200, Modified = new(2024, 2, 1), Metadata = Taken("2022-06-01T10:00:00") },
    ];

    private static System.Collections.Immutable.ImmutableDictionary<string, string> Taken(string date) =>
        System.Collections.Immutable.ImmutableDictionary<string, string>.Empty.Add("taken", date);

    [Theory]
    [InlineData(SortBy.Modified, false, new[] { "a.jpg", "c.jpg", "b.jpg" })]
    [InlineData(SortBy.Taken, false, new[] { "b.jpg", "c.jpg", "a.jpg" })]
    [InlineData(SortBy.Size, false, new[] { "a.jpg", "c.jpg", "b.jpg" })]
    [InlineData(SortBy.Size, true, new[] { "b.jpg", "c.jpg", "a.jpg" })]
    public void Sorts_by_date_or_size(SortBy by, bool descending, string[] expected) =>
        Assert.Equal(expected, new SortModule(descending, by).Apply(Photos).Names());

    [Fact]
    public void Equal_values_keep_the_natural_order_of_names()
    {
        var input = TestItems.Of("Episode 10.mkv", "Episode 2.mkv");

        Assert.Equal(["Episode 2.mkv", "Episode 10.mkv"], new SortModule(By: SortBy.Size).Apply(input).Names());
    }
}
