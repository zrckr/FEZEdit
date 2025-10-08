using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyTimeSpan : EditorProperty<TimeSpan>
{
    private static readonly string[] NodePaths = ["%SpinBoxH", "%SpinBoxM", "%SpinBoxS", "%SpinBoxMS"];

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
    
    protected override TimeSpan TypedValue
    {
        get
        {
            var hours = (int)_spinBoxes[0].Value;
            var minutes = (int)_spinBoxes[1].Value;
            var seconds = (int)_spinBoxes[2].Value;
            var milliseconds = (int)_spinBoxes[3].Value;
            var total = (hours * 3600000) + (minutes * 60000) + (seconds * 1000) + milliseconds;
            return TimeSpan.FromMilliseconds(total);
        }

        set
        {
            _spinBoxes[0].Value = value.Hours;
            _spinBoxes[1].Value = value.Minutes;
            _spinBoxes[2].Value = value.Seconds;
            _spinBoxes[3].Value = value.Milliseconds;
        }
    }
    
    protected override event Action<TimeSpan> TypedValueChanged;

    private readonly List<SpinBox> _spinBoxes = [];

    public override void _Ready()
    {
        base._Ready();
        foreach (var nodePath in NodePaths)
        {
            var spinBox = GetNode<SpinBox>(nodePath);
            spinBox.ValueChanged += _ => TypedValueChanged?.Invoke(TypedValue);
            _spinBoxes.Add(spinBox);
        }
    }
}