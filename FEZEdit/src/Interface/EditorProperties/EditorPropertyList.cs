using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyList : EditorProperty<IList>
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

    protected override IList TypedValue
    {
        get
        {
            return _itemsContainer.GetChildren()
                .Select(itemContainer => itemContainer.GetChild<IEditorProperty>(1))
                .Select(itemEditor => itemEditor.Value)
                .ToList();
        }

        set
        {
            _editorProperties.Clear();
            _foldableContainer.Title = $"List (size: {value.Count})";
            
            var type = Type.GetGenericArguments()[0];
            for (int i = 0; i < value.Count; i++)
            {
                var itemContainer = new HBoxContainer();
                _itemsContainer.AddChild(itemContainer);
                
                var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
                itemContainer.AddChild(itemLabel);
                
                var itemEditor = Factory.GetEditorProperty(type);
                itemContainer.AddChild((Node)itemEditor);
                itemEditor.Value = value[i];
                itemEditor.Label = string.Empty;
                _editorProperties.Add(itemEditor);
            }
        }
    }

    protected override event Action<IList> TypedValueChanged;

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