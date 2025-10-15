using Godot;

namespace FEZEdit.Interface.Gizmos;

[Tool]
public partial class AxisGizmo3D : Node3D
{
    [Export] public Color AxisXColor { get; set; } = new(0.96f, 0.20f, 0.32f);

    [Export] public Color AxisYColor { get; set; } = new(0.53f, 0.84f, 0.01f);

    [Export] public Color AxisZColor { get; set; } = new(0.16f, 0.55f, 0.96f);
    
    [Export] public float AxisLength = 0.7f;
    
    [Export] public float ArrowSize = 0.1f;
   
    [Export] public float LineThickness = 0.02f;
    
    [Export] public bool ShowLabels = true;

    public override void _Ready()
    {
        CreateGizmo();
    }

    private void CreateGizmo()
    {
        CreateAxis(Vector3.Right, AxisXColor, "X");
        CreateAxis(Vector3.Up, AxisYColor, "Y");
        CreateAxis(Vector3.Back, AxisZColor, "Z");
    }

    private void CreateAxis(Vector3 direction, Color color, string label)
    {
        var lineMesh = new CylinderMesh
        {
            TopRadius = LineThickness,
            BottomRadius = LineThickness,
            Height = AxisLength
        };

        var lineMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = color,
            NoDepthTest = true
        };

        var lineInstance = new MeshInstance3D
        {
            Mesh = lineMesh,
            MaterialOverride = lineMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        
        // Use a different up vector when direction is parallel to up axis
        Vector3 lookAtUp = (direction == Vector3.Up || direction == -Vector3.Up) 
            ? Vector3.Forward 
            : Vector3.Up;
        
        lineInstance.Position = direction * AxisLength * 0.5f;
        lineInstance.LookAtFromPosition(lineInstance.Position, direction * AxisLength, lookAtUp);
        lineInstance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
        AddChild(lineInstance);
        
        var arrowMesh = new CylinderMesh
        {
            TopRadius = 0,
            BottomRadius = ArrowSize,
            Height = ArrowSize * 2
        };

        var arrowInstance = new MeshInstance3D
        {
            Mesh = arrowMesh,
            MaterialOverride = lineMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        arrowInstance.Position = direction * AxisLength;
        arrowInstance.LookAtFromPosition(arrowInstance.Position, direction * (AxisLength + 1), lookAtUp);
        arrowInstance.RotateObjectLocal(Vector3.Right, -Mathf.Pi / 2);
        AddChild(arrowInstance);
        
        if (ShowLabels)
        {
            var labelNode = new Label3D
            {
                Text = label,
                Position = direction * (AxisLength + ArrowSize * 2),
                Modulate = color,
                FontSize = 32,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true,
                OutlineSize = 4,
                OutlineModulate = Colors.Black
            };
            AddChild(labelNode);
        }
    }
}