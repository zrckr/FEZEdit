using Godot;

namespace FEZEdit.Materializers;

public partial class MaterializerProxy : StaticBody3D
{
    public object Object { get; set; }

    public static MaterializerProxy CreateFromMesh(object @object, Mesh mesh)
    {
        return CreateFromShape(@object, mesh.CreateTrimeshShape());
    }

    public static MaterializerProxy CreateFromBox(object @object, Vector3 size)
    {
        return CreateFromShape(@object, new BoxShape3D { Size = size });
    }

    public static MaterializerProxy CreateFromShape(object @object, Shape3D shape)
    {
        var proxy = new MaterializerProxy { Object = @object };
        var collisionShape = new CollisionShape3D { Shape = shape };
        proxy.AddChild(collisionShape);
        return proxy;
    }

    public static MaterializerProxy CreateEmpty(object @object)
    {
        return new MaterializerProxy { Object = @object };
    }
}