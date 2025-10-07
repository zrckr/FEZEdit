using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public class FloatPropertyEditor : PropertyEditor<float>
{
    private SpinBox _spinBox;

    public override void SetTypedValue(float value) => _spinBox?.SetValue(value);

    public override float GetTypedValue() => (float)(_spinBox?.Value ?? 0f);

    public override Control CreateControl()
    {
        _spinBox = new SpinBox
        {
            MinValue = float.MinValue,
            MaxValue = float.MaxValue,
            Step = 0.1f,
            Editable = false,
            Alignment = HorizontalAlignment.Center
        };
        return _spinBox;
    }
}