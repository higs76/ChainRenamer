using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChainRenamer.Engine.Renaming;

/// <summary>One move done on disk, or, with <see cref="Undo"/>, the undoing of that move.</summary>
public sealed record JournalEntry(
    string From,
    string To,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Undo = false);

/// <summary>
/// The undo log: one JSON object per line (JSON Lines), appended and flushed after each move,
/// so that it stays readable even if the program stops in the middle of a renaming.
/// Undoing a move is written too, so that the journal always knows what remains to undo.
/// </summary>
public sealed class Journal : IDisposable
{
    private readonly StreamWriter writer;

    private Journal(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        writer = new StreamWriter(path, append: true);
    }

    public static Journal Open(string path) => new(path);

    public void Write(JournalEntry entry)
    {
        writer.WriteLine(JsonSerializer.Serialize(entry));
        writer.Flush();
    }

    public void Dispose() => writer.Dispose();

    /// <summary>Reads a journal; a line cut short by a crash is ignored.</summary>
    public static IReadOnlyList<JournalEntry> Read(string path) =>
        [.. File.ReadLines(path)
            .Select(line => { try { return JsonSerializer.Deserialize<JournalEntry>(line); } catch (JsonException) { return null; } })
            .OfType<JournalEntry>()];

    /// <summary>The moves not undone yet, oldest first.</summary>
    public static IReadOnlyList<JournalEntry> ToUndo(IEnumerable<JournalEntry> entries)
    {
        var moves = new List<JournalEntry>();
        foreach (var entry in entries)
        {
            if (!entry.Undo) moves.Add(entry);
            else if (moves.FindLastIndex(m => m.From == entry.From && m.To == entry.To) is var i and >= 0) moves.RemoveAt(i);
        }
        return moves;
    }
}
