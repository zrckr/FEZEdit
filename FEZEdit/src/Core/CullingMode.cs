using System;
using System.Collections.Generic;
using Godot;

namespace FEZEdit.Core;

[Flags]
public enum CullingMode
{
    None = 0,
    Front = 1,
    Right = 2,
    Back = 4,
    Left = 8
}

public static class CullingModeExtensions
{
    private static readonly Dictionary<CullingMode, Vector3I> DepthVectors = new()
    {
        [CullingMode.Front] = Vector3I.Forward,
        [CullingMode.Right] = Vector3I.Right,
        [CullingMode.Back] = Vector3I.Back,
        [CullingMode.Left] = Vector3I.Left
    };
    
    private static readonly Dictionary<CullingMode, Vector3I> SideVectors = new()
    {
        [CullingMode.Front] = Vector3I.Left,
        [CullingMode.Right] = Vector3I.Back,
        [CullingMode.Back] = Vector3I.Right,
        [CullingMode.Left] = Vector3I.Forward
    };

    public static Vector3I GetDepth(this CullingMode mode)
    {
        return DepthVectors[mode];
    }
    
    public static Vector3I GetSide(this CullingMode mode)
    {
        return SideVectors[mode];
    }
}