using System;
using FEZEdit.Extensions;
using Godot;
using Color = FEZRepacker.Core.Definitions.Game.XNA.Color;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyColor : EditorProperty<Color>
{
    protected override Color TypedValue
    {
        get => _colorPickerButton.Color.ToXna();
        set => _colorPickerButton.Color = value.ToGodot();
    }

    public override bool Disabled
    {
        get => _colorPickerButton.Disabled;
        set => _colorPickerButton.Disabled = value;
    }

    protected override event Action<Color> TypedValueChanged;

    private ColorPickerButton _colorPickerButton;

    public override void _Ready()
    {
        base._Ready();
        _colorPickerButton = GetNode<ColorPickerButton>("ColorPickerButton");
        _colorPickerButton.ColorChanged += color => TypedValueChanged?.Invoke(color.ToXna());
    }
}