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
}