using System;
using System.Text.RegularExpressions;
using FEZEdit.Interface.EditorProperties;
using Godot;

namespace FEZEdit.Interface;

public partial class Inspector : Control
{
    [Export] public bool ShowDisabled { get; set; }
    
    [Export] private EditorPropertyFactory _factory;

    public event Action<object> TargetChanged;

    public UndoRedo UndoRedo { get; set; }
    
    private object _currentTarget;

    private TextureRect _headerIcon;
    
    private Label _headerLabel;
    
    private VBoxContainer _properties;

    public override void _Ready()
    {
        _headerIcon = GetNode<TextureRect>("%HeaderIcon");
        _headerLabel = GetNode<Label>("%HeaderLabel");
        _properties = GetNode<VBoxContainer>("%Properties");
        RefreshProperties();
    }

    public void Inspect(object target)
    {
        if (_currentTarget != null)
        {
            UndoRedo.ClearHistoryForTag(_currentTarget);
        }
        _currentTarget = target;
        Callable.From(RefreshProperties).CallDeferred();
    }

    private void RefreshProperties()
    {
        foreach (var node in _properties.GetChildren())
        {
            node.QueueFree();
        }

        if (_currentTarget == null)
        {
            Visible = false;
            return;
        }
        
        var currentType = _currentTarget.GetType();
        _headerLabel.Text = Regex.Replace(currentType.Name, "(\\B[A-Z])", " $1");
        Visible = true;

        foreach (var property in currentType.GetProperties())
        {
            var editorProperty = _factory.GetEditorProperty(_currentTarget, property);
            _properties.AddChild(editorProperty, true);
            editorProperty.Label = Regex.Replace(property.Name, "(\\B[A-Z])", " $1");
            editorProperty.Value = property.GetValue(_currentTarget);
            editorProperty.UndoRedo = UndoRedo;     // Enable undo/redo after initial value was set
            editorProperty.Disabled = ShowDisabled;
            editorProperty.ValueChanged += _ => TargetChanged?.Invoke(_currentTarget);
        }
    }
}