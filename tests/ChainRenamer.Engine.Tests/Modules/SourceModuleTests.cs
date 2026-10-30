using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class SourceModuleTests
{
    private readonly IReadOnlyList<Item> input = TestItems.Of("a.txt", "b.txt", "c.txt");

    [Fact]
    public void Keeps_every_file_by_default()
    {
        Assert.Same(input, new SourceModule(@"C:\folder").Apply(input));
    }

    [Fact]
    public void Leaves_out_unchecked_files_and_keeps_the_order()
    {
        var module = new SourceModule(@"\\server\share\folder") { Excluded = [input[1].FileId] };

        Assert.Equal(["a.txt", "c.txt"], module.Apply(input).Names());
    }
}
