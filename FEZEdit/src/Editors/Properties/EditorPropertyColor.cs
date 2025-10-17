using FEZEdit.Core;
using Godot;
using Color = FEZRepacker.Core.Definitions.Game.XNA.Color;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyColor : EditorProperty
{
    public override bool Disabled
    {
        get => _colorPickerButton.Disabled;
        set => _colorPickerButton.Disabled = value;
    }

    private ColorPickerButton _colorPickerButton;
    
    protected override object GetValue()
    {
        return _colorPickerButton.Color.ToXna();
    }

    protected override void SetValue(object value)
    {
        _colorPickerButton.Color = ((Color)value).ToGodot();
    }

    public override void _Ready()
    {
        base._Ready();
        _colorPickerButton = GetNode<ColorPickerButton>("ColorPickerButton");
        _colorPickerButton.ColorChanged += color =>
        {
            var newColor = color.ToXna();
            RecordValueChange(PropertyInfo?.GetValue(Target), newColor);
            NotifyValueChanged(newColor);
        };
    }
}