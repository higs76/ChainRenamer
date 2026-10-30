using ChainRenamer.Engine.Renaming;

namespace ChainRenamer.Engine.Tests.Renaming;

public class NameRulesTests
{
    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("Speedy - S01E01 - Remove The Limiter!.mkv")]
    [InlineData(".gitignore")]
    [InlineData("CONSOLE.txt")]
    [InlineData("COM10")]
    [InlineData("été à l'école.txt")]
    public void Accepts_valid_names(string name)
    {
        Assert.Null(NameRules.Check(name));
    }

    [Theory]
    [InlineData("", ConflictKind.EmptyName)]
    [InlineData("a<b", ConflictKind.InvalidCharacter)]
    [InlineData("a:b", ConflictKind.InvalidCharacter)]
    [InlineData("a\"b", ConflictKind.InvalidCharacter)]
    [InlineData("a/b", ConflictKind.InvalidCharacter)]
    [InlineData(@"a\b", ConflictKind.InvalidCharacter)]
    [InlineData("a|b", ConflictKind.InvalidCharacter)]
    [InlineData("a?b", ConflictKind.InvalidCharacter)]
    [InlineData("a*b", ConflictKind.InvalidCharacter)]
    [InlineData("a\tb", ConflictKind.InvalidCharacter)]
    [InlineData("name.", ConflictKind.TrailingDotOrSpace)]
    [InlineData("name ", ConflictKind.TrailingDotOrSpace)]
    [InlineData("CON", ConflictKind.ReservedName)]
    [InlineData("nul.txt", ConflictKind.ReservedName)]
    [InlineData("Com1.tar.gz", ConflictKind.ReservedName)]
    [InlineData("LPT9", ConflictKind.ReservedName)]
    [InlineData("aux .txt", ConflictKind.ReservedName)]
    public void Rejects_invalid_names(string name, ConflictKind expected)
    {
        Assert.Equal(expected, NameRules.Check(name));
    }
}
