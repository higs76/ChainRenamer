using System.Collections.Immutable;

namespace ChainRenamer.Engine;

/// <summary>A file found on disk, as it was before any renaming.</summary>
public sealed record FileItem(FileId Id, string FullPath)
{
    public string Name => Path.GetFileName(FullPath);
    public string Directory => Path.GetDirectoryName(FullPath) ?? "";

    /// <summary>The size in bytes, read when the folder was listed.</summary>
    public long Size { get; init; }

    /// <summary>The last write time, read when the folder was listed.</summary>
    public DateTime Modified { get; init; }

    /// <summary>Shooting date, camera, music tags… read by <see cref="MetadataReader"/>; empty until then.</summary>
    public ImmutableDictionary<string, string> Metadata { get; init; } = ImmutableDictionary<string, string>.Empty;

    public static FileItem FromPath(string fullPath) => new(FileId.New(), fullPath);
}
