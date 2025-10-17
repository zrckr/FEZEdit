using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Editors.Properties;

public abstract partial class EditorPropertyVector<T> : EditorProperty
{
    public bool Unit
    {
        set
        {
            foreach (var spinBox in _spinBoxes)
            {
                if (value)
                {
                    spinBox.MinValue = -1f;
                    spinBox.MaxValue = 1f;
                }
                else
                {
                    spinBox.MinValue = float.MinValue;
                    spinBox.MaxValue = float.MaxValue;
                }
            }
        }
    }

    public override bool Disabled
    {
        get => _spinBoxes.All(s => !s.Editable);
        set
        {
            foreach (var spinBox in _spinBoxes)
            {
                spinBox.Editable = !value;
            }
        }
    }

    protected abstract string[] Components { get; }

    protected readonly List<SpinBox> _spinBoxes = [];
    
    protected abstract T GetValueInternal();
    
    protected abstract void SetValueInternal(T value);
    
    public override void _Ready()
    {
        base._Ready();
        foreach (var component in Components)
        {
            var spinBox = GetNode<SpinBox>(component);
            spinBox.ValueChanged += OnSpinBoxValueChanged;
            _spinBoxes.Add(spinBox);
        }
    }

    protected override object GetValue()
    {
        return GetValueInternal();
    }

    protected override void SetValue(object value)
    {
        SetValueInternal((T)value);
    }

    private void OnSpinBoxValueChanged(double value)
    {
        var oldValue = PropertyInfo?.GetValue(Target);
        var newValue = GetValueInternal();
        
        if (!Equals(oldValue, newValue))
        {
            RecordValueChange(oldValue, newValue);
            NotifyValueChanged(newValue);
        }
    }
}