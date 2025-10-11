using System.Collections.Generic;
using FEZEdit.Extensions;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;

namespace FEZEdit.Materializers;

public partial class TrileSetMaterializer : Materializer<TrileSet>
{
    private const float NewLineStep = 15;

    private const float StepOffsetX = 2f;

    private const float StepOffsetZ = -2f;

    public override void CreateNodesFrom(TrileSet trileSet)
    {
        Name = trileSet.Name;
        var material = trileSet.TextureAtlas.ToGodotMaterial();
        var translation = Vector3.Zero;
        var steps = 0;
        
        foreach (var trile in trileSet.Triles.Values)
        {
            var mesh = trile.Geometry.ToGodotMesh(material);
            var meshInstance = new MeshInstance3D { Name = trile.Name, Mesh = mesh };
            meshInstance.AddChild(MaterializerProxy.CreateFromBox(trile, trile.Size.ToGodot()));
            AddChild(meshInstance);
            
            steps++;
            translation.X += StepOffsetX;
            if (steps % NewLineStep == 0)
            {
                translation.X = 0;
                translation.Z += StepOffsetZ;
            }
            meshInstance.Translate(translation);
        }
    }
}