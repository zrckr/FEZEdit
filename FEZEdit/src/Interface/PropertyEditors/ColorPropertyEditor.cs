using FEZEdit.Extensions;
using Godot;
using Color = FEZRepacker.Core.Definitions.Game.XNA.Color;

namespace FEZEdit.Interface.PropertyEditors;

public class ColorPropertyEditor: PropertyEditor<Color>
{
    private ColorPickerButton _colorPickerButton;

    public override void SetTypedValue(Color value) => _colorPickerButton?.SetPickColor(value.ToGodot());

    public override Color GetTypedValue() => (_colorPickerButton?.Color ?? Colors.White).ToXna();
    
    public override Control CreateControl()
    {
        _colorPickerButton = new ColorPickerButton
        {
            EditAlpha = false,
            EditIntensity = false,
            Disabled = true
        };
        return _colorPickerButton;
    }

}