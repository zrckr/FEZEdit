using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Rectangle = FEZRepacker.Core.Definitions.Game.XNA.Rectangle;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyRectangle : EditorProperty<Rectangle>
{
    private static readonly string[] NodePaths = ["%SpinBoxX", "%SpinBoxY", "%SpinBoxW", "%SpinBoxH"];

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
    
    protected override Rectangle TypedValue
    {
        get => new(
            x: (int)_spinBoxes[0].Value,
            y: (int)_spinBoxes[1].Value,
            width: (int)_spinBoxes[2].Value,
            height: (int)_spinBoxes[3].Value
        );

        set
        {
            _spinBoxes[0].Value = value.X;
            _spinBoxes[1].Value = value.Y;
            _spinBoxes[2].Value = value.Width;
            _spinBoxes[3].Value = value.Height;
        }
    }
    
    protected override event Action<Rectangle> TypedValueChanged;

    private readonly List<SpinBox> _spinBoxes = [];

    public override void _Ready()
    {
        base._Ready();
        foreach (var nodePath in NodePaths)
        {
            var spinBox = GetNode<SpinBox>(nodePath);
            spinBox.MinValue = int.MinValue;
            spinBox.MaxValue = int.MaxValue;
            spinBox.ValueChanged += _ => TypedValueChanged?.Invoke(TypedValue);
            _spinBoxes.Add(spinBox);
        }
    }
}