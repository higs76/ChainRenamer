using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace ChainRenamer.App.Tests;

/// <summary>Every text of the interface and of the help exists in English and in French.</summary>
public class ResourceTests
{
    private static string Resources([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "src", "ChainRenamer.App", "Resources"));

    private static Dictionary<string, string> Read(string file) =>
        XDocument.Load(Path.Combine(Resources(), file)).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? "");

    [Theory]
    [InlineData("Strings")]
    [InlineData("HelpTexts")]
    public void French_and_english_have_the_same_texts(string name)
    {
        var english = Read(name + ".resx");
        var french = Read(name + ".fr.resx");

        Assert.Empty(english.Keys.Except(french.Keys));
        Assert.Empty(french.Keys.Except(english.Keys));
        Assert.All(english.Concat(french), t => Assert.False(string.IsNullOrWhiteSpace(t.Value), t.Key));
    }
}
