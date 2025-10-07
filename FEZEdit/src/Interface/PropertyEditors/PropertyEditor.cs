using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public abstract class PropertyEditor<T> : IPropertyEditor
{
    public Type PropertyType => typeof(T);
    
    public abstract Control CreateControl();
    
    public abstract void SetTypedValue(T value);

    public abstract T GetTypedValue();
    
    public void SetValue(object value) => SetTypedValue((T) value);
    
    public object GetValue() => GetTypedValue();
}