using Godot;

namespace FEZEdit.Interface.Gizmos;

public partial class AxisGizmoOverlay : SubViewportContainer
{
    const float CameraDistance = 2.0f;
    
    [Export] private Camera3D _trackedCamera;
    
    private AxisGizmo3D _gizmo;
    
    private Camera3D _gizmoCamera;

    public override void _Ready()
    {
        _gizmo = GetNode<AxisGizmo3D>("%AxisGizmo3D");
        _gizmoCamera = GetNode<Camera3D>("%GizmoCamera");
        _trackedCamera ??= GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        _gizmoCamera.GlobalRotation = _trackedCamera.GlobalRotation;
        _gizmoCamera.Position = -_gizmoCamera.GlobalTransform.Basis.Z * CameraDistance;
        _gizmoCamera.LookAt(_gizmo.GlobalPosition);
    }
}