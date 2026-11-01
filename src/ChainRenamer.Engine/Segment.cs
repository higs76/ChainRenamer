namespace ChainRenamer.Engine;

/// <summary>
/// The part of the name, without its extension, that a name step works on: after a text and/or before a text.
/// <c>Speedy - 01x01 - the first lap</c>: after the last <c> - </c>, a Title case leaves the series alone.
/// The pipeline gives the module that part only, then puts the name back together: modules stay unaware of it.
/// </summary>
/// <param name="After">The part starts after this text (case ignored); empty: at the start of the name.</param>
/// <param name="Before">The part ends before this text, searched after the start; empty: at the end of the name.</param>
/// <param name="LastAfter">Starts after the last occurrence of <paramref name="After"/> rather than the first.</param>
public sealed record Segment(string After = "", string Before = "", bool LastAfter = false)
{
    /// <summary>No limit: the step works on the whole name.</summary>
    public bool IsWhole => After.Length == 0 && Before.Length == 0;

    /// <summary>The name cut in three, or null if a text is missing from it: the step then leaves the file alone.</summary>
    public (string Prefix, string Part, string Suffix)? Split(string stem)
    {
        int start = 0, end = stem.Length;
        if (After.Length > 0)
        {
            int at = LastAfter ? stem.LastIndexOf(After, StringComparison.OrdinalIgnoreCase) : stem.IndexOf(After, StringComparison.OrdinalIgnoreCase);
            if (at < 0) return null;
            start = at + After.Length;
        }
        if (Before.Length > 0)
        {
            end = stem.IndexOf(Before, start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) return null;
        }
        return (stem[..start], stem[start..end], stem[end..]);
    }
}
