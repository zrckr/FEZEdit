using System.Collections.Generic;
using FEZEdit.Extensions;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using Godot;

namespace FEZEdit.Materializers;

public class ArtObjectMaterializer : Materializer<ArtObject, MeshInstance3D>
{
    protected override MeshInstance3D Materialize(ArtObject input)
    {
        var material = input.Cubemap.ToGodotMaterial();
        var mesh = input.Geometry.ToGodotMesh(material);
        var meshInstance = new MeshInstance3D { Name = input.Name, Mesh = mesh };

        var staticBody = new StaticBody3D();
        meshInstance.AddChild(staticBody);
        var collisionShape = new CollisionShape3D { Shape = mesh.CreateConvexShape() };
        staticBody.AddChild(collisionShape);
        GameTypeRelations.TryAdd(staticBody, input);

        return meshInstance;
    }
}