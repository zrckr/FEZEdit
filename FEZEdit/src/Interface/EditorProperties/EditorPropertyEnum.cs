using System;
using System.Reflection;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyEnum : EditorProperty<Enum>
{
    [Export] public string TypeFullName { get; set; }
    
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

    private Type _type;

    public override void _Ready()
    {
        base._Ready();
        _type = Type.GetType(TypeFullName)!;
        _enumValues = Enum.GetValues(_type);
        
        _optionButton = GetNode<OptionButton>("OptionButton");
        _optionButton.ItemSelected += index => TypedValueChanged?.Invoke((Enum)_enumValues.GetValue(index));
        foreach (var enumValue in _enumValues)
        {
            _optionButton.AddItem(enumValue.ToString(), (int)enumValue);
        }
    }
    
    public override void SetGenericArguments(params Type[] types) => TypeFullName = types[0].FullName;
}