using System;
using Godot;

namespace FEZEdit.Editors.Eddy;

public partial class TimeNode : Node
{
    public event Action Tick;
    
    public TimeSpan CurrentTime { get; set; }
    
    [Export(PropertyHint.Range, "0,1000,1")] private float _timeFactor;

    [Export(PropertyHint.Range, "0,23,1")] private int _initialHour;

    public override void _Ready()
    {
        CurrentTime = TimeSpan.FromHours(_initialHour); 
    }

    public override void _Process(double delta)
    {
        var previousTime = CurrentTime;
        CurrentTime += TimeSpan.FromSeconds(delta * _timeFactor);

        if (Mathf.Abs(CurrentTime.Minutes - previousTime.Minutes) >= 1)
        {
            Tick?.Invoke();
        }
    }

    public void SetRunning(bool running)
    {
        SetProcess(running);
    }
}