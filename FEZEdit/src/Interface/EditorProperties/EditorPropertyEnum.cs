using System;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyEnum : EditorProperty
{
    public override bool Disabled
    {
        get => _optionButton.Disabled;
        set => _optionButton.Disabled = value;
    }

    private OptionButton _optionButton;

    private Array _enumValues;
    
    protected override object GetValue()
    {
        return _optionButton.Selected >= 0 
            ? _enumValues.GetValue(_optionButton.Selected) 
            : null;
    }

    protected override void SetValue(object value)
    {
        var index = Array.IndexOf(_enumValues, value);
        if (index >= 0)
        {
            _optionButton.Selected = index;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        _enumValues = Enum.GetValues(Type);
        _optionButton = GetNode<OptionButton>("OptionButton");
        
        _optionButton.ItemSelected += index => 
        {
            var newValue = _enumValues.GetValue(index);
            RecordValueChange(PropertyInfo?.GetValue(Target), newValue);
            NotifyValueChanged(newValue);
        };
        
        foreach (var enumValue in _enumValues)
        {
            _optionButton.AddItem(enumValue.ToString(), (int)enumValue);
        }
    }
}