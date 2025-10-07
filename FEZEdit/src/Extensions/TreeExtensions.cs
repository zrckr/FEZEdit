using System.Collections.Generic;
using Godot;

namespace FEZEdit.Extensions;

public static class TreeExtensions
{
    public static string GetFullPath(this TreeItem item, bool excludeRoot = false, char separator = '\\')
    {
        var names = new Stack<string>();
        names.Push(item.GetText(0));
        
        var parent = item.GetParent();
        while (parent != null)
        {
            names.Push(parent.GetText(0));
            parent = parent.GetParent();
        }
        if (excludeRoot)
        {
            names.Pop();
        }
        
        return string.Join(separator, names);
    }

    public static bool TryFindChildByText(this TreeItem item, string text, out TreeItem foundItem)
    {
        var child = item.GetFirstChild();
        while (child != null)
        {
            if (child.GetText(0) == text)
            {
                foundItem = child;
                return true;
            }
            child = child.GetNext();
        }
        foundItem = null;
        return false;
    }

    public static bool HasChild(this TreeItem item, TreeItem child)
    {
        var currentItem = child.GetParent();
        while (currentItem != null)
        {
            if (currentItem == item)
            {
                return true;
            }
            currentItem = currentItem.GetParent();
        }
        return false;
    }
}