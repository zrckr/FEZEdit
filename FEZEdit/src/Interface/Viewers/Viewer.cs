using System;
using System.Collections.Generic;
using FEZEdit.Loaders;
using FEZEdit.Materializers;
using Godot;

namespace FEZEdit.Interface.Viewers;

public abstract partial class Viewer : MarginContainer
{
    public abstract event Action<object> ObjectSelected;
    
    public abstract Dictionary<Type, Type> Materializers { get; }
    
    protected Materializer Materializer { get; private set; }
    
    protected Node Instance { get; private set; }

    public override void _Ready()
    {
        var oldInstance = GetNodeOrNull<Node>("%Instance");
        if (oldInstance != null)
        {
            var parent = oldInstance.GetParent();
            oldInstance.QueueFree();
            parent.AddChild(Instance);
        }
    }
    
    public void Prepare(object @object, ILoader assetLoader)
    {
        if (!Materializers.TryGetValue(@object.GetType(), out var type))
        {
            throw new NotSupportedException(nameof(@object));
        }
        
        Materializer = (Materializer) Activator.CreateInstance(type)!;
        Materializer.AssetLoader = assetLoader;
        Instance = Materializer.Materialize(@object);
    }
}