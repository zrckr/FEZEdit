using System.ComponentModel;
using FEZRepacker.Core.Definitions.Game.XNA;
using Godot;
using Color = Godot.Color;
using Quaternion = Godot.Quaternion;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

namespace FEZEdit.Extensions;

public static class GodotExtensions
{
    #region XNA -> Godot

    public static Vector3 ToGodot(this FEZRepacker.Core.Definitions.Game.XNA.Vector3 vector)
        => new(vector.X, vector.Y, vector.Z);

    public static Vector2 ToGodot(this FEZRepacker.Core.Definitions.Game.XNA.Vector2 vector)
        => new(vector.X, vector.Y);

    public static Quaternion ToGodot(this FEZRepacker.Core.Definitions.Game.XNA.Quaternion quaternion)
        => new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

    public static Color ToGodot(this FEZRepacker.Core.Definitions.Game.XNA.Color color)
        => Color.Color8(color.R, color.G, color.B, color.A);

    public static Rect2I ToGodot(this FEZRepacker.Core.Definitions.Game.XNA.Rectangle rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static Mesh.PrimitiveType ToGodot(this FEZRepacker.Core.Definitions.Game.XNA.PrimitiveType primitive)
    {
        return primitive switch
        {
            PrimitiveType.TriangleList => Mesh.PrimitiveType.Triangles,
            PrimitiveType.TriangleStrip => Mesh.PrimitiveType.TriangleStrip,
            PrimitiveType.LineList => Mesh.PrimitiveType.Lines,
            PrimitiveType.LineStrip => Mesh.PrimitiveType.LineStrip,
            _ => throw new InvalidEnumArgumentException()
        };
    }

    #endregion

    #region Godot -> XNA

    public static FEZRepacker.Core.Definitions.Game.XNA.Vector3 ToXna(this Vector3 vector)
        => new(vector.X, vector.Y, vector.Z);

    public static FEZRepacker.Core.Definitions.Game.XNA.Vector2 ToXna(this Vector2 vector)
        => new(vector.X, vector.Y);

    public static FEZRepacker.Core.Definitions.Game.XNA.Quaternion ToXna(this Quaternion quaternion)
        => new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

    public static FEZRepacker.Core.Definitions.Game.XNA.Color ToXna(this Color color)
        => new((byte)color.R8, (byte)color.G8, (byte)color.B8, (byte)color.A8);

    public static FEZRepacker.Core.Definitions.Game.XNA.Rectangle ToXna(this Rect2I rect)
        => new(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y);

    public static FEZRepacker.Core.Definitions.Game.XNA.PrimitiveType ToXna(this Mesh.PrimitiveType primitive)
    {
        return primitive switch
        {
            Mesh.PrimitiveType.Triangles => PrimitiveType.TriangleList,
            Mesh.PrimitiveType.TriangleStrip => PrimitiveType.TriangleStrip,
            Mesh.PrimitiveType.Lines => PrimitiveType.LineList,
            Mesh.PrimitiveType.LineStrip => PrimitiveType.LineStrip,
            _ => throw new InvalidEnumArgumentException()
        };
    }

    #endregion

    #region Various

    public static Shortcut WithShift(this Key key)
    {
        var shortcut = new Shortcut();
        shortcut.Events.Add(new InputEventKey() { Keycode = key, ShiftPressed = true });
        return shortcut;
    }
    
    public static Shortcut WithCtrl(this Key key)
    {
        var shortcut = new Shortcut();
        shortcut.Events.Add(new InputEventKey() { Keycode = key, CtrlPressed = true });
        return shortcut;
    }
    
    public static Shortcut WithCtrlShift(this Key key)
    {
        var shortcut = new Shortcut();
        shortcut.Events.Add(new InputEventKey() { Keycode = key, CtrlPressed = true, ShiftPressed = true });
        return shortcut;
    }

    #endregion
}