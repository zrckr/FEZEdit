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
        Visible = true;
    }
}