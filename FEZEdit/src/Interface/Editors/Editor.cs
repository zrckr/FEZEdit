using FEZEdit.Loaders;
using Godot;

namespace FEZEdit.Interface.Editors;

public abstract partial class Editor : Control
{
    public abstract bool Disabled { set; }

    public abstract object Value { get; set; }

    public ILoader Loader { get; set; }

    public EditorHistory History { get; set; }
}