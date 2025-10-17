using System;
using Godot;

namespace FEZEdit.Editors;

public partial class EditorFactory : Resource
{
    [Export] private Godot.Collections.Dictionary<string, PackedScene> _editors = new();
    
    [Export] private PackedScene _emptyEditor;
    
    [Export] private PackedScene _unsupportedEditor;
    
    [Export] private PackedScene _saveSlotEditor;
    
    public Editor EmptyEditor => _emptyEditor.Instantiate<Editor>();

    public Editor UnsupportedEditor => _unsupportedEditor.Instantiate<Editor>();

    public Editor SaveSlotEditor => _saveSlotEditor.Instantiate<Editor>();

    public bool TryGetEditor(Type type, out Editor editor)
    {
        var typeName = type.ToString();
        if (_editors.TryGetValue(typeName, out var scene))
        {
            editor = scene.Instantiate<Editor>();
            return true;
        }
        editor = null;
        return false;
    }
}