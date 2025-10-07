using System;
using System.Reflection;
using Godot;

namespace FEZEdit.Interface.PropertyEditors;

public interface IPropertyEditor
{
    Type PropertyType { get; }
    
    object GetValue();
    
    void SetValue(object value);
    
    Control CreateControl();
}