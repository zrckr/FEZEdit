using System;
using System.Reflection;
using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public class EnumPropertyEditor : PropertyEditor<Enum>
{
    private OptionButton _optionButton;

    private Array _enumValues;

    public override void SetTypedValue(Enum value)
    {
        if (value == null)
        {
            return;
        }

        if (_enumValues == null)
        {
            _enumValues = Enum.GetValues(value.GetType());
            foreach (var enumValue in _enumValues)
            {
                _optionButton.AddItem(enumValue.ToString(), (int)enumValue);
            }
        }

        var index = Array.IndexOf(_enumValues, value);
        if (index >= 0)
        {
            _optionButton.Selected = index;
        }
    }

    public override Enum GetTypedValue()
    {
        if (_optionButton.Selected >= 0)
        {
            return (Enum)_enumValues.GetValue(_optionButton.Selected);
        }

        return null;
    }

    public override Control CreateControl()
    {
        _optionButton = new OptionButton { Selected = 0, Disabled = true, Alignment = HorizontalAlignment.Center };
        return _optionButton;
    }
}