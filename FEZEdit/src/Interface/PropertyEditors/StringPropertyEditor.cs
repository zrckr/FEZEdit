using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public class StringPropertyEditor : PropertyEditor<string>
{
    private LineEdit _lineEdit;

    public override void SetTypedValue(string value) => _lineEdit?.SetText(value);

    public override string GetTypedValue() => _lineEdit?.Text ?? string.Empty;

    public override Control CreateControl()
    {
        _lineEdit = new LineEdit { Editable = false, Alignment = HorizontalAlignment.Center };
        return _lineEdit;
    }
}