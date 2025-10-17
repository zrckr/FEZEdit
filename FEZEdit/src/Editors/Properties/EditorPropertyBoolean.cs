using Godot;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyBoolean : EditorProperty
{
    public override bool Disabled
    {
        get => _checkBox.Disabled;
        set => _checkBox.Disabled = value;
    }

    private CheckBox _checkBox;
    
    protected override object GetValue()
    {
        return _checkBox.ButtonPressed;
    }

    protected override void SetValue(object value)
    {
        _checkBox.ButtonPressed = (bool)value;
    }

    public override void _Ready()
    {
        base._Ready();
        _checkBox = GetNode<CheckBox>("CheckBox");
        _checkBox.Pressed += () => 
        {
            var newValue = _checkBox.ButtonPressed;
            RecordValueChange(PropertyInfo?.GetValue(Target), newValue);
            NotifyValueChanged(newValue);
        };
    }
}