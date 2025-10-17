using System;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyTimeSpan : EditorPropertyVector<TimeSpan>
{
    private static readonly string[] NodePaths = ["%SpinBoxH", "%SpinBoxM", "%SpinBoxS", "%SpinBoxMS"];

    protected override string[] Components => NodePaths;
    
    protected override TimeSpan GetValueInternal()
    {
        var hours = (int)_spinBoxes[0].Value;
        var minutes = (int)_spinBoxes[1].Value;
        var seconds = (int)_spinBoxes[2].Value;
        var milliseconds = (int)_spinBoxes[3].Value;
        var total = (hours * 3600000) + (minutes * 60000) + (seconds * 1000) + milliseconds;
        return TimeSpan.FromMilliseconds(total);
    }

    protected override void SetValueInternal(TimeSpan value)
    {
        _spinBoxes[0].Value = value.Hours;
        _spinBoxes[1].Value = value.Minutes;
        _spinBoxes[2].Value = value.Seconds;
        _spinBoxes[3].Value = value.Milliseconds;
    }
}