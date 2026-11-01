using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class PadNumbersModuleTests
{
    private static string[] Pad(PadNumbersModule module, params string[] names) => module.Apply(TestItems.Of(names)).Names();

    [Theory]
    [InlineData("Episode 5.mkv", "Episode 05.mkv")]
    [InlineData("S1E5.mkv", "S01E05.mkv")]
    [InlineData("Episode 12.mkv", "Episode 12.mkv")]
    [InlineData("Episode 123.mkv", "Episode 123.mkv")]     // longer: kept
    [InlineData("No number.mkv", "No number.mkv")]
    public void Pads_every_number(string name, string expected) =>
        Assert.Equal([expected], Pad(new(), name));

    [Fact]
    public void Never_touches_the_extension()
    {
        Assert.Equal(["Track 01.mp3", "Clip 07.3gp"], Pad(new(), "Track 1.mp3", "Clip 7.3gp"));
    }

    [Theory]
    [InlineData(WhichNumbers.First, "Speedy 01 x265 1080p.mkv")]
    [InlineData(WhichNumbers.Last, "Speedy 1 x265 1080p.mkv")]
    public void Can_pad_only_the_first_or_the_last_number(WhichNumbers which, string expected) =>
        Assert.Equal([expected], Pad(new(Which: which), "Speedy 1 x265 1080p.mkv"));

    [Fact]
    public void Last_pads_the_last_number_of_the_name()
    {
        Assert.Equal(["Holiday 2024 - 007.jpg"], Pad(new(3, WhichNumbers.Last), "Holiday 2024 - 7.jpg"));
    }

    [Fact]
    public void Can_trim_extra_zeros()
    {
        Assert.Equal(["Episode 07.mkv", "Episode 10.mkv", "Episode 00.mkv"], Pad(new(TrimZeros: true), "Episode 007.mkv", "Episode 0010.mkv", "Episode 000.mkv"));
        Assert.Equal(["Episode 007.mkv"], Pad(new(), "Episode 007.mkv"));
    }

    [Fact]
    public void Automatic_digits_follow_the_longest_number_of_the_list()
    {
        Assert.Equal(["Photo 001.jpg", "Photo 020.jpg", "Photo 120.jpg"], Pad(new(Digits: 0), "Photo 1.jpg", "Photo 20.jpg", "Photo 120.jpg"));
    }

    [Fact]
    public void Automatic_digits_only_count_the_numbers_padded()
    {
        // The year is not the number padded: it does not make every episode four digits long.
        Assert.Equal(["2024 Speedy 01.mkv", "2024 Speedy 12.mkv"], Pad(new(0, WhichNumbers.Last), "2024 Speedy 1.mkv", "2024 Speedy 12.mkv"));
        Assert.Equal(["Episode 7.mkv"], Pad(new(0, TrimZeros: true), "Episode 007.mkv"));
        Assert.Equal(["None.mkv"], Pad(new(0), "None.mkv"));
    }

    [Fact]
    public void Only_ascii_digits_are_numbers()
    {
        Assert.Equal(["Épisode ٣ 01.mkv"], Pad(new(), "Épisode ٣ 1.mkv"));
    }
}
