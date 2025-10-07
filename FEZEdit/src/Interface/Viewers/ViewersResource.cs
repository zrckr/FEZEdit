using System.Linq;
using Godot;
using Godot.Collections;

namespace FEZEdit.Interface.Viewers;

public partial class ViewersResource: Resource
{
    [Export] private PackedScene _emptyScene;

    [Export] private PackedScene _unsupportedScene;

    [Export] private Array<PackedScene> _scenes;
    
    public Viewer EmptyViewer => _emptyScene!.Instantiate<Viewer>();
    
    public Viewer UnsupportedViewer => _unsupportedScene!.Instantiate<Viewer>();

    public bool TryGetViewer(object @object, out Viewer viewer)
    {
        var type = @object.GetType();
        foreach (var scene in _scenes)
        {
            viewer = scene.Instantiate<Viewer>();
            if (viewer.Materializers.ContainsKey(type))
            {
                return true;
            }
        }
        
        viewer = EmptyViewer;
        return false;
    }
}