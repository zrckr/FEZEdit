using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public class IntegerPropertyEditor : PropertyEditor<int>
{
    private SpinBox _spinBox;

    public override void SetTypedValue(int value) => _spinBox?.SetValue(value);

    public override int GetTypedValue() => (int)(_spinBox?.GetValue() ?? 0);
    
    public override Control CreateControl()
    {
        _spinBox = new SpinBox
        {
            MinValue = int.MinValue,
            MaxValue = int.MaxValue,
            Editable = false,
            Alignment = HorizontalAlignment.Center
        };
        return _spinBox;
    }
}