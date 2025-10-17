using System;
using System.Reflection;
using Godot;
using Godot.Collections;

namespace FEZEdit.Editors.Properties;

[Tool]
public partial class EditorPropertyFactory: Resource
{
    [Export] private Dictionary<string, PackedScene> _editorProperties = new();
    
    [Export] private PackedScene _defaultEditorProperty;
    
    [Export] private PackedScene _enumEditorProperty;

    public EditorProperty GetEditorProperty(Type type)
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
        
        var instance = scene.Instantiate<EditorProperty>();
        instance.Type = type;
        instance.PropertyFactory = this;
        return instance;
    }

    public EditorProperty GetEditorProperty(object target, PropertyInfo info)
    {
        var editorProperty = GetEditorProperty(info.PropertyType);
        editorProperty.Target = target;
        editorProperty.PropertyInfo = info;
        return editorProperty;
    }
}