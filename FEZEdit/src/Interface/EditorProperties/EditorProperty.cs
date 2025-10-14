using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public interface IEditorProperty
{
    string Label { get; set; }
    
    bool Disabled { get; set; }
    
    object Value { get; set; }
    
    event Action<object> ValueChanged;
    
    EditorPropertyFactory PropertyFactory { set; }
    
    UndoRedo UndoRedo { set; }
    
    Type Type { set; }
    
    object Target { get; set; }
    
    PropertyInfo PropertyInfo { get; set; }
}

public abstract partial class EditorProperty<T> : Control, IEditorProperty
{
    public string Label
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public abstract bool Disabled { get; set; }

    public object Value
    {
        get => TypedValue;
        set
        {
            var oldValue= TypedValue;
            var newValue = (T)value;
            if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                TypedValue = newValue;
                RecordValueChange(oldValue, newValue);
            }
        }
    }
    
    public event Action<object> ValueChanged;
    
    public UndoRedo UndoRedo { protected get; set; }
    
    public EditorPropertyFactory PropertyFactory { protected get; set; }
    
    public object Target { get; set; }
    
    public PropertyInfo PropertyInfo { get; set; }
    
    public Type Type { protected get; set; }
    
    protected abstract T TypedValue { get; set; }
    
    protected abstract event Action<T> TypedValueChanged;

    private Label _label;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        TypedValueChanged += value => ValueChanged?.Invoke(value);
    }
    
    protected virtual void RecordValueChange(T oldValue, T newValue)
    {
        if (!UndoRedo.IsCommitting)
        {
            UndoRedo.CreateAction($"Change {Label}");
            UndoRedo.AddUndoProperty(
                () => (T)PropertyInfo.GetValue(Target),
                value => PropertyInfo.SetValue(Target, value),
                oldValue
            );
            UndoRedo.AddDoProperty(
                () => (T)PropertyInfo.GetValue(Target),
                value => PropertyInfo.SetValue(Target, value),
                newValue
            );
            UndoRedo.CommitAction();
        }
    }
}