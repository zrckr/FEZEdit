using Godot;

namespace FEZEdit.Core;

// ReSharper disable once IdentifierTypo
public static class Mathz
{
    public static readonly Vector3 XzMask = Vector3.One - Vector3.Up;
    
    public const float PixelSize = 1f / 16f;
}