using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public abstract partial class EditorPropertyVector<T> : EditorProperty<T>
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

    protected override event Action<T> TypedValueChanged;

    protected abstract string[] Components { get; }

    protected readonly List<SpinBox> _spinBoxes = [];

    public override void _Ready()
    {
        base._Ready();
        foreach (var component in Components)
        {
            var spinBox = GetNode<SpinBox>(component);
            spinBox.ValueChanged += _ => TypedValueChanged?.Invoke(TypedValue);
            _spinBoxes.Add(spinBox);
        }
    }
}