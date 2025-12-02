namespace FEZEdit.Editors;

public partial class EmptyEditor : Editor
{
    public override object Value { get; set; }

    public override bool Disabled { set {} }

    public override void _Refresh() { }
}