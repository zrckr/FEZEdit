using System;
using System.Collections.Generic;
using FEZEdit.Materializers;
using FEZEdit.Scene;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.Level;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;

namespace FEZEdit.Interface.Viewers;

public partial class GeometryViewer : Viewer
{
    private const float RayCastDistance = 1000.0f;

    public override event Action<object> ObjectSelected;

    public override Dictionary<Type, Type> Materializers => new()
    {
        { typeof(ArtObject), typeof(ArtObjectMaterializer) },
        { typeof(TrileSet), typeof(TrileSetMaterializer) },
        { typeof(Level), typeof(LevelMaterializer) }
    };

    private Node3D _viewerNode;

    private FreelookCamera3D _freelookCamera3D;

    public override void _Ready()
    {
        base._Ready();
        _viewerNode = GetNode<Node3D>("%ViewerNode");
        _freelookCamera3D = GetNode<FreelookCamera3D>("%FreelookCamera");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_freelookCamera3D.IsMoving ||
            @event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } button)
        {
            return;
        }

        var camera = GetViewport().GetCamera3D();
        var direction = camera.ProjectRayNormal(button.Position);
        var from = camera.ProjectRayOrigin(button.Position);
        var result = _viewerNode.GetWorld3D().DirectSpaceState.IntersectRay(new PhysicsRayQueryParameters3D
        {
            From = from, To = from + direction * RayCastDistance
        });

        if (result.Count == 0)
        {
            return;
        }

        var collider = (Node)result["collider"];
        var node = collider.GetParent<Node3D>();

        if (Materializer.GameTypeRelations.TryGetValue(node, out object value))
        {
            ObjectSelected?.Invoke(value);
        }
    }
}