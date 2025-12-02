using System;
using FEZRepacker.Core.Definitions.Game.Common;
using Godot;

namespace FEZEdit.Core;

public readonly struct Bounds(Vector3 min, Vector3 max)
{
    public static readonly Bounds Default = new(Vector3.Inf, -Vector3.Inf);

    public Vector3 Center => (max - min) / 2.0f;

    public Bounds Merge(Vector3 vector)
    {
        return new Bounds(min.Min(vector), max.Max(vector));
    }

    public Vector3 GetDepth(FaceOrientation orientation, float offset = 0.0f)
    {
        return orientation switch
        {
            FaceOrientation.Front => Vector3.Back * (max.Z + offset),
            FaceOrientation.Back => Vector3.Back * (min.Z - offset),
            FaceOrientation.Right => Vector3.Right * (max.X + offset),
            FaceOrientation.Left => Vector3.Right * (min.X - offset),
            FaceOrientation.Top => Vector3.Up * (max.Y + offset),
            FaceOrientation.Down => Vector3.Up * (min.Y - offset),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null)
        };
    }
}