using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyList : EditorProperty
{
    [Export] private Texture2D _removeIcon;
    
    public override bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            foreach (var property in _editorProperties)
            {
                property.Disabled = _disabled;
            }

            foreach (var buttons in _editorButtons)
            {
                buttons.Disabled = _disabled;
            }
        }
    }

    private FoldableContainer _foldableContainer;

    private VBoxContainer _itemsContainer;
    
    private VBoxContainer _addContainer;

    private Button _addButton;

    private readonly List<EditorProperty> _editorProperties = [];
    
    private readonly List<Button> _editorButtons = [];
    
    private EditorProperty _addEditorProperty;
    
    private bool _disabled;

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
        _editorButtons.Clear();
        foreach (var child in _itemsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _addContainer.RemoveChild(_addEditorProperty);

        var list = (IList)value;
        var elementType = Type.GetGenericArguments()[0];
        _foldableContainer.Title = $"List (size: {list.Count})";

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var itemContainer = new HBoxContainer();
            _itemsContainer.AddChild(itemContainer);

            var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            itemContainer.AddChild(itemLabel);

            var itemEditor = PropertyFactory.GetEditorProperty(elementType);
            itemEditor.UndoRedo = UndoRedo;
            itemEditor.ValueChanged += _ => OnItemValueChanged();
            itemContainer.AddChild(itemEditor);
            itemEditor.Value = item;
            itemEditor.Label = string.Empty;
            itemEditor.Disabled = true;
            _editorProperties.Add(itemEditor);
            
            var index = i;
            var itemButton = new Button
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd, 
                IconAlignment = HorizontalAlignment.Center
            };
            itemContainer.AddChild(itemButton);
            itemButton.Icon = _removeIcon;
            itemButton.CustomMinimumSize = new Vector2(24, 16);
            itemButton.Pressed += () => OnItemRemove(index);
            _editorButtons.Add(itemButton);
        }
        
        _addEditorProperty = PropertyFactory.GetEditorProperty(elementType);
        _addContainer.AddChild(_addEditorProperty);
        _addContainer.MoveChild(_addEditorProperty, 0);
        _addEditorProperty.Label = Tr("New value:");
        _editorProperties.Add(_addEditorProperty);
        
        _editorButtons.Add(_addButton);
    }

    public override void _Ready()
    {
        base._Ready();
        _foldableContainer = GetNode<FoldableContainer>("%FoldableContainer");
        _foldableContainer.Folded = true;
        _itemsContainer = GetNode<VBoxContainer>("%ItemsContainer");
        _addContainer = GetNode<VBoxContainer>("%AddContainer");
        _addButton = GetNode<Button>("%AddButton");
        _addButton.Pressed += OnItemAdd;
    }

    private void OnItemValueChanged()
    {
        var oldList = (IList)PropertyInfo?.GetValue(Target);
        var newList = (IList)GetValue();
        if (!ListsAreEqual(oldList, newList))
        {
            RecordValueChange(oldList, newList);
            NotifyValueChanged(newList);
        }
    }
    
    private void OnItemAdd()
    {
        if (_addEditorProperty?.Value != null)
        {
            var list = (IList)GetValue();
            list.Add(_addEditorProperty.Value);
            SetValue(list);
        }
    }
    
    private void OnItemRemove(int index)
    {
        var list = (IList)GetValue();
        list.RemoveAt(index);
        SetValue(list);
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