namespace ChainRenamer.Engine;

/// <summary>A file found on disk, as it was before any renaming.</summary>
public sealed record FileItem(FileId Id, string FullPath)
{
    public string Name => Path.GetFileName(FullPath);
    public string Directory => Path.GetDirectoryName(FullPath) ?? "";

    public static FileItem FromPath(string fullPath) => new(FileId.New(), fullPath);
}
