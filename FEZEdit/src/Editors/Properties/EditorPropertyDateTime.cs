using System;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyDateTime : EditorPropertyVector<DateTime>
{
    private static readonly string[] NodePaths =
        ["%SpinBoxYY", "%SpinBoxMM", "%SpinBoxDD", "%SpinBoxH", "%SpinBoxM", "%SpinBoxS", "%SpinBoxMS"];

    protected override string[] Components => NodePaths;

    protected override DateTime GetValueInternal()
    {
        var year = (int)_spinBoxes[0].Value;
        var month = (int)_spinBoxes[1].Value;
        var day = (int)_spinBoxes[2].Value;
        var hour = (int)_spinBoxes[3].Value;
        var minute = (int)_spinBoxes[4].Value;
        var second = (int)_spinBoxes[5].Value;
        var millisecond = (int)_spinBoxes[6].Value;
        return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
    }

    protected override void SetValueInternal(DateTime value)
    {
        _spinBoxes[0].Value = value.Year;
        _spinBoxes[1].Value = value.Month;

        _spinBoxes[2].MaxValue = DateTime.DaysInMonth(value.Year, value.Month);
        _spinBoxes[2].Value = Godot.Mathf.Clamp(value.Day, 1, _spinBoxes[2].MaxValue);

        _spinBoxes[3].Value = value.Hour;
        _spinBoxes[4].Value = value.Minute;
        _spinBoxes[5].Value = value.Second;
        _spinBoxes[6].Value = value.Millisecond;
    }
}