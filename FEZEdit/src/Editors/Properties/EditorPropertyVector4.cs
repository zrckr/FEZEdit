using Vector4 = FEZRepacker.Core.Definitions.Game.XNA.Vector4;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyVector4 : EditorPropertyVector<Vector4>
{
    private static readonly string[] NodePaths = ["%SpinBoxX", "%SpinBoxY", "%SpinBoxZ", "%SpinBoxW"];

    protected override string[] Components => NodePaths;
    
    protected override Vector4 GetValueInternal()
    {
        return new Vector4(
            x: (float)_spinBoxes[0].Value,
            y: (float)_spinBoxes[1].Value,
            z: (float)_spinBoxes[2].Value,
            w: (float)_spinBoxes[3].Value
        );
    }

    protected override void SetValueInternal(Vector4 value)
    {
        _spinBoxes[0].Value = value.X;
        _spinBoxes[1].Value = value.Y;
        _spinBoxes[2].Value = value.Z;
        _spinBoxes[3].Value = value.W;
    }
}