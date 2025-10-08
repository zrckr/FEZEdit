using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyDictionary : EditorProperty<IDictionary>
{
    [Export] private string KeyTypeFullName { get; set; }
    
    [Export] private string ValueTypeFullName { get; set; }

    [Export] private EditorPropertyFactory _factory;
    
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
            var dict = new Dictionary<object, object>();
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
            foreach (var key in value.Keys)
            {
                var keyProperty = _factory.GetEditorProperty(_keyType);
                keyProperty.Label = string.Empty;
                keyProperty.Value = value;
                _keyProperties.Add(keyProperty);
            }
            
            _valueProperties.Clear();
            foreach (var item in value.Values)
            {
                var valueProperty = _factory.GetEditorProperty(_valueType);
                valueProperty.Label = string.Empty;
                valueProperty.Value = item;
                _valueProperties.Add(valueProperty);
            }
            
            _foldableContainer.Title = $"Dictionary (size: {value.Count})";
            for (int i = 0; i < value.Count; i++)
            {
                var itemContainer = new HBoxContainer();
                itemContainer.AddChild((Node) _keyProperties[i]);
                itemContainer.AddChild((Node) _valueProperties[i]);
                _itemsContainer.AddChild(itemContainer);
            }
        }
    }

    protected override event Action<IDictionary> TypedValueChanged;

    private FoldableContainer _foldableContainer;

    private VBoxContainer _itemsContainer;
    
    private Type _keyType;
    
    private Type _valueType;

    private readonly List<IEditorProperty> _keyProperties = [];
    
    private readonly List<IEditorProperty> _valueProperties = [];

    public override void _Ready()
    {
        base._Ready();
        _foldableContainer = GetNode<FoldableContainer>("%FoldableContainer");
        _itemsContainer = GetNode<VBoxContainer>("%ItemsContainer");
        _keyType = Type.GetType(KeyTypeFullName);
        _valueType = Type.GetType(ValueTypeFullName);
    }

    public override void SetGenericArguments(params Type[] types)
    {
        KeyTypeFullName = types[0].FullName;
        ValueTypeFullName = types[1].FullName;
    }
}