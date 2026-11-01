using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class CaseModuleTests
{
    private static string Change(CaseModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    [Theory]
    [InlineData(TextCase.Lower, "the matrix reloaded.MKV")]
    [InlineData(TextCase.Upper, "THE MATRIX RELOADED.MKV")]
    [InlineData(TextCase.Title, "The Matrix Reloaded.MKV")]
    [InlineData(TextCase.Sentence, "The matrix reloaded.MKV")]
    public void Changes_the_case_but_not_the_extension(TextCase textCase, string expected) =>
        Assert.Equal(expected, Change(new(textCase, KeepAcronyms: false), "the MATRIX reloaded.MKV"));

    [Fact]
    public void Words_are_separated_by_anything_but_letters_and_digits()
    {
        Assert.Equal("Speedy.01x03.My_First-Race.mkv", Change(new(), "speedy.01x03.my_first-race.mkv"));
        Assert.Equal("Été Indien.mkv", Change(new(), "été indien.mkv"));
    }

    [Fact]
    public void Small_words_stay_in_lowercase_except_first_and_last()
    {
        Assert.Equal("The Lord of the Rings.mkv", Change(new(SmallWords: SmallWords.English), "the lord of the rings.mkv"));
        Assert.Equal("Le Seigneur des Anneaux.mkv", Change(new(SmallWords: SmallWords.French), "le seigneur des anneaux.mkv"));
        Assert.Equal("Give Up the Fight.mkv", Change(new(SmallWords: SmallWords.English), "give up the fight.mkv"));
        Assert.Equal("Of Mice and Men.mkv", Change(new(SmallWords: SmallWords.English), "of mice and men.mkv"));
        Assert.Equal("The Lord Of The Rings.mkv", Change(new(), "the lord of the rings.mkv"));     // no language: every word
    }

    [Fact]
    public void French_elision_capitalizes_the_word_after_the_apostrophe()
    {
        Assert.Equal("Le Retour de l'Épisode.mkv", Change(new(SmallWords: SmallWords.French), "le retour de l'épisode.mkv"));
        Assert.Equal("L'Odyssée.mkv", Change(new(SmallWords: SmallWords.French), "l'odyssée.mkv"));
        Assert.Equal("Don't Stop.mkv", Change(new(SmallWords: SmallWords.English), "don't stop.mkv"));
    }

    [Fact]
    public void Roman_numerals_go_uppercase_up_to_39()
    {
        Assert.Equal("Rocky IV.mkv", Change(new(), "rocky iv.mkv"));
        Assert.Equal("Part XXXIX.mkv", Change(new(), "part xxxix.mkv"));
        Assert.Equal("Mix Dix Civil.mkv", Change(new(), "mix dix civil.mkv"));
        Assert.Equal("Rocky Iv.mkv", Change(new(RomanNumerals: false), "rocky iv.mkv"));
    }

    [Fact]
    public void Acronyms_are_kept_unless_the_whole_name_is_uppercase()
    {
        Assert.Equal("Best of the USA on DVD.mkv", Change(new(SmallWords: SmallWords.English), "best of the USA on DVD.mkv"));
        Assert.Equal("Best Of The Usa.mkv", Change(new(KeepAcronyms: false), "best of the USA.mkv"));
        Assert.Equal("The Matrix.mkv", Change(new(), "THE MATRIX.mkv"));
        Assert.Equal("The usa.mkv", Change(new(TextCase.Sentence, KeepAcronyms: false), "THE USA.mkv"));
    }

    [Fact]
    public void Exceptions_impose_their_case_in_every_mode()
    {
        var module = new CaseModule(Exceptions: "iPhone, TV, tv");

        Assert.Equal("My iPhone On TV.mkv", Change(module, "my IPHONE on tv.mkv"));
        Assert.Equal("MY iPhone ON TV.mkv", Change(module with { Case = TextCase.Upper }, "my iphone on tv.mkv"));
    }

    [Fact]
    public void Can_separate_glued_words_first()
    {
        Assert.Equal("My Holiday In Paris.jpg", Change(new(SpaceBeforeCapitals: true), "MyHolidayInParis.jpg"));
        Assert.Equal("Myholidayinparis.jpg", Change(new(), "MyHolidayInParis.jpg"));
    }
}
