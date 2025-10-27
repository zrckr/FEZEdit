using System;
using System.Reflection;
using Godot;
using Godot.Collections;
using Serilog;

namespace FEZEdit.Editors.Properties;

[Tool]
public partial class EditorPropertyFactory: Resource
{
    private static readonly ILogger Logger = LoggerFactory.Create<EditorPropertyFactory>();
    
    [Export] private Dictionary<string, PackedScene> _editorProperties = new();
    
    [Export] private PackedScene _defaultEditorProperty;
    
    [Export] private PackedScene _enumEditorProperty;
    
    [Export] private PackedScene _nullableEditorProperty;

    [Export] private PackedScene _arrayEditorProperty;
    
    [Export] private PackedScene _objectEditorProperty;

    public EditorProperty GetEditorProperty(Type type)
    {
        PackedScene scene;

        var typeName = type.ToString().Split('`')[0];
        if (type.IsValueType && Nullable.GetUnderlyingType(type) != null)
        {
            scene = _nullableEditorProperty;
        }
        else if (type.IsEnum)
        {
            scene = _enumEditorProperty;
        }
        else if (type.IsArray)
        {
            scene = _arrayEditorProperty;
        }
        else if (IsCustomClass(type))
        {
            scene = _objectEditorProperty;
        }
        else if (_editorProperties.TryGetValue(typeName, out var typeScene))
        {
            scene = typeScene;
        }
        else
        {
            scene = _defaultEditorProperty;
            Logger.Warning("Unknown editor property type '{0}'", type.Name);
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

    private static bool IsCustomClass(Type type)
    {
        return type.IsClass && 
               type != typeof(string) && 
               !type.IsArray &&
               !type.Namespace?.StartsWith("System.") == true &&
               !type.Namespace?.StartsWith("Microsoft.") == true;
    }
}