using FEZEdit.Core;
using FEZEdit.Extensions;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using Godot;

namespace FEZEdit.Editors.Trixel;

public partial class ArtObjectMaterializer : Node3D
{
    public void Initialize(ArtObject artObject)
    {
        var material = artObject.Cubemap.ToGodotMaterial();
        var mesh = artObject.Geometry.ToGodotMesh(material);
        var meshInstance = new MeshInstance3D { Name = artObject.Name, Mesh = mesh };
        meshInstance.AddChild(MaterializerProxy.CreateFromBox(artObject, artObject.Size.ToGodot()));
        AddChild(meshInstance);
    }
}