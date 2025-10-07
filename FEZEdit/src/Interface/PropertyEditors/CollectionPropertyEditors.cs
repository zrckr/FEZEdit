using System;
using System.Collections;
using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public abstract class CollectionPropertyEditor<T> : PropertyEditor<T>
{
    private const float MinimumSize = 120f;

    protected abstract int Count { get; }

    protected abstract string TypeName { get; }

    protected Tree _tree;

    protected Button _expandButton;

    protected VBoxContainer _container;

    protected bool _expanded;

    protected T _value;

    protected abstract void PopulateTree(TreeItem item);

    public override void SetTypedValue(T value)
    {
        _value = value;
        if (_container?.GetChild(0) is HBoxContainer header)
        {
            if (header.GetChild(0) is Label countLabel)
            {
                countLabel.Text = $"{TypeName} ({Count} items)";
            }
        }

        if (!_expanded || _tree == null)
        {
            return;
        }

        _tree.Clear();
        PopulateTree(_tree.CreateItem());
    }

    public override T GetTypedValue() => _value;

    public override Control CreateControl()
    {
        _container = new VBoxContainer();

        var header = new HBoxContainer();
        var countLabel = new Label()
        {
            Text = $"{TypeName} ({Count} items)", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        header.AddChild(countLabel);

        _expandButton = new Button() { Text = _expanded ? "Collapse" : "Expand" };
        _expandButton.Pressed += OnExpandToggle;
        header.AddChild(_expandButton);
        _container.AddChild(header);

        _tree = new Tree() { Columns = 2, CustomMinimumSize = Vector2.Down * MinimumSize, Visible = _expanded };
        _tree.SetColumnTitle(0, "Key/Index");
        _tree.SetColumnTitle(1, "Value");
        _tree.SetColumnExpand(0, true);
        _tree.SetColumnExpand(1, true);
        _container.AddChild(_tree);

        if (_expanded)
        {
            PopulateTree(_tree.CreateItem());
        }

        return _container;
    }

    private void OnExpandToggle()
    {
        _expanded = !_expanded;
        _expandButton.Text = _expanded ? "Collapse" : "Expand";
        _tree.Visible = _expanded;

        if (!_expanded)
        {
            return;
        }

        _tree.Clear();
        PopulateTree(_tree.CreateItem());
    }

    protected string FormatValue(object value)
    {
        return value switch
        {
            null => "null",
            string str => $"\"{str}\"",
            bool b => b.ToString().ToLower(),
            ICollection collection => $"{value.GetType().Name} ({collection.Count} items)",
            _ => value.ToString()
        };
    }
}

public class ArrayPropertyEditor : CollectionPropertyEditor<Array>
{
    protected override int Count => _value?.Length ?? 0;

    protected override string TypeName => _value == null ? "Array" : $"{_value.GetType().GetElementType()?.Name}[]";

    protected override void PopulateTree(TreeItem root)
    {
        if (_value == null)
        {
            return;
        }

        for (int i = 0; i < _value.Length; i++)
        {
            var item = _tree.CreateItem(root);
            item.SetText(0, $"[{i}]");
            item.SetText(1, FormatValue(_value.GetValue(i)));

            var value = _value.GetValue(i);
            if (value == null)
            {
                continue;
            }

            item.SetTooltipText(0, $"Index: {i}\nType: {value.GetType()}");
            item.SetTooltipText(1, $"Value: {value}\nType: {value.GetType()}");
        }
    }
}

public class ListPropertyEditor : CollectionPropertyEditor<IList>
{
    protected override int Count => _value?.Count ?? 0;

    protected override string TypeName
    {
        get
        {
            if (_value == null)
            {
                return "List";
            }

            var listType = _value.GetType();
            if (!listType.IsGenericType)
            {
                return "List";
            }

            var genericArgs = listType.GetGenericArguments();
            return genericArgs.Length > 0
                ? $"List<{genericArgs[0].Name}>"
                : "List";
        }
    }

    protected override void PopulateTree(TreeItem root)
    {
        if (_value == null)
        {
            return;
        }

        for (int i = 0; i < _value.Count; i++)
        {
            var item = _tree.CreateItem(root);
            item.SetText(0, $"[{i}]");
            item.SetText(1, FormatValue(_value[i]));

            var value = _value[i];
            if (value == null)
            {
                continue;
            }

            item.SetTooltipText(0, $"Index: {i}\nType: {value.GetType()}");
            item.SetTooltipText(1, $"Value: {value}\nType: {value.GetType()}");
        }
    }
}

public class DictionaryPropertyEditor : CollectionPropertyEditor<IDictionary>
{
    protected override int Count => _value?.Count ?? 0;

    protected override string TypeName
    {
        get
        {
            if (_value == null)
            {
                return "Dictionary";
            }

            var dictType = _value.GetType();
            if (!dictType.IsGenericType)
            {
                return "Dictionary";
            }

            var genericArgs = dictType.GetGenericArguments();
            return genericArgs.Length >= 2
                ? $"Dictionary<{genericArgs[0].Name}, {genericArgs[1].Name}>"
                : "Dictionary";
        }
    }

    protected override void PopulateTree(TreeItem root)
    {
        if (_value == null)
        {
            return;
        }

        foreach (DictionaryEntry entry in _value)
        {
            var item = _tree.CreateItem(root);
            item.SetText(0, FormatValue(entry.Key));
            item.SetText(1, FormatValue(entry.Value));

            item.SetTooltipText(0, $"Key: {entry.Key}\nType: {entry.Key.GetType()}");
            item.SetTooltipText(1, $"Value: {entry.Value}\nType: {entry.Value?.GetType()}");
        }
    }
}