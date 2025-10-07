using System.Reflection;
using System.Text.Json.Serialization;
using FEZEdit.Interface.PropertyEditors;
using Godot;

namespace FEZEdit.Interface;

public partial class Inspector : Control
{
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
        RefreshProperties();
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
        
        _headerLabel.Text = _currentTarget.GetType().Name;

        var properties = _currentTarget.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var row = new HBoxContainer() { Name = property.Name };
            _properties.AddChild(row);
            
            var label = new Label() { Text = property.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddChild(label);
            
            var editor = PropertyEditorFactory.CreateEditor(property, out bool notFound);
            var editorControl = editor.CreateControl();
            editorControl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(editorControl);
            
            var value = property.GetValue(_currentTarget);
            value = notFound ? value?.ToString() : value;
            editor.SetValue(value);
        }

        Visible = true;
    }
}