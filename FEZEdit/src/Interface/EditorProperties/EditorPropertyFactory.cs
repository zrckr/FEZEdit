using System;
using Godot;
using Godot.Collections;

namespace FEZEdit.Interface.EditorProperties;

[Tool]
public partial class EditorPropertyFactory: Resource
{
    [Export] private Dictionary<string, PackedScene> _editorProperties = new();
    
    [Export] private PackedScene _defaultEditorProperty;
    
    [Export] private PackedScene _enumEditorProperty;

    public IEditorProperty GetEditorProperty(Type type)
    {
        var scene = _defaultEditorProperty;
        var typeName = type.ToString().Split('`')[0];
        if (type.IsEnum)
        {
            scene = _enumEditorProperty;
        }
        else if (_editorProperties.TryGetValue(typeName, out var typeScene))
        {
            scene = typeScene;
        }
        
        var instance = scene.Instantiate<IEditorProperty>();
        instance.Type = type;
        instance.Factory = this;
        return instance;
    }
}