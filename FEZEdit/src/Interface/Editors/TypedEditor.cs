namespace FEZEdit.Interface.Editors;

public abstract partial class TypedEditor<T> : Editor
{
    public abstract T TypedValue { get; set; }

    public override object Value
    {
        get => TypedValue;
        set => TypedValue = (T)value;
    }
}