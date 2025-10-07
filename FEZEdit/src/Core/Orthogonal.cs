using System.Collections.Generic;
using Godot;

namespace FEZEdit.Core;

public enum Orthogonal : sbyte
{
    Invalid = -1,
    FrontUp,
    FrontRight,
    FrontDown,
    FrontLeft,
    RightUp,
    RightRight,
    RightDown,
    RightLeft,
    BackUp,
    BackRight,
    BackDown,
    BackLeft,
    LeftUp,
    LeftRight,
    LeftDown,
    LeftLeft,
    UpUp,
    UpRight,
    UpDown,
    UpLeft,
    DownUp,
    DownRight,
    DownDown,
    DownLeft
}

public static class OrthogonalExtensions
{
    private static readonly Dictionary<Orthogonal, Basis> Bases = new() {
        { Orthogonal.FrontUp, new Basis(1, 0, 0, 0, 1, 0, 0, 0, 1) },
        { Orthogonal.FrontLeft, new Basis(0, -1, 0, 1, 0, 0, 0, 0, 1) },
        { Orthogonal.FrontDown, new Basis(-1, 0, 0, 0, -1, 0, 0, 0, 1) },
        { Orthogonal.FrontRight, new Basis(0, 1, 0, -1, 0, 0, 0, 0, 1) },
        { Orthogonal.DownUp, new Basis(1, 0, 0, 0, 0, -1, 0, 1, 0) },
        { Orthogonal.RightRight, new Basis(0, 0, 1, 1, 0, 0, 0, 1, 0) },
        { Orthogonal.UpUp, new Basis(-1, 0, 0, 0, 0, 1, 0, 1, 0) },
        { Orthogonal.LeftLeft, new Basis(0, 0, -1, -1, 0, 0, 0, 1, 0) },
        { Orthogonal.BackDown, new Basis(1, 0, 0, 0, -1, 0, 0, 0, -1) },
        { Orthogonal.BackRight, new Basis(0, 1, 0, 1, 0, 0, 0, 0, -1) },
        { Orthogonal.BackUp, new Basis(-1, 0, 0, 0, 1, 0, 0, 0, -1) },
        { Orthogonal.BackLeft, new Basis(0, -1, 0, -1, 0, 0, 0, 0, -1) },
        { Orthogonal.UpDown, new Basis(1, 0, 0, 0, 0, 1, 0, -1, 0) },
        { Orthogonal.LeftRight, new Basis(0, 0, -1, 1, 0, 0, 0, -1, 0) },
        { Orthogonal.DownDown, new Basis(-1, 0, 0, 0, 0, -1, 0, -1, 0) },
        { Orthogonal.RightLeft, new Basis(0, 0, 1, -1, 0, 0, 0, -1, 0) },
        { Orthogonal.RightUp, new Basis(0, 0, 1, 0, 1, 0, -1, 0, 0) },
        { Orthogonal.UpLeft, new Basis(0, -1, 0, 0, 0, 1, -1, 0, 0) },
        { Orthogonal.LeftDown, new Basis(0, 0, -1, 0, -1, 0, -1, 0, 0) },
        { Orthogonal.DownLeft, new Basis(0, 1, 0, 0, 0, -1, -1, 0, 0) },
        { Orthogonal.RightDown, new Basis(0, 0, 1, 0, -1, 0, 1, 0, 0) },
        { Orthogonal.UpRight, new Basis(0, 1, 0, 0, 0, 1, 1, 0, 0) },
        { Orthogonal.LeftUp, new Basis(0, 0, -1, 0, 1, 0, 1, 0, 0) },
        { Orthogonal.DownRight, new Basis(0, -1, 0, 0, 0, -1, 1, 0, 0) }
    };

    public static Basis GetBasis(this Orthogonal orthogonal)
    {
        return Bases[orthogonal];
    }

    public static Orthogonal GetOrthogonal(this Basis basis)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Vector3 bv = basis[i];
                float v = bv[j];
                bv[j] = v > 0.5f ? 1.0f : (v < -0.5f ? -1.0f : 0);
                basis[i] = bv;
            }
        }
        foreach ((Orthogonal key, var value) in Bases)
        {
            if (value.IsEqualApprox(basis))
            {
                return key;
            }
        }
        return Orthogonal.Invalid;
    }
}