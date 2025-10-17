using Vector3 = FEZRepacker.Core.Definitions.Game.XNA.Vector3;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyVector3 : EditorPropertyVector<Vector3>
{
    private static readonly string[] NodePaths = ["%SpinBoxX", "%SpinBoxY", "%SpinBoxZ"];

    protected override string[] Components => NodePaths;
    
    protected override Vector3 GetValueInternal()
    {
        return new Vector3(
            x: (float)_spinBoxes[0].Value,
            y: (float)_spinBoxes[1].Value,
            z: (float)_spinBoxes[2].Value
        );
    }

    protected override void SetValueInternal(Vector3 value)
    {
        _spinBoxes[0].Value = value.X;
        _spinBoxes[1].Value = value.Y;
        _spinBoxes[2].Value = value.Z;
    }
}