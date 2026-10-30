namespace ChainRenamer.Engine.Tests;

internal static class TestItems
{
    public static IReadOnlyList<Item> Of(params string[] names) => [.. names.Select(n => new Item(FileId.New(), n))];

    public static string[] Names(this IReadOnlyList<Item> items) => [.. items.Select(i => i.Name)];
}
