using System;
using Godot;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyString : EditorProperty<string>
{
    protected override string TypedValue
    {
        get => _lineEdit.Text;
        set => _lineEdit.Text = value;
    }

    public override bool Disabled
    {
        get => !_lineEdit.Editable;
        set => _lineEdit.Editable = !value;
    }

    protected override event Action<string> TypedValueChanged;
    
    private LineEdit _lineEdit;

    public override void _Ready()
    {
        base._Ready();
        _lineEdit = GetNode<LineEdit>("LineEdit");
        _lineEdit.TextChanged += text => TypedValueChanged?.Invoke(text);
        _lineEdit.TextChangeRejected += text => TypedValueChanged?.Invoke(text);
    }
}