using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ChainRenamer.App.Views;

/// <summary>
/// The light markup of the help texts, on a TextBlock: <c>`code`</c> and <c>**bold**</c>.
/// In XAML: <c>&lt;TextBlock views:RichText.Markup="{Binding Text}" /&gt;</c>.
/// </summary>
public static partial class RichText
{
    // An attached property: a property that a class defines for controls of another class, as Grid.Row.
    public static readonly AttachedProperty<string?> MarkupProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("Markup", typeof(RichText));

    public static string? GetMarkup(TextBlock block) => block.GetValue(MarkupProperty);
    public static void SetMarkup(TextBlock block, string? value) => block.SetValue(MarkupProperty, value);

    private static readonly FontFamily Code = new("Consolas, Cascadia Mono, monospace");
    private static readonly IBrush CodeBackground = new SolidColorBrush(Color.FromArgb(0x26, 0x80, 0x80, 0x80));

    static RichText() => MarkupProperty.Changed.AddClassHandler<TextBlock>((block, e) => Render(block, e.NewValue as string));

    [GeneratedRegex(@"(`[^`]+`|\*\*[^*]+\*\*)")]
    private static partial Regex Spans();

    private static void Render(TextBlock block, string? text)
    {
        var inlines = new InlineCollection();
        foreach (var part in Spans().Split(text ?? "").Where(p => p.Length > 0))
        {
            inlines.Add(part switch
            {
                ['`', .., '`'] => new Run(part[1..^1]) { FontFamily = Code, Background = CodeBackground },
                ['*', '*', .., '*', '*'] => new Run(part[2..^2]) { FontWeight = FontWeight.SemiBold },
                _ => new Run(part),
            });
        }
        block.Inlines = inlines;
    }
}
