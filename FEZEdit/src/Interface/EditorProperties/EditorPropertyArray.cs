using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyArray : EditorProperty
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
        var array = Array.CreateInstance(Type.GetElementType() ?? typeof(object), _editorProperties.Count);
        var index = 0;
        foreach (var itemContainer in _itemsContainer.GetChildren())
        {
            var itemEditor = itemContainer.GetChild<EditorProperty>(1);
            array.SetValue(itemEditor.Value, index);
            index += 1;
        }

        return array;
    }

    protected override void SetValue(object value)
    {
        _editorProperties.Clear();
        foreach (var child in _itemsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var array = (Array)value;
        var elementType = Type.GetElementType();
        _foldableContainer.Title = $"Array (size: {array.Length})";
        
        for (int i = 0; i < array.Length; i++)
        {
            var itemContainer = new HBoxContainer();
            _itemsContainer.AddChild(itemContainer);

            var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            itemContainer.AddChild(itemLabel);

            var itemEditor = PropertyFactory.GetEditorProperty(elementType);
            itemEditor.UndoRedo = UndoRedo;
            itemEditor.Target = this;
            itemEditor.ValueChanged += OnItemValueChanged;

            itemContainer.AddChild(itemEditor);
            itemEditor.Value = array.GetValue(i);
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
        var oldArray = (Array)PropertyInfo?.GetValue(Target);
        var newArray = (Array)GetValue();
        if (!ArraysAreEqual(oldArray, newArray))
        {
            RecordValueChange(oldArray, newArray);
            NotifyValueChanged(newArray);
        }
    }
    
    private static bool ArraysAreEqual(Array a, Array b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        
        for (int i = 0; i < a.Length; i++)
            if (!Equals(a.GetValue(i), b.GetValue(i)))
                return false;
        
        return true;
    }
}