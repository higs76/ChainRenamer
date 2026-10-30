using System.Globalization;

namespace ChainRenamer.Engine;

public static class NaturalOrder
{
    /// <summary>
    /// "Episode 2" before "Episode 10", case-insensitive.
    /// Invariant culture, so that a saved chain sorts the same way on every machine.
    /// </summary>
    public static readonly StringComparer Comparer =
        StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering | CompareOptions.IgnoreCase);
}
