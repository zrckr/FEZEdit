using System;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyBoolean : EditorProperty<bool>
{
    protected override bool TypedValue
    {
        get => _checkBox.ButtonPressed;
        set => _checkBox.ButtonPressed = value;
    }

    public override bool Disabled
    {
        get => _checkBox.Disabled;
        set => _checkBox.Disabled = value;
    }

    protected override event Action<bool> TypedValueChanged;

    private CheckBox _checkBox;

    public override void _Ready()
    {
        base._Ready();
        _checkBox = GetNode<CheckBox>("CheckBox");
        _checkBox.Pressed += () => TypedValueChanged?.Invoke(TypedValue);
    }
}