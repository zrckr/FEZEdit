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
            var list = (IList)Activator.CreateInstance(Type)!;
            foreach (var itemContainer in _itemsContainer.GetChildren())
            {
                var itemEditor = itemContainer.GetChild<IEditorProperty>(1);
                list.Add(itemEditor.Value);
            }

            return list;
        }

        set
        {
            _editorProperties.Clear();
            foreach (var child in _itemsContainer.GetChildren())
            {
                child.QueueFree();
            }

            _foldableContainer.Title = $"List (size: {value.Count})";
            var elementType = Type.GetGenericArguments()[0];
            for (int i = 0; i < value.Count; i++)
            {
                var itemContainer = new HBoxContainer();
                _itemsContainer.AddChild(itemContainer);

                var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
                itemContainer.AddChild(itemLabel);

                var itemEditor = PropertyFactory.GetEditorProperty(elementType);
                itemEditor.UndoRedo = UndoRedo;
                itemEditor.ValueChanged += OnItemValueChanged;
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

    private void OnItemValueChanged(object newValue)
    {
        var oldList = PropertyInfo?.GetValue(Target);
        var newList = TypedValue;
        if (UndoRedo != null && Target != null && PropertyInfo != null && !UndoRedo.IsCommitting)
        {
            RecordValueChange((IList)oldList, newList);
        }

        TypedValueChanged?.Invoke(newList);
    }
}