using System.Collections.Generic;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Memento;
using Godot;

namespace FEZEdit.Editors.Eddy.Materializers;

using FEZRepacker.Core.Definitions.Game.Level;
using FEZRepacker.Core.Definitions.Game.TrileSet;

public partial class TrileMaterializer : Node, IMaterializer
{
    public static readonly Vector3 EmplacementCenter = Vector3.One / 2f;
    
    private static readonly Basis[] BasisLookup =
    [
        Basis.Identity.Rotated(Vector3.Up, -Mathf.Tau / 2f),
        Basis.Identity.Rotated(Vector3.Up, -Mathf.Tau / 4f),
        Basis.Identity.Rotated(Vector3.Up, 0),
        Basis.Identity.Rotated(Vector3.Up, +Mathf.Tau / 4f)
    ];
    
    private const float CollisionAlpha = 0.5f;

    private const float CollisionOversize = 1.001f;
    
    private readonly Dictionary<int, TrileMesh> _meshes = new();
    
    private readonly Dictionary<int, MultiMeshInstance3D> _visualInstances = new();
    
    private readonly Dictionary<int, MultiMeshInstance3D> _collisionInstances = new();

    private readonly Dictionary<TrileEmplacement, MaterializerProxy> _proxies = new();
    
    private Dictionary<TrileEmplacement, TrileInstance> _triles;

    private Dictionary<int, TrileGroup> _trileGroups;
    
    private TrileSet _trileSet;

    private int _updateQueued;

    public MementoManager Memento { private get; set; }

    public static IMaterializer Create(
        TrileSet trileSet,
        Dictionary<TrileEmplacement, TrileInstance> triles,
        Dictionary<int, TrileGroup> groups)
    {
        var materializer = new TrileMaterializer();
        materializer._trileSet = trileSet;
        materializer._triles = triles;
        materializer._trileGroups = groups;
        return materializer;
    }
    
    public override void _Ready()
    {
        Name = nameof(TrileMaterializer);
        InitializeMeshes();
        ClearInstances();
        UpdateInstances();
    }
    
    public GeometryInstance3D GetCursorInstance(string path)
    {
        var trileId = _trileSet.Triles
            .FirstOrDefault(pair => pair.Value.Name.Equals(path))
            .Key;

        if (!_meshes.TryGetValue(trileId, out var mesh))
        {
            // TODO: add missing mesh
        }

        var instance = new MeshInstance3D { Mesh = mesh?.Visual ?? mesh?.Collision };
        return instance;
    }

    public void CreateInstance(string path, Transform3D emplacement)
    {
        var trileId = _trileSet.Triles
            .FirstOrDefault(pair => pair.Value.Name.Equals(path))
            .Key;

        using (Memento.BeginScope("Place trile"))
        {
            var trileEmplacement = new TrileEmplacement
            {
                X = (int)emplacement.Origin.X, Y = (int)emplacement.Origin.Y, Z = (int)emplacement.Origin.Z
            };
            
            var trileInstance = new TrileInstance
            {
                TrileId = trileId,
                Position = emplacement.Origin.ToXna(),
                PhiLight = FindPhi(emplacement.Basis),
                ActorSettings = null
            };
            
            _triles[trileEmplacement] = trileInstance;
            UpdateInstances();
        }
    }

    public void RemoveInstance(Transform3D emplacement)
    {
        var trileEmplacement = new TrileEmplacement
        {
            X = (int)emplacement.Origin.X, Y = (int)emplacement.Origin.Y, Z = (int)emplacement.Origin.Z
        };
        
        if (_triles.ContainsKey(trileEmplacement))
        {
            using (Memento.BeginScope("Erase trile"))
            {
                _triles.Remove(trileEmplacement);
                UpdateInstances();
            }
        }
    }

    public string PickInstance(Transform3D emplacement)
    {
        var trileEmplacement = new TrileEmplacement
        {
            X = (int)emplacement.Origin.X, Y = (int)emplacement.Origin.Y, Z = (int)emplacement.Origin.Z
        };

        return _triles.TryGetValue(trileEmplacement, out TrileInstance trile) 
            ? _trileSet.Triles[trile.TrileId].Name 
            : string.Empty;
    }

    public void SetTrile(TrileEmplacement emplacement, TrileInstance instance)
    {
        if (instance == null)
        {
            _triles.Remove(emplacement);
        }
        else
        {
            _triles[emplacement] = instance;
        }

        _updateQueued += 1;
        if (_updateQueued == 1)
        {
            Callable.From(() =>
            {
                ClearInstances();
                UpdateInstances();
                _updateQueued = 0;
            });
        }
    }

    public void ShowCollision()
    {
        foreach (var instance in _collisionInstances.Values)
        {
            instance.Show();
        }
    }

