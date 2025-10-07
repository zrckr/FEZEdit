using System;
using System.Collections.Generic;
using Godot;
using Quaternion = FEZRepacker.Core.Definitions.Game.XNA.Quaternion;

namespace FEZEdit.Interface.PropertyEditors;

public class QuaternionPropertyEditor : PropertyEditor<Quaternion>
{
    private const int ComponentSize = 4;
    
    private VBoxContainer _container;
    
    private readonly List<SpinBox> _spinBoxes = [];

    public override void SetTypedValue(Quaternion quaternion)
    {
        for (int i = 0; i < ComponentSize; i++)
        {
            var value = GetQuaternionValue(quaternion, i);
            _spinBoxes[i].Value = value;
        }
    }

    public override Quaternion GetTypedValue()
    {
        var quaternion = new Quaternion();
        for (int i = 0; i < ComponentSize; i++)
        {
            var value = (float) _spinBoxes[i].Value;
            SetQuaternionValue(ref quaternion, i, value);
        }
        return quaternion;
    }
    
    public override Control CreateControl()
    {
        _container = new VBoxContainer();
        for (int i = 0; i < ComponentSize; i++)
        {
            var spinBox = new SpinBox
            {
                MinValue = -1.0f,
                MaxValue = 1.0f,
                Step = 0.01f,
                Prefix = GetQuaternionAxis(i),
                Editable = false,
                Alignment = HorizontalAlignment.Center
            };
            _spinBoxes.Add(spinBox);
            _container.AddChild(spinBox);
        }
        
        return _container;
    }

    private static string GetQuaternionAxis(int index)
    {
        if (index == 0) return "x";
        if (index == 1) return "y";
        if (index == 2) return "z";
        if (index == 3) return "w";
        throw new IndexOutOfRangeException();
    }
    
    private static float GetQuaternionValue(Quaternion quaternion, int index)
    {
        if (index == 0) return quaternion.X;
        if (index == 1) return quaternion.Y;
        if (index == 2) return quaternion.Z;
        if (index == 3) return quaternion.W;
        throw new IndexOutOfRangeException();
    }

    private static void SetQuaternionValue(ref Quaternion quaternion, int index, float value)
    {
        if (index == 0) quaternion.X = value;
        if (index == 1) quaternion.Y = value;
        if (index == 2) quaternion.Z = value;
        if (index == 3) quaternion.W = value;
        throw new IndexOutOfRangeException();
    }
}