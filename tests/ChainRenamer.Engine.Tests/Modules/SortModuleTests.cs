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
    public void Keeps_the_order_of_equal_names()
    {
        var input = TestItems.Of("same", "SAME", "Same");

        Assert.Equal(input.Select(i => i.FileId), new SortModule().Apply(input).Select(i => i.FileId));
    }
}
