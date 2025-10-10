using System.Collections.Generic;
using FEZEdit.Loaders;
using Godot;

namespace FEZEdit.Materializers;

public abstract partial class Materializer<T> : Node3D
{
    public ILoader Loader { get; set; }

    public List<MaterializerProxy> Proxies { get; } = [];
    
    public abstract void CreateNodesFrom(T t);

    public Materializer()
    {
        Name = typeof(T).Name + "Materializer";
    }
}