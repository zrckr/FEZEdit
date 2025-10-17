using FEZEdit.Gizmos;
using Godot;

namespace FEZEdit.Extensions;

/// <summary>
/// A collection of helper methods translated from the C++ Godot source.
/// They're required by <see cref="Gizmo3D"/> but lack binds to GDScript and C#.
/// </summary>
public static class GizmoExtensions
{
    /// <summary>
    /// Port of https://github.com/godotengine/godot/blob/master/scene/resources/material.cpp#L2856
    /// </summary>
    /// <param name="material">The material to alter.</param>
    /// <param name="alpha">If the material supports transparency.</param>
    public static void SetOnTopOfAlpha(this BaseMaterial3D material, bool alpha = false)
    {
        material.Transparency = alpha ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled;
        material.RenderPriority = (int) Material.RenderPriorityMax;
        material.NoDepthTest = true;
    }
}