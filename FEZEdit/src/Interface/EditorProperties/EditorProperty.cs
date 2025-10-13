using System;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public interface IEditorProperty
{
    string Label { get; set; }
    
    bool Disabled { get; set; }
    
    object Value { get; set; }
    
    event Action<object> ValueChanged;
    
    EditorPropertyFactory Factory { set; }
    
    Type Type { set; }
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
        set => TypedValue = (T) value;
    }
    
    public event Action<object> ValueChanged;
    
    public EditorPropertyFactory Factory { protected get; set; }
    
    public Type Type { protected get; set; }
    
    protected abstract T TypedValue { get; set; }
    
    protected abstract event Action<T> TypedValueChanged;

    private Label _label;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        TypedValueChanged += value => ValueChanged?.Invoke(value);
    }
}