using System.Collections.Generic;
using FEZEdit.Loaders;
using Godot;

namespace FEZEdit.Materializers;

public abstract class Materializer
{
    public IDictionary<Node, object> GameTypeRelations { get; } = new Dictionary<Node, object>();

    public ILoader AssetLoader { get; set; }
    
    public abstract Node Materialize(object input);
}

public abstract class Materializer<TInput, TOutput> : Materializer
{
    protected abstract TOutput Materialize(TInput input);

    public override Node Materialize(object input) => Materialize((TInput)input) as Node;
}
