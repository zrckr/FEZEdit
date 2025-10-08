using System;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyFloat : EditorProperty<float>
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

    protected override float TypedValue
    {
        get => (float)_spinBox.Value;
        set => _spinBox.Value = value;
    }

    public override bool Disabled
    {
        get => !_spinBox.Editable;
        set => _spinBox.Editable = !value;
    }

    protected override event Action<float> TypedValueChanged;

    private SpinBox _spinBox;

    public override void _Ready()
    {
        base._Ready();
        _spinBox = GetNode<SpinBox>("SpinBox");
        _spinBox.ValueChanged += value => TypedValueChanged?.Invoke((float)value);
        _spinBox.MinValue = float.MinValue;
        _spinBox.MaxValue = float.MaxValue;
    }
}