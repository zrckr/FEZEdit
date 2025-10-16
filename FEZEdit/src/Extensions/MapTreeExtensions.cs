using System;
using System.Collections.Generic;
using System.Linq;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.MapTree;
using Godot;

namespace FEZEdit.Extensions;

public static class MapTreeExtensions
{
    public static float GetSizeFactor(this LevelNodeType type)
    {
        return type switch
        {
            LevelNodeType.Hub => 2f,
            LevelNodeType.Lesser => 0.5f,
            LevelNodeType.Node => 1f,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static Vector3 AsVector(this FaceOrientation face)
    {
        return face switch
        {
            FaceOrientation.Left => Vector3.Left,
            FaceOrientation.Down => Vector3.Down,
            FaceOrientation.Back => Vector3.Forward,
            FaceOrientation.Right => Vector3.Right,
            FaceOrientation.Top => Vector3.Up,
            FaceOrientation.Front => Vector3.Back,
            _ => throw new ArgumentOutOfRangeException(nameof(face), face, null)
        };
    }

    public static FaceOrientation GetOpposite(this FaceOrientation face)
    {
        return face switch
        {
            FaceOrientation.Left => FaceOrientation.Right,
            FaceOrientation.Down => FaceOrientation.Top,
            FaceOrientation.Back => FaceOrientation.Front,
            FaceOrientation.Right => FaceOrientation.Left,
            FaceOrientation.Top => FaceOrientation.Down,
            FaceOrientation.Front => FaceOrientation.Back,
            _ => throw new ArgumentOutOfRangeException(nameof(face), face, null)
        };
    }

    public static bool IsSide(this FaceOrientation face)
    {
        return face is not (FaceOrientation.Down or FaceOrientation.Top);
    }

    public static (MapNode, MapNodeConnection) FindParentWithConnection(this MapTree tree, MapNode node)
    {
        var stack = new Stack<MapNode>();
        stack.Push(tree.Root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var currentConnection in current.Connections)
                if (currentConnection.Node == node)
                    return (current, currentConnection);
            foreach (var connection in current.Connections)
                stack.Push(connection.Node);
        }

        return (null, null);
    }
}