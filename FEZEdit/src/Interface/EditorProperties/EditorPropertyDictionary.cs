using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyDictionary : EditorProperty
{
    public override bool Disabled
    {
        get => _valueProperties.All(i => i.Disabled) && 
               _keyProperties.All(i => i.Disabled);
        set
        {
            for (int i = 0; i < _keyProperties.Count; i++)
            {
                _keyProperties[i].Disabled = value;
                _valueProperties[i].Disabled = value;
            }
        }
    }

    private FoldableContainer _foldableContainer;

    private VBoxContainer _itemsContainer;

    private readonly List<EditorProperty> _keyProperties = [];

    private readonly List<EditorProperty> _valueProperties = [];

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
        _keyProperties.Clear();
        _valueProperties.Clear();
        foreach (var child in _itemsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var dict = (IDictionary)value;
        var types = Type.GetGenericArguments();
        _foldableContainer.Title = $"Dictionary (size: {dict.Count})";
        
        foreach (DictionaryEntry entry in dict)
        {
            var itemContainer = new HBoxContainer();
            _itemsContainer.AddChild(itemContainer);

            var keyProperty = PropertyFactory.GetEditorProperty(types[0]);
            keyProperty.UndoRedo = UndoRedo;
            keyProperty.ValueChanged += OnDictionaryItemChanged;
            itemContainer.AddChild(keyProperty);
            keyProperty.Label = string.Empty;
            keyProperty.Value = entry.Key;
            _keyProperties.Add(keyProperty);

            var valueProperty = PropertyFactory.GetEditorProperty(types[1]);
            valueProperty.UndoRedo = UndoRedo;
            valueProperty.ValueChanged += OnDictionaryItemChanged;
            itemContainer.AddChild(valueProperty);
            valueProperty.Label = string.Empty;
            valueProperty.Value = entry.Value;
            _valueProperties.Add(valueProperty);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        _foldableContainer = GetNode<FoldableContainer>("%FoldableContainer");
        _itemsContainer = GetNode<VBoxContainer>("%ItemsContainer");
    }

    private void OnDictionaryItemChanged(object newValue)
    {
        var oldDict = (IDictionary)PropertyInfo?.GetValue(Target);
        var newDict = (IDictionary)GetValue();
        if (!DictionariesAreEqual(oldDict, newDict))
        {
            RecordValueChange(oldDict, newDict);
            NotifyValueChanged(newDict);
        }
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