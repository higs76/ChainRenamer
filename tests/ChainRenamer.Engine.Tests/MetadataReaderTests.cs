using System.Buffers.Binary;
using System.Text;
using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests;

/// <summary>Real files, built byte by byte in a temporary folder: no sample file in the repository.</summary>
public sealed class MetadataReaderTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("ChainRenamer-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(root, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    // ───────────── Builders ─────────────

    /// <summary>A JPEG with an EXIF block (camera model, shooting date) and its frame size.</summary>
    private static byte[] Jpeg(string model, string taken, int width, int height)
    {
        // TIFF, little endian: IFD0 (model, pointer to the EXIF IFD), then the EXIF IFD (DateTimeOriginal), then the texts.
        var modelBytes = Encoding.ASCII.GetBytes(model + "\0");
        var dateBytes = Encoding.ASCII.GetBytes(taken + "\0");
        const int ifd0 = 8, ifd0Size = 2 + 2 * 12 + 4, exifIfd = ifd0 + ifd0Size, exifSize = 2 + 12 + 4, data = exifIfd + exifSize;
        var tiff = new byte[data + modelBytes.Length + dateBytes.Length];
        "II*\0"u8.CopyTo(tiff);
        BinaryPrimitives.WriteInt32LittleEndian(tiff.AsSpan(4), ifd0);

        void Entry(int at, ushort tag, ushort type, int count, int value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(at), tag);
            BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(at + 2), type);
            BinaryPrimitives.WriteInt32LittleEndian(tiff.AsSpan(at + 4), count);
            BinaryPrimitives.WriteInt32LittleEndian(tiff.AsSpan(at + 8), value);
        }
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(ifd0), 2);
        Entry(ifd0 + 2, 0x0110, 2, modelBytes.Length, data);                       // Model, ASCII
        Entry(ifd0 + 14, 0x8769, 4, 1, exifIfd);                                     // pointer to the EXIF IFD
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(exifIfd), 1);
        Entry(exifIfd + 2, 0x9003, 2, dateBytes.Length, data + modelBytes.Length);   // DateTimeOriginal, ASCII
        modelBytes.CopyTo(tiff, data);
        dateBytes.CopyTo(tiff, data + modelBytes.Length);

        var jpeg = new List<byte> { 0xFF, 0xD8, 0xFF, 0xE1 };
        var app1 = "Exif\0\0"u8.ToArray().Concat(tiff).ToArray();
        jpeg.AddRange([(byte)((app1.Length + 2) >> 8), (byte)(app1.Length + 2)]);
        jpeg.AddRange(app1);
        // SOF0: 8 bits, height, width, one component.
        jpeg.AddRange([0xFF, 0xC0, 0, 11, 8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 1, 1, 0x11, 0]);
        jpeg.AddRange([0xFF, 0xD9]);
        return [.. jpeg];
    }

    /// <summary>An MP3 starting with an ID3v2 tag of text frames; encoding 3 is UTF-8, 1 UTF-16 with its mark.</summary>
    private static byte[] Mp3(int version, byte encoding, params (string Id, string Text)[] frames)
    {
        var body = new List<byte>();
        foreach (var (id, text) in frames)
        {
            var content = encoding == 1 ? [0xFF, 0xFE, .. Encoding.Unicode.GetBytes(text)] : Encoding.UTF8.GetBytes(text);
            int size = content.Length + 1;
            body.AddRange(Encoding.ASCII.GetBytes(id));
            body.AddRange(version == 4 ? SyncSafe(size) : [(byte)(size >> 24), (byte)(size >> 16), (byte)(size >> 8), (byte)size]);
            body.AddRange([0, 0, encoding]);
            body.AddRange(content);
        }
        return [.. "ID3"u8, (byte)version, 0, 0, .. SyncSafe(body.Count), .. body, 0xFF, 0xFB, 0x90, 0x00];
    }

    private static byte[] SyncSafe(int n) => [(byte)(n >> 21 & 0x7F), (byte)(n >> 14 & 0x7F), (byte)(n >> 7 & 0x7F), (byte)(n & 0x7F)];

    /// <summary>A FLAC header: STREAMINFO, then the Vorbis comments as the last block.</summary>
    private static byte[] Flac(params string[] comments)
    {
        var block = new List<byte>();
        void Int(int n) => block.AddRange(BitConverter.GetBytes(n));     // little endian on Windows
        var vendor = "test"u8.ToArray();
        Int(vendor.Length);
        block.AddRange(vendor);
        Int(comments.Length);
        foreach (var comment in comments.Select(Encoding.UTF8.GetBytes))
        {
            Int(comment.Length);
            block.AddRange(comment);
        }
        return [.. "fLaC"u8, 0x00, 0, 0, 34, .. new byte[34], 0x84, (byte)(block.Count >> 16), (byte)(block.Count >> 8), (byte)block.Count, .. block];
    }

    // ───────────── Tests ─────────────

    [Fact]
    public void Reads_the_shooting_date_camera_and_size_of_a_photo()
    {
        var path = Write("IMG_0042.jpg", Jpeg("Pixel 8", "2024:05:17 20:30:00", 4000, 3000));

        var meta = MetadataReader.Read(path);

        Assert.Equal("2024-05-17T20:30:00", meta["taken"]);
        Assert.Equal("Pixel 8", meta["camera"]);
        Assert.Equal("4000", meta["width"]);
        Assert.Equal("3000", meta["height"]);
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    [InlineData(3, 1)]
    public void Reads_the_id3v2_tags_of_a_song(int version, byte encoding)
    {
        var path = Write("song.mp3", Mp3(version, encoding,
            ("TIT2", "Bohemian Rhapsody"), ("TPE1", "Queen"), ("TALB", "A Night at the Opera"), ("TRCK", "11/12"), (version == 4 ? "TDRC" : "TYER", "1975"), ("TCON", "(17)Rock")));

        var meta = MetadataReader.Read(path);

        Assert.Equal("Bohemian Rhapsody", meta["title"]);
        Assert.Equal("Queen", meta["artist"]);
        Assert.Equal("A Night at the Opera", meta["album"]);
        Assert.Equal("11", meta["track"]);
        Assert.Equal("1975", meta["year"]);
        Assert.Equal("Rock", meta["genre"]);
    }

    [Fact]
    public void Falls_back_to_the_id3v1_tag_at_the_end()
    {
        var tag = new byte[128];
        "TAG"u8.CopyTo(tag);
        Encoding.Latin1.GetBytes("Été indien").CopyTo(tag, 3);
        Encoding.Latin1.GetBytes("Joe Dassin").CopyTo(tag, 33);
        Encoding.Latin1.GetBytes("1975").CopyTo(tag, 93);
        tag[126] = 4;
        var path = Write("old.mp3", [.. new byte[500], .. tag]);

        var meta = MetadataReader.Read(path);

        Assert.Equal("Été indien", meta["title"]);
        Assert.Equal("Joe Dassin", meta["artist"]);
        Assert.Equal("1975", meta["year"]);
        Assert.Equal("4", meta["track"]);
    }

    [Fact]
    public void Reads_the_vorbis_comments_of_a_flac()
    {
        var path = Write("track.flac", Flac("TITLE=Clair de lune", "ARTIST=Debussy", "TRACKNUMBER=3", "DATE=1905-01-01"));

        var meta = MetadataReader.Read(path);

        Assert.Equal("Clair de lune", meta["title"]);
        Assert.Equal("Debussy", meta["artist"]);
        Assert.Equal("3", meta["track"]);
        Assert.Equal("1905", meta["year"]);
    }

    [Fact]
    public void A_damaged_or_unknown_file_has_no_metadata()
    {
        Assert.Empty(MetadataReader.Read(Write("broken.jpg", [0xFF, 0xD8, 1, 2, 3])));
        Assert.Empty(MetadataReader.Read(Write("empty.mp3", [])));
        Assert.Empty(MetadataReader.Read(Path.Combine(root, "missing.jpg")));
        Assert.False(MetadataReader.CanRead("notes.txt"));
    }

    [Fact]
    public async Task Reads_only_the_files_that_may_hold_metadata()
    {
        var files = new[] { Write("a.jpg", Jpeg("X", "2023:01:02 03:04:05", 10, 10)), Write("b.txt", "x"u8.ToArray()) }.Select(FileItem.FromPath).ToList();
        int read = 0;

        var withMetadata = await MetadataReader.ReadAsync(files, new Progress<int>(n => read = n));

        Assert.Equal(files.Select(f => f.Id), withMetadata.Select(f => f.Id));
        Assert.Equal("2023-01-02T03:04:05", withMetadata[0].Metadata["taken"]);
        Assert.Empty(withMetadata[1].Metadata);
    }

    [Fact]
    public void Variables_give_the_metadata_with_formats()
    {
        var file = FileItem.FromPath(Write("IMG_0042.jpg", Jpeg("Pixel 8", "2024:05:17 20:30:00", 4000, 3000)));
        var pipeline = new Pipeline(new SourceModule(root));
        pipeline.SetFiles([file with { Metadata = MetadataReader.Read(file.FullPath) }]);
        pipeline.Add(new TemplateModule("{taken:yyyy-MM-dd HH.mm} {appareil} {width}x{hauteur} {track:2}"));

        Assert.Equal(["2024-05-17 20.30 Pixel 8 4000x3000 {track:2}.jpg"], pipeline.Output.Names());
    }
}
