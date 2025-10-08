using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyArray : EditorProperty<Array>
{
    [Export] public string TypeFullName { get; set; }

    [Export] private EditorPropertyFactory _factory;
    
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
            for (int i = 0; i < value.Length; i++)
            {
                var itemContainer = new HBoxContainer();
                _itemsContainer.AddChild(itemContainer);
                
                var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
                itemContainer.AddChild(itemLabel);
                
                var itemEditor = _factory.GetEditorProperty(_type);
                itemEditor.Value = value.GetValue(i);
                itemEditor.Label = string.Empty;
                itemContainer.AddChild((Node)itemEditor);
                _editorProperties.Add(itemEditor);
            }
        }
    }

    protected override event Action<Array> TypedValueChanged;

    private FoldableContainer _foldableContainer;

    private VBoxContainer _itemsContainer;

    private Type _type;

    private readonly List<IEditorProperty> _editorProperties = [];

    public override void _Ready()
    {
        base._Ready();
        _foldableContainer = GetNode<FoldableContainer>("%FoldableContainer");
        _itemsContainer = GetNode<VBoxContainer>("%ItemsContainer");
        _type = Type.GetType(TypeFullName);
    }

    public override void SetGenericArguments(params Type[] types) => TypeFullName = types[0].FullName;
}