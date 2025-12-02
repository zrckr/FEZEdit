using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Serilog;

namespace FEZEdit.Memento;

public class Memento
{
    private static readonly ILogger Logger = LoggerFactory.Create<Memento>();
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public string Description { get; }
    
    public DateTime Timestamp { get; }
    
    private readonly Dictionary<string, object> _state = new();

    public Memento(object target, string description = null)
    {
        Description = description ?? "State change";
        Timestamp = DateTime.Now;
        CaptureState(target);
    }

    private void CaptureState(object target)
    {
        if (target == null)
        {
            return;
        }

        var type = target.GetType();
        var properties = GetCapturableProperties(type);

        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(target);
                _state[prop.Name] = DeepClone(value, prop.PropertyType);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error capturing property {0}", prop.Name);
            }
        }
    }

    private static object DeepClone(object value, Type type)
    {
        if (value == null)
        {
            return null;
        }
        
        if (type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(DateTime) || 
            type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(decimal))
        {
            return value;
        }

        try
        {
            var json = JsonSerializer.Serialize(value, type, JsonOptions);
            return JsonSerializer.Deserialize(json, type, JsonOptions);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to deep clone type {0}, storing reference", type.Name);
            return value; // Fallback to reference (memento will be broken for this property)
        }
    }

    public void RestoreState(object target)
    {
        if (target == null)
        {
            return;
        }

        var type = target.GetType();
        var properties = GetCapturableProperties(type);

        foreach (var prop in properties)
        {
            if (_state.TryGetValue(prop.Name, out var value))
            {
                try
                {
                    prop.SetValue(target, value);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error restoring property {0}", prop.Name);
                }
            }
        }
    }

    private static IEnumerable<PropertyInfo> GetCapturableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => IsSupportedType(p.PropertyType));
    }

    private static bool IsSupportedType(Type type)
    {
        if (type.IsPrimitive || type.IsValueType || type == typeof(string) || type == typeof(DateTime) || 
            type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(decimal))
        {
            return true;
        }
        
        if (type.IsClass)
        {
            return !typeof(Delegate).IsAssignableFrom(type);
        }

        return false;
    }

    public bool HasChanges(object target)
    {
        if (target == null)
        {
            return false;
        }

        var type = target.GetType();
        var properties = GetCapturableProperties(type);

        foreach (var prop in properties)
        {
            if (_state.TryGetValue(prop.Name, out var savedValue))
            {
                var currentValue = prop.GetValue(target);
                if (!AreEqual(savedValue, currentValue, prop.PropertyType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool AreEqual(object a, object b, Type type)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }
        
        if (type.IsValueType || type == typeof(string))
        {
            return Equals(a, b);
        }
        
        try
        {
            var jsonA = JsonSerializer.Serialize(a, type, JsonOptions);
            var jsonB = JsonSerializer.Serialize(b, type, JsonOptions);
            return jsonA == jsonB;
        }
        catch
        {
            return Equals(a, b); // Fallback to reference equality
        }
    }
}