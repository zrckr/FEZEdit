using System;
using System.Collections.Generic;
using Godot;
using Vector2 = FEZRepacker.Core.Definitions.Game.XNA.Vector2;
using Vector3 = FEZRepacker.Core.Definitions.Game.XNA.Vector3;
using Vector4 = FEZRepacker.Core.Definitions.Game.XNA.Vector4;

namespace FEZEdit.Interface.PropertyEditors;

public abstract class VectorPropertyEditor<T> : PropertyEditor<T>
{
    private VBoxContainer _container;
    
    private readonly List<SpinBox> _spinBoxes = [];
    
    protected abstract int ComponentCount { get; }

    protected abstract string GetVectorAxis(int index);
    
    protected abstract float GetVectorValue(T vector, int index);
    
    protected abstract void SetVectorValue(ref T vector, int index, float value);

    public override void SetTypedValue(T vector)
    {
        for (int i = 0; i < ComponentCount; i++)
        {
            var value = GetVectorValue(vector, i);
            _spinBoxes[i].Value = value;
        }
    }

    public override T GetTypedValue()
    {
        var vector = default(T);
        for (int i = 0; i < ComponentCount; i++)
        {
            var value = (float)_spinBoxes[i].Value;
            SetVectorValue(ref vector, i, value);
        }
        return vector;
    }

    public override Control CreateControl()
    {
        _container = new VBoxContainer();
        for (int i = 0; i < ComponentCount; i++)
        {
            var spinBox = new SpinBox
            {
                MinValue = float.MinValue,
                MaxValue = float.MaxValue,
                Step = 0.1f,
                Prefix = GetVectorAxis(i),
                Editable = false,
                Alignment = HorizontalAlignment.Center
            };
            _spinBoxes.Add(spinBox);
            _container.AddChild(spinBox);
        }
        
        return _container;
    }
}

public class Vector2PropertyEditor : VectorPropertyEditor<Vector2>
{
    protected override int ComponentCount => 2;

    protected override string GetVectorAxis(int index)
    {
        if (index == 0) return "x";
        if (index == 1) return "y";
        throw new IndexOutOfRangeException();
    }

    protected override float GetVectorValue(Vector2 vector, int index)
    {
        if (index == 0) return vector.X;
        if (index == 1) return vector.Y;
        throw new IndexOutOfRangeException();
    }

    protected override void SetVectorValue(ref Vector2 vector, int index, float value)
    {
        if (index == 0) vector.X = value;
        if (index == 1) vector.Y = value;
        throw new IndexOutOfRangeException();
    }
}

public class Vector3PropertyEditor : VectorPropertyEditor<Vector3>
{
    protected override int ComponentCount => 3;
    
    protected override string GetVectorAxis(int index)
    {
        if (index == 0) return "x";
        if (index == 1) return "y";
        if (index == 2) return "z";
        throw new IndexOutOfRangeException();
    }

    protected override float GetVectorValue(Vector3 vector, int index)
    {
        if (index == 0) return vector.X;
        if (index == 1) return vector.Y;
        if (index == 2) return vector.Z;
        throw new IndexOutOfRangeException();
    }

    protected override void SetVectorValue(ref Vector3 vector, int index, float value)
    {
        if (index == 0) vector.X = value;
        if (index == 1) vector.Y = value;
        if (index == 2) vector.Z = value;
        throw new IndexOutOfRangeException();
    }
}

public class Vector4PropertyEditor : VectorPropertyEditor<Vector4>
{
    protected override int ComponentCount => 4;
    
    protected override string GetVectorAxis(int index)
    {
        if (index == 0) return "x";
        if (index == 1) return "y";
        if (index == 2) return "z";
        if (index == 3) return "w";
        throw new IndexOutOfRangeException();
    }

    protected override float GetVectorValue(Vector4 vector, int index)
    {
        if (index == 0) return vector.X;
        if (index == 1) return vector.Y;
        if (index == 2) return vector.Z;
        if (index == 3) return vector.W;
        throw new IndexOutOfRangeException();
    }

    protected override void SetVectorValue(ref Vector4 vector, int index, float value)
    {
        if (index == 0) vector.X = value;
        if (index == 1) vector.Y = value;
        if (index == 2) vector.Z = value;
        if (index == 3) vector.W = value;
        throw new IndexOutOfRangeException();
    }
}
