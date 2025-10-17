using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyDictionary : EditorProperty
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
    
    private EditorProperty _addKeyEditorProperty;
    
    private EditorProperty _addValueEditorProperty;
    
    private bool _disabled;

    protected override object GetValue()
    {
        var dict = (IDictionary)Activator.CreateInstance(PropertyInfo.PropertyType)!;
        foreach (var itemContainer in _itemsContainer.GetChildren())
        {
            var keyProperty = itemContainer.GetChild<EditorProperty>(0);
            var valueProperty = itemContainer.GetChild<EditorProperty>(1);
            dict[keyProperty.Value] = valueProperty.Value;
        }

        return dict;
    }

    protected override void SetValue(object value)
    {
        _editorProperties.Clear();
        _editorButtons.Clear();
        foreach (var child in _itemsContainer.GetChildren())
        {
            child.Free();
        }
        _addKeyEditorProperty?.Free();
        _addValueEditorProperty?.Free();

        var dict = (IDictionary)value;
        var types = Type.GetGenericArguments();
        _foldableContainer.Title = $"Dictionary (size: {dict.Count})";
        
        foreach (DictionaryEntry entry in dict)
        {
            var itemContainer = new HBoxContainer();
            _itemsContainer.AddChild(itemContainer);

            var keyProperty = PropertyFactory.GetEditorProperty(types[0]);
            keyProperty.UndoRedo = UndoRedo;
            keyProperty.ValueChanged += _ => OnDictionaryItemChanged();
            itemContainer.AddChild(keyProperty);
            keyProperty.Label = string.Empty;
            keyProperty.Value = entry.Key;
            _editorProperties.Add(keyProperty);

            var valueProperty = PropertyFactory.GetEditorProperty(types[1]);
            valueProperty.UndoRedo = UndoRedo;
            valueProperty.ValueChanged += _ => OnDictionaryItemChanged();
            itemContainer.AddChild(valueProperty);
            valueProperty.Label = string.Empty;
            valueProperty.Value = entry.Value;
            _editorProperties.Add(valueProperty);
            
            var itemButton = new Button
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd, 
                IconAlignment = HorizontalAlignment.Center
            };
            itemContainer.AddChild(itemButton);
            itemButton.Icon = _removeIcon;
            itemButton.CustomMinimumSize = new Vector2(24, 16);
            itemButton.Pressed += () => OnItemRemove(entry.Key);
            _editorButtons.Add(itemButton);
        }
        
        _addKeyEditorProperty = PropertyFactory.GetEditorProperty(types[0]);
        _addContainer.AddChild(_addKeyEditorProperty);
        _addContainer.MoveChild(_addKeyEditorProperty, 0);
        _addKeyEditorProperty.Label = Tr("New Key:");
        _editorProperties.Add(_addKeyEditorProperty);
        
        _addValueEditorProperty = PropertyFactory.GetEditorProperty(types[1]);
        _addContainer.AddChild(_addValueEditorProperty);
        _addContainer.MoveChild(_addValueEditorProperty, 1);
        _addValueEditorProperty.Label = Tr("New Value:s");
        _editorProperties.Add(_addValueEditorProperty);
        
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

    private void OnDictionaryItemChanged()
    {
        var oldDict = (IDictionary)PropertyInfo?.GetValue(Target);
        var newDict = (IDictionary)GetValue();
        if (!DictionariesAreEqual(oldDict, newDict))
        {
            RecordValueChange(oldDict, newDict);
            NotifyValueChanged(newDict);
        }
    }
    
    private void OnItemAdd()
    {
        // Ensures that the value in the editor is up to date
        // before writing to UndoRedo
        Callable.From(() =>
        {
            if (_addKeyEditorProperty?.Value != null && _addValueEditorProperty?.Value != null)
            {
                var dict = (IDictionary)GetValue();
                dict.Add(_addKeyEditorProperty.Value, _addValueEditorProperty.Value);
                SetValue(dict);
                OnDictionaryItemChanged();
            }
        });
    }
    
    private void OnItemRemove(object key)
    {
        // Ditto
        Callable.From(() =>
        {
            var dict = (IDictionary)GetValue();
            dict.Remove(key);
            SetValue(dict);
            OnDictionaryItemChanged();
        }).CallDeferred();
    }
    
    private static bool DictionariesAreEqual(IDictionary a, IDictionary b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        
        foreach (var key in a.Keys)
        {
            if (!b.Contains(key))
                return false;

            var aValue = a[key];
            var bValue = b[key];
            
            if (aValue is IDictionary aDict && bValue is IDictionary bDict)
            {
                if (!DictionariesAreEqual(aDict, bDict))
                    return false;
            }
            else if (aValue is Array aArray && bValue is Array bArray)
            {
                if (!ArraysAreEqual(aArray, bArray))
                    return false;
            }
            else if (!Equals(aValue, bValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ArraysAreEqual(Array a, Array b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
    
        for (int i = 0; i < a.Length; i++)
        {
            var aValue = a.GetValue(i);
            var bValue = b.GetValue(i);
            
            if (aValue is IDictionary aDict && bValue is IDictionary bDict)
            {
                if (!DictionariesAreEqual(aDict, bDict))
                    return false;
            }
            else if (aValue is Array aArray && bValue is Array bArray)
            {
                if (!ArraysAreEqual(aArray, bArray))
                    return false;
            }
            else if (!Equals(aValue, bValue))
            {
                return false;
            }
        }
    
        return true;
    }
}