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

            return [.. Directory.EnumerateFiles(source.Folder, "*", options)
                .Select(path => { cancellationToken.ThrowIfCancellationRequested(); return path; })
                .Order(NaturalOrder.Comparer)
                .Select(FileItem.FromPath)];
        }, cancellationToken);
}
