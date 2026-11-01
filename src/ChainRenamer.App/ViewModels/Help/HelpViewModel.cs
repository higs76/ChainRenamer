using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Help;

/// <summary>A line of the list of pages; the first page of a group shows the group name above it.</summary>
public sealed record HelpListItem(HelpPage Page, string? GroupHeader);

/// <summary>The help window: the pages on the left, the selected page on the right, and a search box.</summary>
public partial class HelpViewModel : ViewModelBase
{
    private readonly IReadOnlyList<HelpPage> pages = HelpCatalog.Pages();

    public HelpViewModel(string key = HelpCatalog.Home)
    {
        Items = List(pages);
        Show(key);
    }

    [ObservableProperty] public partial IReadOnlyList<HelpListItem> Items { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Page))]
    public partial HelpListItem? Selected { get; set; }

    [ObservableProperty] public partial string Search { get; set; } = "";

    public HelpPage? Page => Selected?.Page;

    /// <summary>Nothing matches the search.</summary>
    public bool IsEmpty => Items.Count == 0;

    public void Show(string key)
    {
        if (Items.All(i => i.Page.Key != key)) Search = "";
        Selected = Items.FirstOrDefault(i => i.Page.Key == key) ?? Items.FirstOrDefault();
    }

    partial void OnSearchChanged(string value)
    {
        var page = Page;
        Items = List(value.Trim() is { Length: > 0 } search ? pages.Where(p => p.Matches(search)) : pages);
        Selected = Items.FirstOrDefault(i => i.Page == page) ?? Items.FirstOrDefault();
        OnPropertyChanged(nameof(IsEmpty));
    }

    private static IReadOnlyList<HelpListItem> List(IEnumerable<HelpPage> pages)
    {
        string? group = null;   // the group of the previous page: a new one gets its header
        return [.. pages.Select(p => new HelpListItem(p, p.Group == group ? null : group = p.Group))];
    }
}
