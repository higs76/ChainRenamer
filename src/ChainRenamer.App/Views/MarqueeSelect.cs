using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ChainRenamer.App.Views;

/// <summary>
/// Selection with a rectangle, as in the Windows Explorer: press on a row, drag, and every row
/// the rectangle crosses is selected. With Ctrl, they are added to the current selection.
/// Near the top or bottom edge, the grid scrolls by itself.
/// </summary>
public sealed class MarqueeSelect
{
    private const double Threshold = 6;
    private const double ScrollZone = 32;

    private readonly DataGrid grid;
    private readonly DispatcherTimer scrollTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly Canvas overlay = new() { IsHitTestVisible = false };
    private readonly Border band = new() { BorderThickness = new Thickness(1) };

    private object? startItem;          // set on press; the drag starts after a few pixels
    private object? lastItem;
    private Point start, current;
    private double startOffset;         // from the top of the start row, which moves when the grid scrolls
    private object[] kept = [];         // the selection kept with Ctrl
    private bool active;

    private MarqueeSelect(DataGrid grid)
    {
        this.grid = grid;
        overlay.Children.Add(band);
        scrollTimer.Tick += (_, _) => AutoScroll();

        // Tunnel routing: we see the press before the grid. We do not handle it,
        // so that a simple click still selects as usual.
        grid.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        grid.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
        grid.AddHandler(InputElement.PointerReleasedEvent, (_, e) => Stop(e), RoutingStrategies.Tunnel);
        grid.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => Stop(null));
    }

    public static void Attach(DataGrid grid) => _ = new MarqueeSelect(grid);

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(grid);
        if (!point.Properties.IsLeftButtonPressed || e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.Source is not Visual source) return;

        // Check boxes, handles, headers and scroll bars keep their own behaviour.
        var chain = source.GetSelfAndVisualAncestors().ToList();
        if (chain.Any(v => v is CheckBox or DataGridColumnHeader or ScrollBar || v is StyledElement s && s.Classes.Contains("handle"))) return;
        if (chain.OfType<DataGridRow>().FirstOrDefault() is not { DataContext: { } item } row) return;

        startItem = lastItem = item;
        start = current = point.Position;
        startOffset = start.Y - (row.TranslatePoint(default, grid)?.Y ?? 0);
        kept = e.KeyModifiers.HasFlag(KeyModifiers.Control) ? [.. grid.SelectedItems.Cast<object>()] : [];
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (startItem is null) return;
        current = e.GetPosition(grid);

        if (!active)
        {
            if (Math.Abs(current.X - start.X) < Threshold && Math.Abs(current.Y - start.Y) < Threshold) return;
            active = true;
            e.Pointer.Capture(grid);
            ShowBand();
            scrollTimer.Start();
        }

        SelectTo(DragReorder.ItemNearest(grid, current.Y));
        AutoScroll();
        e.Handled = true;
    }

    private void Stop(PointerEventArgs? e)
    {
        startItem = null;
        if (!active) return;

        active = false;
        scrollTimer.Stop();
        AdornerLayer.GetAdornerLayer(grid)?.Children.Remove(overlay);
        if (e is not null)
        {
            e.Handled = true;   // the release must not change the selection we just made
            e.Pointer.Capture(null);
        }
    }

    /// <summary>Selects the rows from the start row to <paramref name="item"/>, plus those kept with Ctrl.</summary>
    private void SelectTo(object? item)
    {
        if (item is null || Items is not { } items) return;
        int from = items.IndexOf(startItem), to = items.IndexOf(item);
        if (from < 0 || to < 0) return;

        lastItem = item;
        var wanted = kept.Concat(Enumerable.Range(Math.Min(from, to), Math.Abs(to - from) + 1).Select(i => items[i]!)).ToHashSet();
        var selected = grid.SelectedItems;
        foreach (var old in selected.Cast<object>().Where(o => !wanted.Contains(o)).ToList()) selected.Remove(old);
        foreach (var added in wanted.Where(w => !selected.Contains(w))) selected.Add(added);
        DrawBand(from < to);
    }

    // Scrolls one row at a time while the pointer is near an edge. Run by the timer (pointer held still) and on each move.
    private void AutoScroll()
    {
        int direction = current.Y < ScrollZone ? -1 : current.Y > grid.Bounds.Height - ScrollZone ? +1 : 0;
        if (!active || direction == 0 || lastItem is null || Items is not { } items) return;

        var next = items[Math.Clamp(items.IndexOf(lastItem) + direction, 0, items.Count - 1)]!;
        grid.ScrollIntoView(next, null);
        SelectTo(next);
    }

    private IList? Items => grid.ItemsSource as IList;

    private void ShowBand()
    {
        var accent = grid.TryFindResource("SystemAccentColor", grid.ActualThemeVariant, out var c) && c is Color color ? color : Colors.DodgerBlue;
        band.Background = new SolidColorBrush(accent, 0.2);
        band.BorderBrush = new SolidColorBrush(accent);
        AdornerLayer.SetAdornedElement(overlay, grid);
        AdornerLayer.GetAdornerLayer(grid)?.Children.Add(overlay);
        DrawBand(true);
    }

    /// <summary>The rectangle from the press point, which follows its row when the grid scrolls, to the pointer.</summary>
    private void DrawBand(bool downwards)
    {
        var startRow = grid.GetVisualDescendants().OfType<DataGridRow>()
            .FirstOrDefault(r => r.IsEffectivelyVisible && Equals(r.DataContext, startItem));
        double startY = startRow?.TranslatePoint(default, grid) is { } top ? top.Y + startOffset : downwards ? 0 : grid.Bounds.Height;

        double Clamp(double v, double max) => Math.Clamp(v, 0, max);
        double x1 = Clamp(start.X, grid.Bounds.Width), x2 = Clamp(current.X, grid.Bounds.Width);
        double y1 = Clamp(startY, grid.Bounds.Height), y2 = Clamp(current.Y, grid.Bounds.Height);

        Canvas.SetLeft(band, Math.Min(x1, x2));
        Canvas.SetTop(band, Math.Min(y1, y2));
        band.Width = Math.Abs(x2 - x1);
        band.Height = Math.Abs(y2 - y1);
    }
}
