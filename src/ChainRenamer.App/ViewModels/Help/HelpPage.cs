using System.Globalization;
using ChainRenamer.App.Resources;
using ChainRenamer.Engine;

namespace ChainRenamer.App.ViewModels.Help;

/// <summary>A setting of a module, or a keyboard shortcut: its label as on screen, and what it does.</summary>
public sealed record HelpOption(string Label, string Text);

/// <summary>One file of an example: its name before and after the module.</summary>
public sealed record HelpRow(string Before, string After)
{
    public bool IsChanged => Before != After;
}

/// <summary>
/// An example computed by the module itself, so that the help always shows what the module really does.
/// Rows follow the output order: a sort shows the files in their new order.
/// </summary>
public sealed class HelpExample(string caption, IModule module, params string[] names)
{
    public string Caption { get; } = caption;
    public IModule Module { get; } = module;

    public IReadOnlyList<HelpRow> Rows { get; } =
        [.. names.Zip(module.Apply([.. names.Select(n => new Item(FileId.New(), n))]), (before, after) => new HelpRow(before, after.Name))];
}

/// <summary>
/// A page of the help. Its <see cref="Key"/> is stable: the module type as written in a .chain file
/// ("Replace"), or the name of a general topic. It will be the anchor of the help website.
/// </summary>
public sealed record HelpPage(string Key, string Group, string Title, string Intro)
{
    /// <summary>The module this page explains, if any: the "?" of its panel opens it.</summary>
    public Type? Module { get; init; }

    public IReadOnlyList<HelpOption> Options { get; init; } = [];

    /// <summary>The heading above the options ("Settings"), or null for none.</summary>
    public string? OptionsTitle { get; init; } = Strings.Help_Settings;

    public string? OptionsHeading => HasOptions ? OptionsTitle : null;

    public IReadOnlyList<HelpExample> Examples { get; init; } = [];

    /// <summary>The paragraphs of the introduction, separated by a blank line in the texts.</summary>
    public IReadOnlyList<string> Paragraphs => Intro.Split("\n\n");

    public bool HasOptions => Options.Count > 0;
    public bool HasExamples => Examples.Count > 0;

    /// <summary>Whether the page talks about <paramref name="search"/>, ignoring case and accents ("ecran" finds "écran").</summary>
    public bool Matches(string search) =>
        Texts().Any(t => CultureInfo.CurrentCulture.CompareInfo.IndexOf(t, search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);

    public IEnumerable<string> Texts() =>
        new[] { Title, Intro }
            .Concat(Options.SelectMany(o => new[] { o.Label, o.Text }))
            .Concat(Examples.SelectMany(e => e.Rows.SelectMany(r => new[] { r.Before, r.After }).Prepend(e.Caption)));
}
