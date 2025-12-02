using System.Collections.Generic;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Memento;
using Godot;

namespace FEZEdit.Editors.Eddy.Materializers;

using FEZRepacker.Core.Definitions.Game.Level;

public partial class ArtObjectMaterializer : Node, IMaterializer
{
    private readonly Dictionary<int, MeshInstance3D> _meshInstances = new();
    
    private readonly Dictionary<int, MaterializerProxy> _proxies = new();

    private readonly Dictionary<string, MeshInstance3D> _uniqueInstances = new();

    private Dictionary<int, ArtObjectInstance> _artObjects;

    public MementoManager Memento { private get; set; }

    public static IMaterializer Create(Dictionary<int, ArtObjectInstance> artObjects)
    {
        var materializer = new ArtObjectMaterializer();
        materializer._artObjects = artObjects;
        return materializer;
    }
    
    public override void _Ready()
    {
        Name = nameof(ArtObjectMaterializer);
        ClearInstances();
        UpdateInstances();
    }
    
    public GeometryInstance3D GetCursorInstance(string path)
    {
        if (!_uniqueInstances.TryGetValue(path, out var instance))
        {
            var ao = ContentLoader.LoadArtObject(path);
            var mesh = ContentConversion.ConvertToMesh(ao);
            instance = new MeshInstance3D { Mesh = mesh };
        }
        return instance;
    }

    public void CreateInstance(string path, Transform3D emplacement)
    {
        using (Memento.BeginScope("Create Art Object"))
        {
            var instance = new ArtObjectInstance
            {
                Name = path,
                Position = emplacement.Origin.ToXna(),
                Rotation = emplacement.Basis.GetRotationQuaternion().ToXna(),
                Scale = Vector3.One.ToXna()
            };

            var nextId = _artObjects.Keys.Max() + 1;
            _artObjects[nextId] = instance;
            UpdateInstances();
        }
    }

    public void RemoveInstance(Transform3D emplacement)
    {
        var position = emplacement.Origin.Floor();
        var id = _artObjects
            .FirstOrDefault(ao => ao.Value.Position.ToGodot().IsEqualApprox(position))
            .Key;

        using (Memento.BeginScope("Remove Art Object"))
        {
            _artObjects.Remove(id);
            UpdateInstances();
        }
    }

    public string PickInstance(Transform3D emplacement)
    {
        var position = emplacement.Origin.Floor();
        var id = _artObjects
            .FirstOrDefault(ao => ao.Value.Position.ToGodot().IsEqualApprox(position))
            .Key;

        return _artObjects[id].Name;
    }

    public void SetTransform(int id, Transform3D transform)
    {
        if (_artObjects.TryGetValue(id, out var artObject))
        {
            artObject.Position = transform.Origin.ToXna();
            artObject.Rotation = transform.Basis.GetRotationQuaternion().ToXna();
            artObject.Scale = transform.Basis.Scale.ToXna();
        }
        
        if (_meshInstances.TryGetValue(id, out var meshInstance))
        {
            meshInstance.Transform = transform;
        }
    }

    private void UpdateInstances()
    {
        var uniqueArtObjects = _artObjects
            .Select(kv => kv.Value.Name)
            .Distinct();

        var meshes = new Dictionary<string, Mesh>();
        foreach (var name in uniqueArtObjects)
        {
            var artObject = ContentLoader.LoadArtObject(name);
            var mesh = ContentConversion.ConvertToMesh(artObject);
            meshes.Add(name, mesh);
        }
        
        _uniqueInstances.Clear();
        foreach ((int key, var instance) in _artObjects)
        {
            var meshInstance = new MeshInstance3D { Mesh = meshes[instance.Name] };
            _meshInstances.Add(key, meshInstance);
            _uniqueInstances.TryAdd(instance.Name, meshInstance.Duplicate() as MeshInstance3D);
            AddChild(meshInstance, true);
            
            meshInstance.Position = instance.Position.ToGodot();
            meshInstance.Quaternion = instance.Rotation.ToGodot();
            meshInstance.Scale = instance.Scale.ToGodot();

            var proxy = MaterializerProxy.CreateFromMesh(instance, meshes[instance.Name]);
            _proxies.Add(key, proxy);
            meshInstance.AddChild(proxy, true);
        }
    }
    
    private void ClearInstances()
    {
        foreach (var proxy in _proxies.Values)
        {
            proxy.QueueFree();
        }
        
        foreach (var instance in _meshInstances.Values)
        {
            instance.QueueFree();
        }
        
        _meshInstances.Clear();
        _proxies.Clear();
    }
}