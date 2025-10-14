using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyList : EditorProperty
{
    public override bool Disabled
    {
        get => _editorProperties.All(i => i.Disabled);
        set
        {
            foreach (var key in _editorProperties)
            {
                key.Disabled = value;
            }
        }
    }

    private FoldableContainer _foldableContainer;

    private VBoxContainer _itemsContainer;

    private readonly List<EditorProperty> _editorProperties = [];

    protected override object GetValue()
    {
        var list = (IList)Activator.CreateInstance(Type)!;
        foreach (var itemContainer in _itemsContainer.GetChildren())
        {
            var itemEditor = itemContainer.GetChild<EditorProperty>(1);
            list.Add(itemEditor.Value);
        }

        return list;
    }

    protected override void SetValue(object value)
    {
        _editorProperties.Clear();
        foreach (var child in _itemsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var list = (IList)value;
        var elementType = Type.GetGenericArguments()[0];
        _foldableContainer.Title = $"List (size: {list.Count})";

        for (int i = 0; i < list.Count; i++)
        {
            var itemContainer = new HBoxContainer();
            _itemsContainer.AddChild(itemContainer);

            var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            itemContainer.AddChild(itemLabel);

            var itemEditor = PropertyFactory.GetEditorProperty(elementType);
            itemEditor.UndoRedo = UndoRedo;
            itemEditor.ValueChanged += OnItemValueChanged;
            itemContainer.AddChild(itemEditor);
            itemEditor.Value = list[i];
            itemEditor.Label = string.Empty;
            _editorProperties.Add(itemEditor);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        _foldableContainer = GetNode<FoldableContainer>("%FoldableContainer");
        _itemsContainer = GetNode<VBoxContainer>("%ItemsContainer");
    }

    private void OnItemValueChanged(object newValue)
    {
        var oldList = (IList)PropertyInfo?.GetValue(Target);
        var newList = (IList)GetValue();
        if (!ListsAreEqual(oldList, newList))
        {
            RecordValueChange(oldList, newList);
            NotifyValueChanged(newList);
        }
    }

    private static bool ListsAreEqual(IList a, IList b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
            if (!Equals(a[i], b[i]))
                return false;

        return true;
    }
}