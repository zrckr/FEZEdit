using Quaternion = FEZRepacker.Core.Definitions.Game.XNA.Quaternion;

namespace FEZEdit.Interface.EditorProperties;

public partial class EditorPropertyQuaternion : EditorPropertyVector<Quaternion>
{
    private static readonly string[] NodePaths = ["%SpinBoxX", "%SpinBoxY", "%SpinBoxZ", "%SpinBoxW"];

    protected override string[] Components => NodePaths;
    
    protected override Quaternion TypedValue
    {
        get => new(
            x: (float)_spinBoxes[0].Value,
            y: (float)_spinBoxes[1].Value,
            z: (float)_spinBoxes[2].Value,
            w: (float)_spinBoxes[3].Value
        );

        set
        {
            _spinBoxes[0].Value = value.X;
            _spinBoxes[1].Value = value.Y;
            _spinBoxes[2].Value = value.Z;
            _spinBoxes[3].Value = value.W;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        Unit = true;
    }
}