namespace ChainRenamer.Engine;

/// <summary>
/// The state of one file as it flows through the chain.
/// Its rank is its position in the list; a file is included as long as it is in the list.
/// </summary>
public sealed record Item(FileId FileId, string Name);
