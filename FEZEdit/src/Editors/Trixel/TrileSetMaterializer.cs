using FEZEdit.Core;
using FEZEdit.Singletons;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;

namespace FEZEdit.Editors.Trixel;

public partial class TrileSetMaterializer : Node3D
{
    private const float NewLineStep = 15;

    private const float StepOffsetX = 2f;

    private const float StepOffsetZ = -2f;

    public void Initialize(TrileSet trileSet)
    {
        Name = trileSet.Name;
        var meshes = ContentConversion.ConvertToMesh(trileSet);
        var translation = Vector3.Zero;
        var steps = 0;
        
        foreach (var trile in trileSet.Triles.Values)
        {
            var mesh = meshes[trile.Name];
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