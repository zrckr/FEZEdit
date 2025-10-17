using Godot;

namespace FEZEdit.Core;

public abstract partial class Materializer<T> : Node3D
{
    public abstract void CreateNodesFrom(T t);

    public Materializer()
    {
        Name = typeof(T).Name + "Materializer";
    }
}