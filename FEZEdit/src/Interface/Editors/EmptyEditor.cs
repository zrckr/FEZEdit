using System;

namespace FEZEdit.Interface.Editors;

public partial class EmptyEditor : Editor
{
    public override event Action ValueChanged;
    
    public override object Value { get; set; }

    public override bool Disabled { set {} }
}