using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ChainRenamer.App.Views;

/// <summary>
/// Reorders a ListBox or a DataGrid by dragging the element with the "handle" class of an item.
/// While dragging, the dragged item takes the place of the item under the pointer, so the preview
/// follows the gesture. Near the top or bottom edge, the list scrolls by itself.
/// </summary>
public sealed class DragReorder
{
    private const double ScrollZone = 32;

    private readonly Control list;
    private readonly Action<object, object> moveTo;
    private readonly DispatcherTimer scrollTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private object? dragged;
    private double pointerY;

    private DragReorder(Control list, Action<object, object> moveTo)
    {
        this.list = list;
        this.moveTo = moveTo;
        scrollTimer.Tick += (_, _) => AutoScroll();

        // Tunnel routing: we see the press before the list uses it for selection.
        list.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerReleasedEvent, (_, e) => Stop(e), RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => Stop(null));
    }

    /// <param name="moveTo">Called with (dragged item, item under the pointer).</param>
    public static void Attach(Control list, Action<object, object> moveTo) => _ = new DragReorder(list, moveTo);

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(list).Properties.IsLeftButtonPressed || e.Source is not Visual source) return;

        var handle = source.GetSelfAndVisualAncestors().OfType<StyledElement>().FirstOrDefault(c => c.Classes.Contains("handle"));
        if (handle?.DataContext is not { } item) return;

        dragged = item;
        pointerY = e.GetPosition(list).Y;
        e.Pointer.Capture(list);
        scrollTimer.Start();
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (dragged is null) return;
        pointerY = e.GetPosition(list).Y;
        Follow();
        AutoScroll();
        e.Handled = true;
    }

    private void Stop(PointerEventArgs? e)
    {
        if (dragged is not { } item) return;
        dragged = null;
        scrollTimer.Stop();
        e?.Pointer.Capture(null);

        // Once the list has been laid out again, make sure the dropped item is in view.
        Dispatcher.UIThread.Post(() => ScrollTo(item), DispatcherPriority.Background);
    }

    private void ScrollTo(object item)
    {
        switch (list)
        {
            case DataGrid grid: grid.ScrollIntoView(item, null); break;
            case ListBox box: box.ScrollIntoView(item); break;
        }
    }

    private void Follow()
    {
        if (dragged is not null && ItemNearest(pointerY) is { } target && !Equals(target, dragged))
            moveTo(dragged, target);
    }

    // Scrolls one item at a time while the pointer is near an edge, then keeps following it.
    // Run by the timer (pointer held still) and on each pointer move.
    private void AutoScroll()
    {
        int direction = pointerY < ScrollZone ? -1 : pointerY > list.Bounds.Height - ScrollZone ? +1 : 0;
        if (dragged is null || direction == 0 || Items is not { } items) return;

        // At either end of the list, this keeps the dragged item itself in view.
        int next = Math.Clamp(items.IndexOf(dragged) + direction, 0, items.Count - 1);
        ScrollTo(items[next]!);
        Follow();
    }

    private IList? Items => list switch
    {
        DataGrid grid => grid.ItemsSource as IList,
        ItemsControl items => items.ItemsSource as IList,
        _ => null,
    };

    private object? ItemNearest(double y) => ItemNearest(list, y);

    /// <summary>The item whose row is closest to <paramref name="y"/>, among the rows visible in the list.</summary>
    internal static object? ItemNearest(Control list, double y) =>
        list.GetVisualDescendants().OfType<Control>()
            .Where(c => c is ListBoxItem or DataGridRow && c.IsEffectivelyVisible)
            .Select(c => (Item: c.DataContext, Top: c.TranslatePoint(default, list)?.Y ?? double.NaN, c.Bounds.Height))
            .Where(r => r.Top + r.Height > 0 && r.Top < list.Bounds.Height)
            .MinBy(r => y < r.Top ? r.Top - y : y > r.Top + r.Height ? y - r.Top - r.Height : 0)
            .Item;
}
