using System.Collections.Immutable;
using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

namespace ChainRenamer.Engine;

/// <summary>
/// Reads the metadata of photos, videos and music: shooting date, camera, dimensions, tags.
/// It opens each file, which costs time on a network share: only the extensions that can hold metadata are read,
/// and the App does it in the background, after showing the names.
/// </summary>
/// <remarks>
/// Values are text, dates in the sortable form <c>yyyy-MM-ddTHH:mm:ss</c>. Names: <see cref="Names"/>.
/// Photos and videos through MetadataExtractor (Apache 2.0); MP3 and FLAC through <see cref="AudioTags"/>.
/// </remarks>
public static class MetadataReader
{
    /// <summary>The metadata names, which are also the names of their variables.</summary>
    public static IReadOnlyList<string> Names { get; } = ["taken", "camera", "width", "height", "artist", "album", "title", "track", "year", "genre"];

    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".tif", ".tiff", ".png", ".heic", ".heif", ".webp", ".cr2", ".cr3", ".nef", ".arw", ".dng", ".orf", ".rw2", ".raf",
        ".mp4", ".mov", ".m4v", ".3gp",
    };

    private static readonly HashSet<string> Audio = new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".flac" };

    /// <summary>Whether a file may hold metadata, from its extension.</summary>
    public static bool CanRead(string path) => Path.GetExtension(path) is var ext && (Images.Contains(ext) || Audio.Contains(ext));

    /// <summary>The files with their metadata, in the same order; the others are returned as they are.</summary>
    /// <param name="progress">The number of files read so far.</param>
    public static Task<IReadOnlyList<FileItem>> ReadAsync(IReadOnlyList<FileItem> files, IProgress<int>? progress = null, CancellationToken cancellation = default) =>
        Task.Run<IReadOnlyList<FileItem>>(() =>
        {
            int done = 0;
            return [.. files.Select(file =>
            {
                cancellation.ThrowIfCancellationRequested();
                var read = CanRead(file.FullPath) ? file with { Metadata = Read(file.FullPath) } : file;
                progress?.Report(++done);
                return read;
            })];
        }, cancellation);

    /// <summary>The metadata of one file; empty if it has none, or cannot be read (damaged, locked…).</summary>
    public static ImmutableDictionary<string, string> Read(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16 * 1024);
            var values = Audio.Contains(Path.GetExtension(path)) ? AudioTags.Read(stream, Path.GetExtension(path)) : ReadImage(stream);
            return values.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ImageProcessingException or ArgumentException
                                   or InvalidOperationException or IndexOutOfRangeException or NotSupportedException)
        {
            return ImmutableDictionary<string, string>.Empty;
        }
    }

    private static Dictionary<string, string> ReadImage(Stream stream)
    {
        var directories = ImageMetadataReader.ReadMetadata(stream);
        var values = new Dictionary<string, string>();

        // The shooting date of a photo; for a video, the creation date of the movie.
        if (directories.OfType<ExifSubIfdDirectory>().FirstOrDefault() is { } exif && exif.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var taken)
            || directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault() is { } movie
               && movie.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out taken) && taken.Year > 1904)
            values["taken"] = taken.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        if (directories.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDescription(ExifDirectoryBase.TagModel) is { Length: > 0 } model)
            values["camera"] = model.Trim();

        // Dimensions: whatever directory gives them first (JPEG, PNG, EXIF, video track…).
        foreach (var (name, tags) in new[] { ("width", new[] { "Image Width", "Exif Image Width", "Width" }), ("height", ["Image Height", "Exif Image Height", "Height"]) })
        {
            var size = directories.SelectMany(d => d.Tags).FirstOrDefault(t => tags.Contains(t.Name));
            if (size?.Description?.Split(' ')[0] is { Length: > 0 } pixels && int.TryParse(pixels, out _)) values[name] = pixels;
        }
        return values;
    }
}
