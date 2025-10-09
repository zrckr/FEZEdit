using Godot;

namespace FEZEdit.Interface.Editors;

public abstract partial class Editor : Control
{
    public abstract bool Disabled { set; }

    public abstract object Value { get; set; }
}

public abstract partial class Editor<T> : Editor
{
    public abstract T TypedValue { get; set; }

    public override object Value
    {
        get => TypedValue;
        set => TypedValue = (T)value;
    }
}