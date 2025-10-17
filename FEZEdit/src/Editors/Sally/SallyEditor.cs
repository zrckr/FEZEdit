using System;
using FEZEdit.Content;
using FEZEdit.Core;

namespace FEZEdit.Editors.Sally;

public partial class SallyEditor : Editor
{
    public override event Action ValueChanged;

    public override object Value
    {
        get => _saveData;
        set => _saveData = (SaveData)value;
    }
    
    public override bool Disabled
    {
        set {}
    }
    
    private SaveData _saveData;
}