    public void GroupTriles(HashSet<TrileEmplacement> emplacements)
    {
        var trileGroup = new TrileGroup();
        foreach (var emplacement in emplacements)
        {
            if (_triles.TryGetValue(emplacement, out var instance))
            {
                trileGroup.Triles.Add(instance);
            }
        }

        var lastKey = _trileGroups.Keys.Max();
        _trileGroups.Add(lastKey + 1, trileGroup);
    }

    public void UngroupTriles(HashSet<TrileEmplacement> emplacements)
    {
        var instances = emplacements.Select(e => _triles.GetValueOrDefault(e)).ToList();
        var ids = _trileGroups.Keys.ToList();
        foreach (var id in ids)
        {
            var group = _trileGroups[id];
            foreach (var instance in instances.Where(instance => group.Triles.Contains(instance)))
            {
                group.Triles.Remove(instance);
            }

            if (group.Triles.Count == 0)
            {
                _trileGroups.Remove(id);
            }
        }
    }

    private void InitializeMeshes()
    {
        var meshes = ContentConversion.ConvertToMesh(_trileSet);
        var lookup = _trileSet.Triles.ToDictionary(t => t.Value.Name, t => (t.Key, t.Value));
        
        _meshes.Clear();
        foreach ((string name, var mesh) in meshes)
        {
            (int id, var instance) = lookup[name];
            var size = instance.Size.ToGodot() * CollisionOversize;
            _meshes[id] = new TrileMesh
            {
                Visual = mesh,
                Collision = ContentConversion.CreateCollisionMesh(instance.Faces, size, CollisionAlpha)
            };
        }
    }

    private void UpdateInstances()
    {
        var multiMeshIds = new Dictionary<int, List<Transform3D>>();
        foreach (TrileInstance instance in _triles.Values)
        {
            if (!multiMeshIds.TryGetValue(instance.TrileId, out List<Transform3D> xforms))
            {
                xforms = [];
                multiMeshIds[instance.TrileId] = xforms;
            }
            
            var xform = Transform3D.Identity;
            xform.Basis = BasisLookup[instance.PhiLight];
            xform.Origin = EmplacementCenter + instance.Position.ToGodot();
            xforms.Add(xform);
        }

        foreach ((int trileId, var xforms) in multiMeshIds)
        {
            var visualInstance = CreateInstance(xforms, _meshes[trileId].Visual);
            if (IsInsideTree())
            {
                _visualInstances[trileId] = visualInstance;
                AddChild(visualInstance, true);
            }
            
            var collisionInstance = CreateInstance(xforms, _meshes[trileId].Collision);
            if (IsInsideTree())
            {
                _collisionInstances[trileId] = collisionInstance;
                AddChild(collisionInstance, true);
                collisionInstance.Hide();
            }
        }
        
        foreach ((TrileEmplacement emplacement, var instance) in _triles)
        {
            if (instance.ActorSettings == null)
            {
                continue;
            }
            
            var size = _trileSet.Triles[instance.TrileId].Size.ToGodot();
            var offset = _trileSet.Triles[instance.TrileId].Offset.ToGodot();
            
            var proxy = MaterializerProxy.CreateFromBox(instance.ActorSettings, size);
            AddChild(proxy, true);
            proxy.Offset = offset;
            proxy.GlobalPosition = EmplacementCenter + instance.Position.ToGodot();

            _proxies[emplacement] = proxy;
        }

        return;

        MultiMeshInstance3D CreateInstance(List<Transform3D> xforms, Mesh mesh)
        {
            var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, InstanceCount = xforms.Count, Mesh = mesh };
            
            for (int i = 0; i < xforms.Count; i++)
            {
                mm.SetInstanceTransform(i, xforms[i]);
            }

            return new MultiMeshInstance3D { Multimesh = mm };
        }
    }

    private void ClearInstances()
    {
        foreach (var instance in _visualInstances.Values)
        {
            instance.QueueFree();
        }
        
        foreach (var instance in _collisionInstances.Values)
        {
            instance.QueueFree();
        }
        
        foreach (var proxy in _proxies.Values)
        {
            proxy.QueueFree();
        }
        
        _visualInstances.Clear();
        _collisionInstances.Clear();
        _proxies.Clear();
    }
    
    public static Basis GetPhiBasis(byte phi)
    {
        return BasisLookup[phi];
    }

    public static byte FindPhi(Basis basis)
    {
        for (int i = 0; i < 3; i++)
        {
            var axis = basis[i];
            for (int j = 0; j < 3; j++)
            {
                axis[j] = axis[j] switch
                {
                    > 0.5f => 1.0f,
                    < -0.5f => -1.0f,
                    _ => 0.0f
                };
            }

            basis[i] = axis;
        }

        for (byte i = 0; i < BasisLookup.Length; i++)
        {
            if (basis.IsEqualApprox(BasisLookup[i]))
            {
                return i;
            }
        }

        return 0;
    }

    public static byte RotatePhi(byte phi)
    {
        return (byte)((phi + 1) % BasisLookup.Length);
    }

    private record TrileMesh
    {
        public Mesh Visual { get; init; }
        public Mesh Collision { get; init; }
    }
}