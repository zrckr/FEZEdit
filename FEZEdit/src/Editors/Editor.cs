using System;
using Godot;

namespace FEZEdit.Editors;

public abstract partial class Editor : Control
{
    public abstract bool Disabled { set; }

    public abstract object Value { get; set; }

    public abstract event Action ValueChanged;

    public UndoRedo UndoRedo { get; } = new();
}