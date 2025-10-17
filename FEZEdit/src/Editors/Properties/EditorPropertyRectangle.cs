using Rectangle = FEZRepacker.Core.Definitions.Game.XNA.Rectangle;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyRectangle : EditorPropertyVector<Rectangle>
{
    private static readonly string[] NodePaths = ["%SpinBoxX", "%SpinBoxY", "%SpinBoxW", "%SpinBoxH"];

    protected override string[] Components => NodePaths;
    
    protected override Rectangle GetValueInternal()
    {
        return new Rectangle(
            x: (int)_spinBoxes[0].Value,
            y: (int)_spinBoxes[1].Value,
            width: (int)_spinBoxes[2].Value,
            height: (int)_spinBoxes[3].Value
        );
    }

    protected override void SetValueInternal(Rectangle value)
    {
        _spinBoxes[0].Value = value.X;
        _spinBoxes[1].Value = value.Y;
        _spinBoxes[2].Value = value.Width;
        _spinBoxes[3].Value = value.Height;
    }

    public override void _Ready()
    {
        base._Ready();
        foreach (var spinBox in _spinBoxes)
        {
            spinBox.MinValue = int.MinValue;
            spinBox.MaxValue = int.MaxValue;
        }
    }
}