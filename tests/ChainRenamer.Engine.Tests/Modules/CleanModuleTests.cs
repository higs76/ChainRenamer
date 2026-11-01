using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class CleanModuleTests
{
    private static string Clean(CleanModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    private static readonly CleanModule Nothing = new(TrimSpaces: false, CollapseSpaces: false);

    [Fact]
    public void By_default_trims_and_collapses_spaces()
    {
        Assert.Equal("Speedy 01 final.mkv", Clean(new(), "  Speedy   01  final .mkv"));
        Assert.Equal("  Speedy   01 .mkv", Clean(Nothing, "  Speedy   01 .mkv"));
    }

    [Fact]
    public void Removes_accents_and_splits_ligatures()
    {
        Assert.Equal("Ete a Noel, coeur, Strasse.mkv", Clean(Nothing with { RemoveAccents = true }, "Été à Noël, cœur, Straße.mkv"));
        Assert.Equal("Ca va.MKV", Clean(Nothing with { RemoveAccents = true }, "Ça va.MKV"));
    }

    [Fact]
    public void Removes_digits()
    {
        Assert.Equal("Speedy x.mkv", Clean(new(RemoveDigits: true), "Speedy 01x03.mkv"));
    }

    [Fact]
    public void Removes_symbols_and_emojis()
    {
        Assert.Equal("Movie (2019) - Final.mkv", Clean(new(RemoveSymbols: true), "Movie™ (2019) - Final ©.mkv"));
        Assert.Equal("Holiday.jpg", Clean(new(RemoveEmojis: true), "Holiday 🏖️👨‍👩‍👧.jpg"));
        Assert.Equal("Price €.jpg", Clean(new(RemoveEmojis: true), "Price € 😀.jpg"));    // emojis only: the euro stays
    }

    [Fact]
    public void Removes_leading_dots_even_of_a_name_without_stem()
    {
        Assert.Equal("hidden.txt", Clean(new(LeadingDots: true), "..hidden.txt"));
        Assert.Equal("config", Clean(new(LeadingDots: true), ".config"));
        Assert.Equal(".config", Clean(new(), ".config"));
    }
}
