using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyDictionary : EditorProperty<IDictionary>
{
    public override bool Disabled
    {
        get => _valueProperties.All(i => i.Disabled);
        set
        {
            foreach (var key in _valueProperties)
            {
                key.Disabled = value;
            }
        }
    }

    protected override IDictionary TypedValue
    {
        get
        {
            var dict = (IDictionary)Activator.CreateInstance(Type)!;
            foreach (var itemContainer in _itemsContainer.GetChildren())
            {
                var keyProperty = itemContainer.GetChild<IEditorProperty>(0);
                var valueProperty = itemContainer.GetChild<IEditorProperty>(1);
                dict[keyProperty.Value] = valueProperty.Value;
            }

            return dict;
        }

        set
        {
            _keyProperties.Clear();
            _valueProperties.Clear();
            foreach (var child in _itemsContainer.GetChildren())
            {
                child.QueueFree();
            }

            _foldableContainer.Title = $"Dictionary (size: {value.Count})";
            var types = Type.GetGenericArguments();
            foreach (DictionaryEntry entry in value)
            {
                var itemContainer = new HBoxContainer();
                _itemsContainer.AddChild(itemContainer);

                var keyProperty = PropertyFactory.GetEditorProperty(types[0]);
                keyProperty.EditorHistory = EditorHistory;
                keyProperty.ValueChanged += OnDictionaryItemChanged;
                itemContainer.AddChild((Node)keyProperty);
                keyProperty.Label = string.Empty;
                keyProperty.Value = entry.Key;
                _keyProperties.Add(keyProperty);

                var valueProperty = PropertyFactory.GetEditorProperty(types[1]);
                valueProperty.EditorHistory = EditorHistory;
                valueProperty.ValueChanged += OnDictionaryItemChanged;
                itemContainer.AddChild((Node)valueProperty);
                valueProperty.Label = string.Empty;
                valueProperty.Value = entry.Value;
                _valueProperties.Add(valueProperty);
            }
        }
    }

    protected override event Action<IDictionary> TypedValueChanged;

    private FoldableContainer _foldableContainer;

    private VBoxContainer _itemsContainer;

    private readonly List<IEditorProperty> _keyProperties = [];

    private readonly List<IEditorProperty> _valueProperties = [];

    public override void _Ready()
    {
        base._Ready();
        _foldableContainer = GetNode<FoldableContainer>("%FoldableContainer");
        _itemsContainer = GetNode<VBoxContainer>("%ItemsContainer");
    }

    private void OnDictionaryItemChanged(object newValue)
    {
        var oldDict = PropertyInfo?.GetValue(Target);
        var newDict = TypedValue;
        if (EditorHistory != null && Target != null && PropertyInfo != null && !EditorHistory.IsCommitting)
        {
            RecordValueChange((IDictionary)oldDict, newDict);
        }

        TypedValueChanged?.Invoke(newDict);
    }
}