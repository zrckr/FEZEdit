using FEZRepacker.Core.Definitions.Game.Level;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyTrileEmplacement : EditorPropertyVector<TrileEmplacement>
{
    private static readonly string[] NodePaths = ["%SpinBoxX", "%SpinBoxY", "%SpinBoxZ"];

    protected override string[] Components => NodePaths;
    
    protected override TrileEmplacement GetValueInternal()
    {
        return new TrileEmplacement(
            x: (int)_spinBoxes[0].Value,
            y: (int)_spinBoxes[1].Value,
            z: (int)_spinBoxes[2].Value
        );
    }

    protected override void SetValueInternal(TrileEmplacement value)
    {
        _spinBoxes[0].Value = value.X;
        _spinBoxes[1].Value = value.Y;
        _spinBoxes[2].Value = value.Z;
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