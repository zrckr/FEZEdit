using System;
using System.Collections.Generic;

namespace FEZEdit.Core;

public enum SizeUnits
{
    Bytes, KiB, MiB, GiB
}

public static class SizeUnitsExtensions
{
    private static string ToSize(this long value, SizeUnits unit)
    {
        var size = (value / Math.Pow(1024, (long)unit)).ToString("0.00");
        return size + " " + unit;
    }

    public static string ToSize(this long value)
    {
        return value switch
        {
            < 1024 => value.ToSize(SizeUnits.Bytes),
            < 1024 * 1024 => value.ToSize(SizeUnits.KiB),
            < 1024 * 1024 * 1024 => value.ToSize(SizeUnits.MiB),
            _ => value.ToSize(SizeUnits.GiB)
        };
    }
}