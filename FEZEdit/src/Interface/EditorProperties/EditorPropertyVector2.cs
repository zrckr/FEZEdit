using Vector2 = FEZRepacker.Core.Definitions.Game.XNA.Vector2;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyVector2 : EditorPropertyVector<Vector2>
{
    private static readonly string[] NodePaths = ["%SpinBoxX", "%SpinBoxY"];

    protected override string[] Components => NodePaths;
    
    protected override Vector2 TypedValue
    {
        get => new(
            x: (float)_spinBoxes[0].Value,
            y: (float)_spinBoxes[1].Value
        );

        set
        {
            _spinBoxes[0].Value = value.X;
            _spinBoxes[1].Value = value.Y;
        }
    }
}