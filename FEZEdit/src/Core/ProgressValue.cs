using Godot;

namespace FEZEdit.Core;

public class ProgressValue(int value, int minimum, int maximum, int step)
{
    public static readonly ProgressValue Single = new(0, 0, 1, 1);
    
    public static readonly ProgressValue Complete = new(1, 0, 1, 1);
    
    public int Minimum { get; } = minimum;

    public int Maximum { get;  } = maximum;

    public int Value { get; set; } = value;

    public int Step { get; } = step;

    public bool Completed => Value >= Maximum;

    public void Next() => Value = Mathf.Clamp(Value + Step, Minimum, Maximum);
}