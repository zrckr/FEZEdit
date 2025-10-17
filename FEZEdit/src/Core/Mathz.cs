using System.ComponentModel;
using Godot;

namespace FEZEdit.Core;

using FEZRepacker.Core.Definitions.Game.XNA;

using Color = Godot.Color;
using Quaternion = Godot.Quaternion;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

/// <summary>
/// A collection of helper methods
/// </summary>
// ReSharper disable once IdentifierTypo
public static class Mathz
{
    public static readonly Vector3 XzMask = Vector3.One - Vector3.Up;

    public const float PixelSize = 1f / 16f;

    // See: https://github.com/godotengine/godot/blob/master/core/math/aabb.cpp#L361
    public static void GetEdge(this Aabb aabb, int edge, out Vector3 from, out Vector3 to)
    {
        from = to = default;
        Vector3 position = aabb.Position;
        Vector3 size = aabb.Size;
        switch (edge)
        {
            case 0:
                {
                    from = new Vector3(position.X + size.X, position.Y, position.Z);
                    to = new Vector3(position.X, position.Y, position.Z);
                    break;
                }
            case 1:
                {
                    from = new Vector3(position.X + size.X, position.Y, position.Z + size.Z);
                    to = new Vector3(position.X + size.X, position.Y, position.Z);
                    break;
                }
            case 2:
                {
                    from = new Vector3(position.X, position.Y, position.Z + size.Z);
                    to = new Vector3(position.X + size.X, position.Y, position.Z + size.Z);
                    break;
                }
            case 3:
                {
                    from = new Vector3(position.X, position.Y, position.Z);
                    to = new Vector3(position.X, position.Y, position.Z + size.Z);
                    break;
                }
            case 4:
                {
                    from = new Vector3(position.X, position.Y + size.Y, position.Z);
                    to = new Vector3(position.X + size.X, position.Y + size.Y, position.Z);
                    break;
                }
            case 5:
                {
                    from = new Vector3(position.X + size.X, position.Y + size.Y, position.Z);
                    to = new Vector3(position.X + size.X, position.Y + size.Y, position.Z + size.Z);
                    break;
                }
            case 6:
                {
                    from = new Vector3(position.X + size.X, position.Y + size.Y, position.Z + size.Z);
                    to = new Vector3(position.X, position.Y + size.Y, position.Z + size.Z);
                    break;
                }
            case 7:
                {
                    from = new Vector3(position.X, position.Y + size.Y, position.Z + size.Z);
                    to = new Vector3(position.X, position.Y + size.Y, position.Z);
                    break;
                }
            case 8:
                {
                    from = new Vector3(position.X, position.Y, position.Z + size.Z);
                    to = new Vector3(position.X, position.Y + size.Y, position.Z + size.Z);
                    break;
                }
            case 9:
                {
                    from = new Vector3(position.X, position.Y, position.Z);
                    to = new Vector3(position.X, position.Y + size.Y, position.Z);
                    break;
                }
            case 10:
                {
                    from = new Vector3(position.X + size.X, position.Y, position.Z);
                    to = new Vector3(position.X + size.X, position.Y + size.Y, position.Z);
                    break;
                }
            case 11:
                {
                    from = new Vector3(position.X + size.X, position.Y, position.Z + size.Z);
                    to = new Vector3(position.X + size.X, position.Y + size.Y, position.Z + size.Z);
                    break;
                }
        }
    }

    // See: https://github.com/godotengine/godot/blob/master/core/math/basis.cpp#L262
    public static Basis ScaledOrthogonal(this Basis basis, Vector3 scale)
    {
        var s = new Vector3(-1, -1, -1) + scale;
        bool sign = (s.X + s.Y + s.Z) < 0;
        var b = basis.Orthonormalized();
        s *= b;
        var dots = Vector3.Zero;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                dots[j] += s[i] * Mathf.Abs(basis[i].Normalized().Dot(b[j]));
            }
        }

        if (sign != ((dots.X + dots.Y + dots.Z) < 0))
        {
            dots = -dots;
        }

        basis *= Basis.FromScale(Vector3.One + dots);
        return basis;
    }
    
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
}