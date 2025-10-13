using System;
using System.Reflection;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyEnum : EditorProperty<Enum>
{
    protected override Enum TypedValue
    {
        get
        {
            if (_optionButton.Selected >= 0)
            {
                return (Enum)_enumValues.GetValue(_optionButton.Selected);
            }

            return null;
        }
        set
        {
            var index = Array.IndexOf(_enumValues, value);
            if (index >= 0)
            {
                _optionButton.Selected = index;
            }
        }
    }

    public override bool Disabled
    {
        get => _optionButton.Disabled;
        set => _optionButton.Disabled = value;
    }

    protected override event Action<Enum> TypedValueChanged;

    private OptionButton _optionButton;

    private Array _enumValues;

    public override void _Ready()
    {
        base._Ready();
        _enumValues = Enum.GetValues(Type);
        _optionButton = GetNode<OptionButton>("OptionButton");
        _optionButton.ItemSelected += index => TypedValueChanged?.Invoke((Enum)_enumValues.GetValue(index));
        foreach (var enumValue in _enumValues)
        {
            _optionButton.AddItem(enumValue.ToString(), (int)enumValue);
        }
    }
}