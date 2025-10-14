using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyInteger : EditorProperty
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

    public override bool Disabled
    {
        get => !_spinBox.Editable;
        set => _spinBox.Editable = !value;
    }

    private SpinBox _spinBox;
    
    protected override object GetValue()
    {
        return (int)_spinBox.Value;
    }

    protected override void SetValue(object value)
    {
        _spinBox.Value = (int)value;
    }

    public override void _Ready()
    {
        base._Ready();
        _spinBox = GetNode<SpinBox>("SpinBox");
        _spinBox.ValueChanged += newValue =>
        {
            RecordValueChange(PropertyInfo?.GetValue(Target), (int)newValue);
            NotifyValueChanged(newValue);
        };
        _spinBox.MinValue = int.MinValue;
        _spinBox.MaxValue = int.MaxValue;
    }
}