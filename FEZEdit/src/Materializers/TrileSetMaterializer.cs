using System.Collections.Generic;
using FEZEdit.Extensions;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;

namespace FEZEdit.Materializers;

public class TrileSetMaterializer : Materializer<TrileSet, Node3D>
{
    private const float NewLineStep = 15;

    private const float StepOffsetX = 2f;

    private const float StepOffsetZ = -2f;

    protected override Node3D Materialize(TrileSet input)
    {
        var material = input.TextureAtlas.ToGodotMaterial();

        var node = new Node3D { Name = input.Name };
        var translation = Vector3.Zero;
        var steps = 0;
        
        foreach (var trile in input.Triles.Values)
        {
            var mesh = trile.Geometry.ToGodotMesh(material);
            var meshInstance = new MeshInstance3D { Name = trile.Name, Mesh = mesh };
            node.AddChild(meshInstance);
            
            var staticBody = new StaticBody3D();
            meshInstance.AddChild(staticBody);
            var collisionShape = new CollisionShape3D { Shape = mesh.CreateConvexShape() };
            staticBody.AddChild(collisionShape);
            
            steps++;
            translation.X += StepOffsetX;
            if (steps % NewLineStep == 0)
            {
                translation.X = 0;
                translation.Z += StepOffsetZ;
            }
            meshInstance.Translate(translation);
            
            GameTypeRelations.TryAdd(staticBody, trile);
        }

        return node;
    }
}