using System;
using System.Collections.Generic;

namespace FEZEdit.Interface.Viewers;

public partial class EmptyViewer : Viewer
{
    public override event Action<object> ObjectSelected;
    
    public override Dictionary<Type, Type> Materializers { get; } = new();
}