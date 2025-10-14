using System.Text.RegularExpressions;
using FEZEdit.Interface.EditorProperties;
using FEZEdit.Interface.Editors;
using Godot;

namespace FEZEdit.Interface;

public partial class Inspector : Control
{
    [Export] public bool ShowDisabled { get; set; }
    
    [Export] private EditorPropertyFactory _factory;

    public EditorHistory EditorHistory { get; set; } = new();
    
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
            var editorProperty = _factory.GetEditorProperty(property.PropertyType);
            editorProperty.EditorHistory = EditorHistory;
            editorProperty.Target = _currentTarget;
            editorProperty.PropertyInfo = property;
            
            _properties.AddChild((Node) editorProperty, true);
            editorProperty.Label = Regex.Replace(property.Name, "(\\B[A-Z])", " $1");
            editorProperty.Value = property.GetValue(_currentTarget);
            editorProperty.Disabled = ShowDisabled;
        }
    }
}