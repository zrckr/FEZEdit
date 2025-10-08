using System;
using Godot;
using Godot.Collections;

namespace FEZEdit.Interface.EditorProperties;

[Tool]
public partial class EditorPropertyFactory: Resource
{
    [Export] private Dictionary<string, PackedScene> _editorProperties = new();
    
    [Export] private PackedScene _defaultEditorProperty;

    public IEditorProperty GetEditorProperty(Type type)
    {
        var scene = _defaultEditorProperty;
        if (_editorProperties.TryGetValue(type.FullName!, out var typeScene))
        {
            scene = typeScene;
        }
        
        var instance = scene.Instantiate<IEditorProperty>();
        if (type.IsGenericType)
        {
            instance.SetGenericArguments(type.GetGenericArguments());
        }
        
        return instance;
    }
}