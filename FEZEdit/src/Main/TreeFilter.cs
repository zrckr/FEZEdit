using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Main;

public partial class TreeFilter : Control
{
    public enum SortingMode
    {
        NameAscending,
        NameDescending,
        TypeAscending,
        TypeDescending
    }

    private static readonly Godot.Collections.Dictionary<SortingMode, string> SortingModeNames = new()
    {
        { SortingMode.NameAscending, "Sort By Name (Ascending)" },
        { SortingMode.NameDescending, "Sort By Name (Descending)" },
        { SortingMode.TypeAscending, "Sort By Type (Ascending)" },
        { SortingMode.TypeDescending, "Sort By Type (Descending)" }
    };

    public TreeItem RootItem { get; set; }

    private LineEdit _filesFilter;

    private MenuButton _sortingMenu;

    private SortingMode _currentSortingMode = SortingMode.NameAscending;

    public override void _Ready()
    {
        _filesFilter = GetNode<LineEdit>("%FilesFilter");
        _filesFilter.TextChanged += OnFilesFilterChanged;

        _sortingMenu = GetNode<MenuButton>("%SortingMenu");
        var popup = _sortingMenu.GetPopup();
        popup.IdPressed += OnSortingModeChanged;

        foreach ((SortingMode mode, var name) in SortingModeNames)
        {
            popup.AddRadioCheckItem(Tr(name), (int)mode);
        }

        popup.SetItemAsRadioCheckable(0, true); // SortingMode.NameAscending
    }

    public void SortTreeItem(TreeItem item)
    {
        if (item == null)
        {
            return;
        }

        var children = new List<TreeItem>();
        var child = item.GetFirstChild();

        while (child != null)
        {
            children.Add(child);
            child = child.GetNext();
        }

        if (children.Count == 0)
        {
            return;
        }

        children.Sort((a, b) =>
        {
            var aIsDir = a.GetChildCount() > 0 || a.GetMetadata(0).AsString() == "";
            var bIsDir = b.GetChildCount() > 0 || b.GetMetadata(0).AsString() == "";

            var aText = a.GetText(0);
            var bText = b.GetText(0);

            return aIsDir switch
            {
                // Folders always come first
                true when !bIsDir => -1,
                false when bIsDir => 1,
                _ => _currentSortingMode switch
                {
                    SortingMode.NameAscending => string.Compare(aText, bText, StringComparison.OrdinalIgnoreCase),
                    SortingMode.NameDescending => string.Compare(bText, aText, StringComparison.OrdinalIgnoreCase),
                    SortingMode.TypeAscending => CompareByType(a, b, true),
                    SortingMode.TypeDescending => CompareByType(a, b, false),
                    _ => 0
                }
            };
        });

        for (int i = 1; i < children.Count; i++)
        {
            children[i].MoveAfter(children[i - 1]);
        }

        foreach (var treeItem in children)
        {
            SortTreeItem(treeItem);
        }
    }

    private void OnFilesFilterChanged(string filter)
    {
        if (RootItem == null)
        {
            return;
        }

        var stack = new Stack<TreeItem>();
        void IterateOverTree(TreeItem item, Action<TreeItem> action)
        {
            stack.Push(item);
            while (stack.Count > 0)
            {
                var stackItem = stack.Pop();
                action(stackItem);
                var child = stackItem.GetFirstChild();
                while (child != null)
                {
                    stack.Push(child);
                    child = child.GetNext();
                }
            }
        }
        
        if (string.IsNullOrEmpty(filter))
        {
            IterateOverTree(RootItem, item =>
            {
                item.Collapsed = true;
                item.Visible = true;
            });
            RootItem.Collapsed = false;
            return;
        }
        
        var visibleItems = new List<TreeItem>();
        IterateOverTree(RootItem, item =>
        {
            item.Visible = false;
            if (FuzzyMatch(item.GetText(0).ToLower(), filter.ToLower()))
            {
                visibleItems.Add(item);
            }
        });
        
        foreach (var item in visibleItems)
        {
            item.Visible = true;
            item.Collapsed = false;
            
            var parentItem = item.GetParent();
            while (parentItem != null)
            {
                parentItem.Visible = true;
                parentItem.Collapsed = false;
                parentItem = parentItem.GetParent();
            }
        }
    }

    private void OnSortingModeChanged(long id)
    {
        _currentSortingMode = (SortingMode)id;
        SortTreeItem(RootItem!);
    }

    private static int CompareByType(TreeItem a, TreeItem b, bool ascending)
    {
        var aName = a.GetText(0);
        var bName = b.GetText(0);

        var aExtension = GetFileExtension(aName);
        var bExtension = GetFileExtension(bName);

        var extComparison = string.Compare(aExtension, bExtension, StringComparison.OrdinalIgnoreCase);
        if (extComparison != 0)
        {
            return ascending ? extComparison : -extComparison;
        }

        return string.Compare(aName, bName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFileExtension(string fileName)
    {
        var lastDot = fileName.LastIndexOf('.');
        if (lastDot == -1 || lastDot == fileName.Length - 1)
        {
            return "";
        }

        return fileName[(lastDot + 1)..];
    }

    private static bool IsAnyAncestorMatched(TreeItem item, string searchText)
    {
        while (item != null)
        {
            if (FuzzyMatch(item.GetText(0).ToLower(), searchText))
            {
                return true;
            }

            item = item.GetParent();
        }

        return false;
    }

    private static bool FuzzyMatch(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return true;
        }
        
        int patternIndex = 0;
        foreach (char c in text)
        {
            if (c != pattern[patternIndex])
            {
                continue;
            }

            patternIndex++;
            if (patternIndex == pattern.Length)
            {
                return true;
            }
        }
        
        return false;
    }
}