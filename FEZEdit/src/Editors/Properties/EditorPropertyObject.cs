using System.Text.Json;
using FEZEdit.Main;
using Godot;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyObject : EditorProperty
{
    public override bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            _inspector.Disabled = value;
        }
    }
    
    private FoldableContainer _inspectorContainer;

    private Inspector _inspector;
    
    private bool _disabled;

    private object _object;

    protected override object GetValue()
    {
        return _object;
    }

    protected override void SetValue(object value)
    {
        _object = value;
        _inspector.ClearProperties();
        _inspector.InspectObject(_object);
    }

    public override void _Ready()
    {
        base._Ready();
        _inspectorContainer = GetNode<FoldableContainer>("%InspectorContainer");
        _inspectorContainer.Folded = true;
        _inspectorContainer.Title = Type?.Name ?? "Object";
        
        _inspector = GetNode<Inspector>("%Inspector");
        _inspector.SetFactory(PropertyFactory);
        _inspector.TargetChanged += obj =>
        {
            _object = obj;
            OnValueChanged();
        };
    }

    private void OnValueChanged()
    {
        var oldObject = PropertyInfo?.GetValue(Target);
        var newObject = GetValue();

        var oldJson = JsonSerializer.Serialize(oldObject, Type);
        var newJson = JsonSerializer.Serialize(newObject, Type);
        if (!oldJson.Equals(newJson))
        {
            RecordValueChange(oldObject, newObject);
            NotifyValueChanged(newObject);
        }
    }
 }