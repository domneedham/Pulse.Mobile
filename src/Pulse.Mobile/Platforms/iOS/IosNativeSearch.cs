using Microsoft.Maui.Platform;
using UIKit;

namespace Pulse.UI;

/// <summary>
/// iOS implementation of <see cref="NativeSearch"/>: hangs a <see cref="UISearchController"/> off the
/// page's navigation item so search sits under the (large) title like Apple's own apps and adopts the
/// system Liquid Glass material on iOS 26. Text changes flow back to the attached Text property and
/// raise the attached Command.
/// </summary>
internal static class IosNativeSearch
{
    public static void Attach(Page page)
    {
        // The navigation item only exists once the page is on screen; (re)wire on Loaded. Guard against
        // double-attaching if several attached properties change.
        page.Loaded -= OnLoaded;
        page.Loaded += OnLoaded;

        if (page.IsLoaded)
        {
            Wire(page);
        }
    }

    private static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is Page page)
        {
            Wire(page);
        }
    }

    private static void Wire(Page page)
    {
        // The page's handler exposes the backing UIViewController, whose NavigationItem hosts the search.
        if (page.Handler is not IPlatformViewHandler { ViewController: { } viewController })
        {
            return;
        }

        var navItem = viewController.NavigationItem;
        if (navItem is null || navItem.SearchController is not null)
        {
            return; // not in a nav controller yet, or already wired
        }

        var search = new UISearchController(searchResultsController: null)
        {
            ObscuresBackgroundDuringPresentation = false,
            HidesNavigationBarDuringPresentation = false
        };
        search.SearchBar.Placeholder = NativeSearch.GetPlaceholder(page) ?? "Search";
        search.SearchBar.AutocapitalizationType = UITextAutocapitalizationType.None;

        search.SearchResultsUpdater = new SearchUpdater(page);

        navItem.SearchController = search;
        navItem.HidesSearchBarWhenScrolling = false; // keep it visible under the large title
    }

    private sealed class SearchUpdater(Page page) : UISearchResultsUpdating
    {
        public override void UpdateSearchResultsForSearchController(UISearchController searchController)
        {
            var text = searchController.SearchBar.Text ?? string.Empty;

            // Push the text into the bound property and fire the search command on the UI thread.
            NativeSearch.SetText(page, text);
            var command = NativeSearch.GetCommand(page);
            if (command?.CanExecute(text) == true)
            {
                command.Execute(text);
            }
        }
    }
}
