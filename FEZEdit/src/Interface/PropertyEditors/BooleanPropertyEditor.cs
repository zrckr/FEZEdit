using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public class BooleanPropertyEditor : PropertyEditor<bool>
{
    private CheckBox _checkBox;

    public override void SetTypedValue(bool value) => _checkBox?.SetPressed(value);

    public override bool GetTypedValue() => _checkBox?.ButtonPressed ?? false;

    public override Control CreateControl()
    {
        _checkBox = new CheckBox { ButtonPressed = false, Disabled = true, Alignment = HorizontalAlignment.Center };
        return _checkBox;
    }
}