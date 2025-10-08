using System;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyInteger : EditorProperty<int>
{
    public int MinValue
    {
        get => (int)_spinBox.MinValue;
        set => _spinBox.MinValue = value;
    }

    public int MaxValue
    {
        get => (int)_spinBox.MaxValue;
        set => _spinBox.MaxValue = value;
    }

    protected override int TypedValue
    {
        get => (int)_spinBox.Value;
        set => _spinBox.Value = value;
    }

    public override bool Disabled
    {
        get => !_spinBox.Editable;
        set => _spinBox.Editable = !value;
    }

    protected override event Action<int> TypedValueChanged;

    private SpinBox _spinBox;

    public override void _Ready()
    {
        base._Ready();
        _spinBox = GetNode<SpinBox>("SpinBox");
        _spinBox.ValueChanged += value => TypedValueChanged?.Invoke((int)value);
        _spinBox.MinValue = int.MinValue;
        _spinBox.MaxValue = int.MaxValue;
    }
}