using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ChainRenamer.App.ViewModels;

/// <summary>An ObservableCollection whose items can all be replaced with a single notification.</summary>
public sealed class RowCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// One "Reset" instead of one notification per item: the grid then rebuilds once,
    /// instead of 50,000 times for 50,000 files.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items) Items.Add(item);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
