using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FEZEdit.Editors.Properties;
using Godot;

namespace FEZEdit.Main;

public partial class Inspector : Control
{
    [Export] private EditorPropertyFactory _factory;

    [Export] private string _initialHeaderText;

    public event Action<object> TargetChanged;

    public bool Disabled { private get; set; }
    
    public UndoRedo UndoRedo { private get; set; }

    private TextureRect _headerIcon;
    
    private Label _headerLabel;
    
    private VBoxContainer _properties;

    public override void _Ready()
    {
        _headerIcon = GetNode<TextureRect>("%HeaderIcon");
        _headerLabel = GetNode<Label>("%HeaderLabel");
        _properties = GetNode<VBoxContainer>("%Properties");
        SetHeaderText(_initialHeaderText);
        ClearProperties();
    }

    public void InspectObject(object target)
    {
        if (target != null)
        {
            UndoRedo.ClearHistoryForTag(target);
        }
        Callable.From(() => AddEditorProperties(target)).CallDeferred();
    }

    public void InspectProperty(object target, string propertyName)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var type = target.GetType();
        var propertyInfo = type.GetProperty(propertyName);
        
        if (propertyInfo == null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        Callable.From(() => AddEditorProperty(target, propertyInfo)).CallDeferred();
    }

    public void AddPropertyTooltip(string propertyName, string tooltip)
    {
        Callable.From(() => AddPropertyTooltipInternal(propertyName, tooltip)).CallDeferred();
    }

    public void SetHeaderText(string text)
    {
        if (_headerLabel != null)
        {
            _headerLabel.Text = Tr(text);
        }
    }

    public void ClearProperties()
    {
        foreach (var node in _properties.GetChildren())
        {
            node.Free();
        }
    }

    private void AddEditorProperties(object target)
    {
        if (target == null)
        {
            ClearProperties();
            Visible = false;
            return;
        }
        
        var type = target.GetType();
        var header = !string.IsNullOrEmpty(_initialHeaderText)
            ? _initialHeaderText
            : NameRegex().Replace(type.Name, " $1");
        
        Visible = true;
        SetHeaderText(header);
        foreach (var property in type.GetProperties())
        {
            AddEditorProperty(target, property);
        }
    }

    private void AddEditorProperty(object target, PropertyInfo propertyInfo)
    {
        var editorProperty = _factory.GetEditorProperty(target, propertyInfo);
        editorProperty.Name = propertyInfo.Name;
        _properties.AddChild(editorProperty, true);
        
        editorProperty.Label = NameRegex().Replace(propertyInfo.Name, " $1");
        editorProperty.Value = propertyInfo.GetValue(target);
        editorProperty.UndoRedo = UndoRedo;     // Enable undo/redo after initial value was set
        editorProperty.Disabled = Disabled;
        editorProperty.ValueChanged += _ => TargetChanged?.Invoke(target);
    }

    private void AddPropertyTooltipInternal(string propertyName, string tooltip)
    {
        var editorProperty = _properties.GetChildren()
            .OfType<Editors.Properties.EditorProperty>()
            .FirstOrDefault(ed => ed.Name == propertyName);

        if (editorProperty != null)
        {
            editorProperty.TooltipText = Tr(tooltip);
        }
    }
    
    [GeneratedRegex("(\\B[A-Z])")]
    private static partial Regex NameRegex();
}