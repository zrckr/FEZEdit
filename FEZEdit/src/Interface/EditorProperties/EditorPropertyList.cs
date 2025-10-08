using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyList : EditorProperty<IList>
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
            for (int i = 0; i < value.Count; i++)
            {
                var itemContainer = new HBoxContainer();
                _itemsContainer.AddChild(itemContainer);
                
                var itemLabel = new Label { Text = $"{i}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
                itemContainer.AddChild(itemLabel);
                
                var itemEditor = _factory.GetEditorProperty(_type);
                itemEditor.Value = value[i];
                itemEditor.Label = string.Empty;
                itemContainer.AddChild((Node)itemEditor);
                _editorProperties.Add(itemEditor);
            }
        }
    }

    protected override event Action<IList> TypedValueChanged;

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