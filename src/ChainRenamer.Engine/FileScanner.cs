using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine;

/// <summary>Lists the files of a source folder, local or UNC, without blocking the caller.</summary>
public static class FileScanner
{
    /// <exception cref="DirectoryNotFoundException">The folder does not exist or the share is unreachable.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the folder is denied.</exception>
    public static Task<IReadOnlyList<FileItem>> ScanAsync(SourceModule source, CancellationToken cancellationToken = default) =>
        Task.Run<IReadOnlyList<FileItem>>(() =>
        {
            // Defaults skip hidden and system files (Thumbs.db, desktop.ini) and inaccessible subfolders.
            var options = new EnumerationOptions { RecurseSubdirectories = source.IncludeSubfolders };

            // FileInfo from an enumeration already holds the size and dates: no extra request per file on a share.
            return [.. new DirectoryInfo(source.Folder).EnumerateFiles("*", options)
                .Select(file => { cancellationToken.ThrowIfCancellationRequested(); return file; })
                .OrderBy(file => file.FullName, NaturalOrder.Comparer)
                .Select(file => new FileItem(FileId.New(), file.FullName) { Size = file.Length, Modified = file.LastWriteTime })];
        }, cancellationToken);
}
