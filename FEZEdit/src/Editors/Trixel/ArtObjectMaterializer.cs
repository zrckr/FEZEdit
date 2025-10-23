using FEZEdit.Core;
using FEZEdit.Extensions;
using FEZEdit.Singletons;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using Godot;

namespace FEZEdit.Editors.Trixel;

public partial class ArtObjectMaterializer : Node3D
{
    public void Initialize(ArtObject artObject)
    {
        var mesh = ContentConversion.ConvertToMesh(artObject);
        var meshInstance = new MeshInstance3D { Name = artObject.Name, Mesh = mesh };
        meshInstance.AddChild(MaterializerProxy.CreateFromBox(artObject, artObject.Size.ToGodot()));
        AddChild(meshInstance);
    }
}