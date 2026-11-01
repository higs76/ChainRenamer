using System.Buffers.Binary;
using System.Text;

namespace ChainRenamer.Engine;

/// <summary>
/// Reads the tags of MP3 files (ID3v2.3, ID3v2.4, and ID3v1 at the end of the file) and FLAC files (Vorbis comments).
/// Only the start, or the end, of the file is read: a few kilobytes, even for a long song.
/// </summary>
internal static class AudioTags
{
    // The tags read, by their ID3 frame and their Vorbis comment, as metadata names.
    private static readonly Dictionary<string, string> Id3Frames = new()
    {
        ["TIT2"] = "title", ["TPE1"] = "artist", ["TALB"] = "album", ["TRCK"] = "track",
        ["TYER"] = "year", ["TDRC"] = "year", ["TCON"] = "genre",
    };

    private static readonly Dictionary<string, string> VorbisFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TITLE"] = "title", ["ARTIST"] = "artist", ["ALBUM"] = "album", ["TRACKNUMBER"] = "track",
        ["DATE"] = "year", ["GENRE"] = "genre",
    };

    public static Dictionary<string, string> Read(Stream stream, string extension) =>
        Clean(extension.Equals(".flac", StringComparison.OrdinalIgnoreCase) ? ReadFlac(stream) : ReadId3(stream));

    // "3/12" → "3"; "2024-05-17" → "2024"; "(17)Rock" → "Rock"; empty values dropped.
    private static Dictionary<string, string> Clean(Dictionary<string, string> tags)
    {
        if (tags.TryGetValue("track", out var track)) tags["track"] = track.Split('/')[0].Trim();
        if (tags.TryGetValue("year", out var year) && year.Length > 4) tags["year"] = year[..4];
        if (tags.TryGetValue("genre", out var genre) && genre.StartsWith('(') && genre.IndexOf(')') is var end and > 0 && end < genre.Length - 1)
            tags["genre"] = genre[(end + 1)..];
        foreach (var key in tags.Where(t => t.Value.Trim().Length == 0).Select(t => t.Key).ToList()) tags.Remove(key);
        return tags.ToDictionary(t => t.Key, t => t.Value.Trim());
    }

    private static Dictionary<string, string> ReadId3(Stream stream)
    {
        var tags = new Dictionary<string, string>();
        Span<byte> header = stackalloc byte[10];
        if (stream.ReadAtLeast(header, 10, throwOnEndOfStream: false) == 10 && header[..3].SequenceEqual("ID3"u8) && header[3] is 3 or 4)
        {
            int version = header[3], size = SyncSafe(header[6..]);
            var body = new byte[size];
            stream.ReadAtLeast(body, size, throwOnEndOfStream: false);
            for (int at = 0; at + 10 <= body.Length && body[at] != 0;)
            {
                var id = Encoding.ASCII.GetString(body, at, 4);
                var sizeBytes = body.AsSpan(at + 4, 4);
                int frameSize = version == 4 ? SyncSafe(sizeBytes) : BinaryPrimitives.ReadInt32BigEndian(sizeBytes);
                if (frameSize <= 0 || at + 10 + frameSize > body.Length) break;
                if (Id3Frames.TryGetValue(id, out var name) && !tags.ContainsKey(name))
                    tags[name] = Text(body.AsSpan(at + 10, frameSize));
                at += 10 + frameSize;
            }
        }
        if (tags.Count == 0 && stream.Length >= 128) ReadId3v1(stream, tags);
        return tags;
    }

    // ID3v1: the last 128 bytes, "TAG" then fixed-width fields.
    private static void ReadId3v1(Stream stream, Dictionary<string, string> tags)
    {
        var tag = new byte[128];
        stream.Seek(-128, SeekOrigin.End);
        if (stream.ReadAtLeast(tag, 128, throwOnEndOfStream: false) < 128 || !tag.AsSpan(0, 3).SequenceEqual("TAG"u8)) return;
        string Field(int start, int length) => Encoding.Latin1.GetString(tag, start, length).TrimEnd('\0', ' ');
        (tags["title"], tags["artist"], tags["album"], tags["year"]) = (Field(3, 30), Field(33, 30), Field(63, 30), Field(93, 4));
        if (tag[125] == 0 && tag[126] != 0) tags["track"] = tag[126].ToString();     // ID3v1.1
    }

    // A text frame: an encoding byte, then the text.
    private static string Text(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 2) return "";
        var text = frame[0] switch
        {
            1 => DecodeUtf16Bom(frame[1..]),
            2 => Encoding.BigEndianUnicode.GetString(frame[1..]),
            3 => Encoding.UTF8.GetString(frame[1..]),
            _ => Encoding.Latin1.GetString(frame[1..]),
        };
        return text.Split('\0')[0];
    }

    // UTF-16 with a byte order mark: FF FE little endian, FE FF big endian.
    private static string DecodeUtf16Bom(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF ? Encoding.BigEndianUnicode.GetString(bytes[2..])
        : bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE ? Encoding.Unicode.GetString(bytes[2..])
        : Encoding.Unicode.GetString(bytes);

    private static int SyncSafe(ReadOnlySpan<byte> b) => (b[0] & 0x7F) << 21 | (b[1] & 0x7F) << 14 | (b[2] & 0x7F) << 7 | b[3] & 0x7F;

    // FLAC: "fLaC", then metadata blocks; block 4 holds the Vorbis comments "KEY=value", in little endian.
    private static Dictionary<string, string> ReadFlac(Stream stream)
    {
        var tags = new Dictionary<string, string>();
        Span<byte> marker = stackalloc byte[4];
        if (stream.ReadAtLeast(marker, 4, throwOnEndOfStream: false) < 4 || !marker.SequenceEqual("fLaC"u8)) return tags;

        Span<byte> header = stackalloc byte[4];
        while (stream.ReadAtLeast(header, 4, throwOnEndOfStream: false) == 4)
        {
            bool last = (header[0] & 0x80) != 0;
            int type = header[0] & 0x7F, length = header[1] << 16 | header[2] << 8 | header[3];
            if (type != 4)
            {
                if (last) break;
                stream.Seek(length, SeekOrigin.Current);
                continue;
            }

            var block = new byte[length];
            stream.ReadAtLeast(block, length, throwOnEndOfStream: false);
            int at = 4 + BinaryPrimitives.ReadInt32LittleEndian(block);     // skips the vendor string
            int count = BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(at));
            at += 4;
            for (int i = 0; i < count && at + 4 <= block.Length; i++)
            {
                int size = BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(at));
                var comment = Encoding.UTF8.GetString(block, at + 4, size);
                at += 4 + size;
                if (comment.Split('=', 2) is [var key, var value] && VorbisFields.TryGetValue(key, out var name)) tags.TryAdd(name, value);
            }
            break;
        }
        return tags;
    }
}
