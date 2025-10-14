using System;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyFloat : EditorProperty
{
    public bool Unit
    {
        set
        {
            if (value)
            {
                _spinBox.MinValue = -1f;
                _spinBox.MaxValue = 1f;
            }
            else
            {
                _spinBox.MinValue = float.MinValue;
                _spinBox.MaxValue = float.MaxValue;
            }
        }
    }

    public override bool Disabled
    {
        get => !_spinBox.Editable;
        set => _spinBox.Editable = !value;
    }

    private SpinBox _spinBox;
    
    protected override object GetValue()
    {
        return (float)_spinBox.Value;
    }

    protected override void SetValue(object value)
    {
        _spinBox.Value = (float)value;
    }

    public override void _Ready()
    {
        base._Ready();
        _spinBox = GetNode<SpinBox>("SpinBox");
        _spinBox.ValueChanged += newValue =>
        {
            RecordValueChange(PropertyInfo?.GetValue(Target), (float)newValue);
            NotifyValueChanged(newValue);
        };
        _spinBox.MinValue = float.MinValue;
        _spinBox.MaxValue = float.MaxValue;
    }
}