using Godot;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyString : EditorProperty
{
    public override bool Disabled
    {
        get => !_lineEdit.Editable;
        set => _lineEdit.Editable = !value;
    }
    
    private LineEdit _lineEdit;

    protected override object GetValue()
    {
        return _lineEdit.Text;
    }

    protected override void SetValue(object value)
    {
        _lineEdit.Text = (string)value;
    }

    public override void _Ready()
    {
        base._Ready();
        _lineEdit = GetNode<LineEdit>("LineEdit");
        _lineEdit.TextChanged += newText =>
        {
            RecordValueChange(PropertyInfo?.GetValue(Target), newText);
            NotifyValueChanged(newText);
        };
    }
}