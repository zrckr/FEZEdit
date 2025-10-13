using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyArray : EditorProperty<Array>
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

    protected override Array TypedValue
    {
        get
        {
            var array = new object[_editorProperties.Count];
            var index = 0;
            foreach (var itemContainer in _itemsContainer.GetChildren())
            {
                var itemEditor = itemContainer.GetChild<IEditorProperty>(1);
                array[index] = itemEditor.Value;
                index += 1;
            }
            return array;
        }

        set
        {
            _editorProperties.Clear();
            _foldableContainer.Title = $"Array (size: {value.Length})";

            var type = Type.GetGenericArguments()[0];
            for (int i = 0; i < value.Length; i++)
            {
                var itemContainer = new HBoxContainer();
                _itemsContainer.AddChild(itemContainer);
                
                var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
                itemContainer.AddChild(itemLabel);
                
                var itemEditor = Factory.GetEditorProperty(type);
                itemContainer.AddChild((Node)itemEditor);
                itemEditor.Value = value.GetValue(i);
                itemEditor.Label = string.Empty;
                _editorProperties.Add(itemEditor);
            }
        }
    }

    protected override event Action<Array> TypedValueChanged;

    private FoldableContainer _foldableContainer;

    private VBoxContainer _itemsContainer;

    private readonly List<IEditorProperty> _editorProperties = [];

    public override void _Ready()
    {
        base._Ready();
        _foldableContainer = GetNode<FoldableContainer>("%FoldableContainer");
        _itemsContainer = GetNode<VBoxContainer>("%ItemsContainer");
    }
}