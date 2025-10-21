using System;
using System.Collections;
using System.Globalization;
using System.Text;
using FEZEdit.Core;
using Godot;

namespace FEZEdit.Extensions;

public static class StringExtensions
{
    public static (string, string) SplitAtExtension(this string instance)
    {
        int length = instance.Find(".");
        return length <= 0
            ? (instance, string.Empty)
            : (instance[..length], instance[length..]);
    }

    public static string WithoutBaseDirectory(this string instance, string baseDirectory)
    {
        int length = baseDirectory.Length;
        return length <= 0
            ? instance
            : instance[(baseDirectory.Length + 1)..];
    }

    public static string Stringify<T>(this T value)
    {
        switch (value)
        {
            case FEZRepacker.Core.Definitions.Game.XNA.Vector2 v2:
                {
                    return string.Format(CultureInfo.InvariantCulture, "X: {0:F}, Y: {1:F}", v2.X, v2.Y);
                }
            
            case FEZRepacker.Core.Definitions.Game.XNA.Vector3 v3:
                {
                    return string.Format(CultureInfo.InvariantCulture, "X: {0:F}, Y: {1:F}, Z: {2:F}", v3.X, v3.Y, v3.Z);
                }
            
            case FEZRepacker.Core.Definitions.Game.XNA.Vector4 v4:
                {
                    return string.Format(CultureInfo.InvariantCulture, "X: {0:F}, Y: {1:F}, Z: {2:F}, W: {3:F}", v4.X, v4.Y, v4.Z, v4.W);
                }
            
            case FEZRepacker.Core.Definitions.Game.XNA.Quaternion q:
                {
                    var euler = q.ToGodot().GetEuler();
                    euler.X = Mathf.RadToDeg(euler.X);
                    euler.Y = Mathf.RadToDeg(euler.Y);
                    euler.Z = Mathf.RadToDeg(euler.Z);
                    return string.Format(CultureInfo.InvariantCulture, "X: {0:F}, Y: {1:F}, Z: {2:F}", euler.X, euler.Y, euler.Z);
                }

            case FEZRepacker.Core.Definitions.Game.Level.Scripting.Entity e:
                {
                    return e.Type + (e.Identifier.HasValue ? $"[{e.Identifier.Value}]" : "");
                }

            case FEZRepacker.Core.Definitions.Game.Level.Scripting.ScriptTrigger st:
                {
                    return string.Join(".", st.Object.Stringify(), st.Event);
                }

            case FEZRepacker.Core.Definitions.Game.Level.Scripting.ScriptCondition sc:
                {
                    string @operator = sc.Operator switch
                    {
                        FEZRepacker.Core.Definitions.Game.Level.Scripting.ComparisonOperator.None => "",
                        FEZRepacker.Core.Definitions.Game.Level.Scripting.ComparisonOperator.Equal => "==",
                        FEZRepacker.Core.Definitions.Game.Level.Scripting.ComparisonOperator.Greater => ">",
                        FEZRepacker.Core.Definitions.Game.Level.Scripting.ComparisonOperator.GreaterEqual => ">=",
                        FEZRepacker.Core.Definitions.Game.Level.Scripting.ComparisonOperator.Less => "<",
                        FEZRepacker.Core.Definitions.Game.Level.Scripting.ComparisonOperator.LessEqual => "<=",
                        FEZRepacker.Core.Definitions.Game.Level.Scripting.ComparisonOperator.NotEqual => "!=",
                        _ => throw new ArgumentOutOfRangeException(nameof(sc.Operator), sc.Operator, null)
                    };
                    
                    return string.Join(".", sc.Object.Stringify(), sc.Property) + " " + @operator + " " + sc.Value;
                }

            case FEZRepacker.Core.Definitions.Game.Level.Scripting.ScriptAction sa:
                {
                    return string.Join(".", sa.Object.Stringify(), sa.Operation) +
                           "(" + string.Join(", ", sa.Arguments) + ")";
                }

            case IList list:
                {
                    var stringBuilder = new StringBuilder();
                    for (int i = 0; i < list.Count; i++)
                    {
                        stringBuilder.Append(list[i].Stringify());
                        if (i != list.Count - 1)
                        {
                            stringBuilder.Append(", ");
                        }
                    }
                    return stringBuilder.ToString();
                }

            default:
                {
                    return value.ToString();
                }
        }
    }
}