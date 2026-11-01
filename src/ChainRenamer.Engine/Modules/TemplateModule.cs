namespace ChainRenamer.Engine.Modules;

/// <summary>
/// Replaces the whole name, without its extension, by a template built from variables:
/// <c>{folder} - {n:2}</c>, <c>{season}x{episode} {name}</c>. An empty template leaves the names as they are.
/// </summary>
[Module("Template")]
public sealed record TemplateModule(string Template = "") : INameModule
{
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        Template.Length == 0 ? input : [.. input.Select((i, k) => i.With(NamePart.Stem, Variables.Expand(Template, i, k + 1)))];
}
