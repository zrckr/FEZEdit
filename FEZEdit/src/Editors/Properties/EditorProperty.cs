using System;
using System.Reflection;
using Godot;

namespace FEZEdit.Editors.Properties;

public abstract partial class EditorProperty : Control
{
    public string Label
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public string Tooltip
    {
        get => _label.TooltipText;
        set => _label.TooltipText = value;
    }
    
    public abstract bool Disabled { get; set; }

    public object Value
    {
        get => GetValue();
        set
        {
            var oldValue = GetValue();
            if (!Equals(oldValue, value))
            {
                SetValue(value);
                RecordValueChange(oldValue, value);
            }
        }
    }
    
    public event Action<object> ValueChanged;
    
    public UndoRedo UndoRedo { get; set; }
    
    public EditorPropertyFactory PropertyFactory { get; set; }
    
    public object Target { get; set; }
    
    public PropertyInfo PropertyInfo { get; set; }
    
    public Type Type { get; set; }

    private Label _label;
    
    private bool _isSettingValueFromCode;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
    }
    
    protected abstract object GetValue();
    
    protected abstract void SetValue(object value);
    
    protected void NotifyValueChanged(object newValue)
    {
        ValueChanged?.Invoke(newValue);
    }
    
    protected void RecordValueChange(object oldValue, object newValue)
    {
        if (UndoRedo?.IsCommitting == false)
        {
            UndoRedo.CreateAction(name: $"Change {Label}", tag: Target);
            UndoRedo.AddUndoProperty(
                () => PropertyInfo.GetValue(Target),
                value => PropertyInfo.SetValue(Target, value),
                oldValue
            );
            UndoRedo.AddDoProperty(
                () => PropertyInfo.GetValue(Target),
                value => PropertyInfo.SetValue(Target, value),
                newValue
            );
            UndoRedo.CommitAction();
        }
    }
}