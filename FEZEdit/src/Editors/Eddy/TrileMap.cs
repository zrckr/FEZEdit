using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using Godot;

namespace FEZEdit.Editors.Eddy;

using FEZRepacker.Core.Definitions.Game.Level;
using FEZRepacker.Core.Definitions.Game.TrileSet;

public partial class TrileMap : Node3D
{
    private const int ChunkSize = 16;

    private const float CollisionAlpha = 0.5f;

    private const float CollisionOversize = 1.001f;

    private readonly Dictionary<TrileEmplacement, TrileInstance> _triles = new();

    private readonly Dictionary<Vector3I, Chunk> _chunks = new();

    private readonly Dictionary<TrileEmplacement, CullingMode> _culling = new();

    private readonly HashSet<TrileEmplacement> _collisionMap = [];

    private readonly Dictionary<int, TrileMesh> _meshes = new();

    private readonly Node _instances = new();

    private readonly Node _collisions = new();

    private readonly Node _proxies = new();

    private CullingMode _cullingMode;

    private TrileSet _trileSet;

    private bool _showCollisionMap;

    private int _updateQueued;

    #region Public

    public CullingMode Culling
    {
        get => _cullingMode;
        set
        {
            _cullingMode = value;
            foreach (var chunk in _chunks.Values)
            {
                chunk.Dirty = true;
            }

            Callable.From(ChunksUpdate).CallDeferred();
        }
    }

    public TrileSet TrileSet
    {
        get => _trileSet;
        set
        {
            _trileSet = value;
            Callable.From(CreateTrileMeshes).CallDeferred();
            Callable.From(ChunksRecreate).CallDeferred();
        }
    }

    public bool ShowCollisionMap
    {
        get => _showCollisionMap;
        set
        {
            _showCollisionMap = value;
            Callable.From(CollisionMapUpdate).CallDeferred();
        }
    }

    public Aabb Bounds { get; set; }

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationEnterWorld:
                {
                    ChunksEnterWorld();
                    break;
                }

            case NotificationEnterTree:
                {
                    AddChild(_instances, true);
                    AddChild(_collisions, true);
                    AddChild(_proxies, true);
                    ChunksUpdateVisibility();
                    break;
                }

            case NotificationReady:
            case NotificationVisibilityChanged:
                {
                    ChunksUpdateVisibility();
                    break;
                }

            case NotificationExitWorld:
                {
                    ChunksExitWorld();
                    RemoveChild(_proxies);
                    RemoveChild(_collisions);
                    RemoveChild(_instances);
                    break;
                }
        }
    }

    #endregion

    #region Emplacements

    public void SetTrile(TrileEmplacement emplacement, TrileInstance instance)
    {
        var chunkKey = new Vector3I(emplacement.X, emplacement.Y, emplacement.Z) / ChunkSize;
        Chunk chunk;

        if (instance == null)
        {
            if (_chunks.TryGetValue(chunkKey, out chunk))
            {
                chunk.Emplacements.Remove(emplacement);
                chunk.Dirty = true;
            }

            _triles.Remove(emplacement);
            return;
        }

        if (!_chunks.TryGetValue(chunkKey, out chunk))
        {
            chunk = new Chunk();
            _chunks[chunkKey] = chunk;
        }

        chunk.Emplacements.Add(emplacement);
        chunk.Dirty = true;

        _triles[emplacement] = instance;
        _updateQueued += 1;

        if (_updateQueued == 1)
        {
            Callable.From(() =>
            {
                CullingUpdate();
                ChunksUpdate();
                _updateQueued = 0;
            }).CallDeferred();
        }
    }

    #endregion

    #region Chunks

    private bool ChunkUpdate(Vector3I chunkKey)
    {
        if (!_chunks.TryGetValue(chunkKey, out Chunk chunk) || !chunk.Dirty)
        {
            return false;
        }

        // Clear existing data in the chunk
        foreach (var instance in chunk.Instances)
        {
            instance.QueueFree();
        }

        chunk.Instances.Clear();

        foreach (var proxy in chunk.Proxies)
        {
            proxy.QueueFree();
        }

        chunk.Proxies.Clear();

        if (chunk.Emplacements.Count == 0)
        {
            // Chunk no longer needed
            return true;
        }

        // Apply culling data on the chunk
        var culledChunks = new HashSet<TrileEmplacement>();
        if (Culling == CullingMode.None)
        {
            culledChunks = chunk.Emplacements;
        }
        else
        {
            foreach (var emplacement in chunk.Emplacements)
            {
                if (_culling.TryGetValue(emplacement, out CullingMode culling) && (culling & Culling) != 0)
                {
                    culledChunks.Add(emplacement);
                }
            }
        }

        // Group triles by id for multimesh instances
        var multiMeshIds = new Dictionary<int, List<Transform3D>>();
        foreach (TrileInstance trile in culledChunks.Select(emplacement => _triles[emplacement]))
        {
            if (!multiMeshIds.TryGetValue(trile.TrileId, out List<Transform3D> xforms))
            {
                xforms = [];
                multiMeshIds[trile.TrileId] = xforms;
            }

            var xform = Transform3D.Identity;
            xform.Basis = BasisLookup[trile.PhiLight];
            xform.Origin = trile.Position.ToGodot();
            xforms.Add(xform);
        }

        // Create multimesh instances
        foreach ((int trileId, var xforms) in multiMeshIds)
        {
            var mm = new MultiMesh();
            mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
            mm.InstanceCount = xforms.Count;
            mm.Mesh = _meshes[trileId].Visual;
            for (int i = 0; i < xforms.Count; i++)
            {
                mm.SetInstanceTransform(i, xforms[i]);
            }

            var mmi = new MultiMeshInstance3D();
            mmi.Name = $"{trileId}";
            mmi.Multimesh = mm;

            if (IsInsideTree())
            {
                _instances.AddChild(mmi, true);
                mmi.GlobalTransform = GlobalTransform;
            }

            chunk.Instances.Add(mmi);
        }

        // Compute bounds
        foreach (var trileId in multiMeshIds.Keys)
        {
            var size = TrileSet.Triles[trileId].Size.ToGodot();
            var offset = TrileSet.Triles[trileId].Offset.ToGodot();
            var aabb = new Aabb(offset, size);

            foreach (var xform in multiMeshIds[trileId])
            {
                Bounds.Merge(aabb * xform);
            }
        }

        // Place proxies
        foreach (var emplacement in culledChunks)
        {
            var instance = _triles[emplacement];
            if (instance.ActorSettings == null)
            {
                continue;
            }

            var size = TrileSet.Triles[instance.TrileId].Size.ToGodot();
            var offset = TrileSet.Triles[instance.TrileId].Offset.ToGodot();

            var proxy = MaterializerProxy.CreateFromBox(instance.ActorSettings, size);
            _proxies.AddChild(proxy, true);
            proxy.Offset = offset;
            proxy.GlobalPosition = instance.Position.ToGodot();

            chunk.Proxies.Add(proxy);
        }

        chunk.Dirty = false;
        return false;
    }

    private void ChunkCleanUp(Vector3I chunkKey)
    {
        if (_chunks.TryGetValue(chunkKey, out Chunk chunk))
        {
            foreach (var instance in chunk.Instances)
            {
                instance.QueueFree();
            }

            chunk.Instances.Clear();
        }
    }

    private void ChunksEnterWorld()
    {
        foreach (var instance in _chunks.Values.SelectMany(chunk => chunk.Instances))
        {
            _instances.AddChild(instance, true);
            instance.GlobalTransform = GlobalTransform;
        }
    }

    private void ChunksUpdate()
    {
        var toDelete = new HashSet<Vector3I>();
        foreach (var chunkKey in _chunks.Keys.Where(ChunkUpdate))
        {
            toDelete.Add(chunkKey);
        }

        foreach (var chunkKey in toDelete)
        {
            ChunkCleanUp(chunkKey);
            _chunks.Remove(chunkKey);
        }

        ChunksUpdateVisibility();
    }

    private void ChunksUpdateVisibility()
    {
        foreach (var instance in _chunks.Values.SelectMany(chunk => chunk.Instances))
        {
            instance.Visible = IsVisibleInTree();
        }
    }

    private void ChunksExitWorld()
    {
        foreach (var instance in _chunks.Values.SelectMany(chunk => chunk.Instances))
        {
            _instances.RemoveChild(instance);
        }
    }

    private void ChunksRecreate()
    {
        var triles = new Dictionary<TrileEmplacement, TrileInstance>(_triles);
        if (IsInsideTree())
        {
            ChunksExitWorld();
        }

        foreach (var chunkKey in _chunks.Keys)
        {
            ChunkCleanUp(chunkKey);
        }

        _chunks.Clear();
        _triles.Clear();

        foreach ((TrileEmplacement emplacement, var instance) in triles)
        {
            SetTrile(emplacement, instance);
        }
    }

    #endregion

    #region Culling Map

    private void CullingUpdate()
    {
        // Invalidate existing culling data
        _culling.Clear();
        _collisionMap.Clear();

        // Compute culling bounds
        var start = new TrileEmplacement(int.MaxValue, int.MaxValue, int.MaxValue);
        var end = new TrileEmplacement(int.MinValue, int.MinValue, int.MinValue);
        foreach (var emplacement in _triles.Keys)
        {
            start = new TrileEmplacement
            {
                X = Mathf.Min(emplacement.X, start.X),
                Y = Mathf.Min(emplacement.Y, start.Y),
                Z = Mathf.Min(emplacement.Z, start.Z)
            };
            end = new TrileEmplacement
            {
                X = Mathf.Max(emplacement.X, end.X),
                Y = Mathf.Max(emplacement.Y, end.Y),
                Z = Mathf.Max(emplacement.Z, end.Z)
            };
        }

        // Process each orthogonal view direction
        foreach (var cullingMode in Enum.GetValues<CullingMode>())
        {
            if (cullingMode == CullingMode.None)
            {
                continue;
            }

            // Determine iteration ranges based on view direction
            var horizontalRange = GetRange(cullingMode.GetSide());
            var verticalRange = GetRange(Vector3I.Up);
            var depthRange = GetRange(cullingMode.GetDepth());

            // Iterate through the view plane
            for (int y = verticalRange.A; y <= verticalRange.B; y++)
            {
                for (int x = horizontalRange.A; x <= horizontalRange.B; x++)
                {
                    var emplacements = new List<TrileEmplacement>();
                    var seeThroughBits = new BitArray(depthRange.B - depthRange.A + 1);

                    // Collect triles along the depth axis for culling
                    int i = 0;
                    for (int z = depthRange.A; z <= depthRange.B; z++)
                    {
                        var vector = Vector3I.Up * y + cullingMode.GetSide() * x + cullingMode.GetDepth() * z;
                        var emplacement = new TrileEmplacement { X = vector.X, Y = vector.Y, Z = vector.Z };

                        if (!_triles.TryGetValue(emplacement, out TrileInstance trileInstance))
                        {
                            continue;
                        }

                        if (!TrileSet.Triles.TryGetValue(trileInstance.TrileId, out var trile))
                        {
                            continue;
                        }

                        seeThroughBits[i++] = trile.SeeThrough;
                        _culling.TryAdd(emplacement, CullingMode.None);
                        emplacements.Add(emplacement);
                    }

                    // No culling - skip the current depth iteration
                    if (emplacements.Count < 1)
                    {
                        continue;
                    }

                    var seeThrough = GetInteger(seeThroughBits);
                    var seeThroughAll = (ulong)(Mathf.Pow(2, emplacements.Count) - 1);

                    // If all emplacements are solid, then cull only the front one
                    if (seeThrough == 0)
                    {
                        _culling[emplacements[0]] |= cullingMode;
                        _collisionMap.Add(emplacements[0]);
                        continue;
                    }

                    // If all emplacements are see-through, then cull them all
                    if (seeThrough == seeThroughAll)
                    {
                        foreach (var emplacement in emplacements)
                        {
                            _culling[emplacement] |= cullingMode;
                        }

                        _collisionMap.Add(emplacements[0]);
                        continue;
                    }

                    // Cull emplacements until the solid one was hit
                    for (i = 0; i < emplacements.Count; i++)
                    {
                        _culling[emplacements[i]] |= cullingMode;
                        if (i == 0)
                        {
                            _collisionMap.Add(emplacements[i]);
                        }

                        var isSolid = (seeThrough & (1u << i)) == 0;
                        if (isSolid)
                        {
                            break;
                        }
                    }
                }
            }
        }

        return;

        (int A, int B) GetRange(Vector3I axis)
        {
            var startValue = start.X * axis.X + start.Y * axis.Y + start.Z * axis.Z;
            var endValue = end.X * axis.X + end.Y * axis.Y + end.Z * axis.Z;
            return endValue < startValue
                ? (endValue, startValue)
                : (startValue, endValue);
        }

        ulong GetInteger(BitArray bits)
        {
            var array = new ulong[1];
            bits.CopyTo(array, 0);
            return array[0];
        }
    }

    #endregion

    #region Collision Map

    private void CreateTrileMeshes()
    {
        _meshes.Clear();

        var meshes = ContentConversion.ConvertToMesh(TrileSet);
        var lookup = TrileSet.Triles.ToDictionary(t => t.Value.Name, t => (t.Key, t.Value));

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

    private void CollisionMapUpdate()
    {
        foreach (var mmi in _collisions.GetChildren())
        {
            mmi.QueueFree();
        }

        if (!IsInsideTree() || !ShowCollisionMap)
        {
            return;
        }

        // Group triles by id for multimesh instances
        var multiMeshIds = new Dictionary<int, List<Transform3D>>();
        foreach (TrileInstance instance in _collisionMap.Select(emplacement => _triles[emplacement]))
        {
            if (!multiMeshIds.TryGetValue(instance.TrileId, out var xforms))
            {
                xforms = [];
                multiMeshIds[instance.TrileId] = xforms;
            }

            var xform = Transform3D.Identity;
            xform.Basis = BasisLookup[instance.PhiLight];
            xform.Origin = instance.Position.ToGodot();
            xforms.Add(xform);
        }

        // Create multimesh instances
        foreach ((int trileId, var xforms) in multiMeshIds)
        {
            var mm = new MultiMesh();
            mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
            mm.InstanceCount = xforms.Count;
            mm.Mesh = _meshes[trileId].Collision;

            for (int i = 0; i < xforms.Count; i++)
            {
                mm.SetInstanceTransform(i, xforms[i]);
            }

            var mmi = new MultiMeshInstance3D();
            mmi.Name = $"{trileId}";
            mmi.Multimesh = mm;

            if (IsInsideTree())
            {
                _collisions.AddChild(mmi, true);
                mmi.GlobalTransform = GlobalTransform;
            }
        }
    }

    #endregion

    #region Phi lookup

    private static readonly Basis[] BasisLookup =
    [
        Basis.Identity.Rotated(Vector3.Up, -Mathf.Pi),
        Basis.Identity.Rotated(Vector3.Up, -Mathf.Pi / 2f),
        Basis.Identity.Rotated(Vector3.Up, 0),
        Basis.Identity.Rotated(Vector3.Up, +Mathf.Pi / 2f)
    ];

    private const int DefaultPhi = 0;

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

        return DefaultPhi;
    }

    public static byte RotatePhi(byte phi, Vector3 axis, float angle)
    {
        Debug.Assert(phi <= BasisLookup.Length);
        var rotatedBasis = BasisLookup[phi].Rotated(axis, angle);
        return FindPhi(rotatedBasis);
    }

    #endregion

    #region Internal types

    private record Chunk
    {
        public List<MultiMeshInstance3D> Instances { get; } = [];
        public List<MaterializerProxy> Proxies { get; } = [];
        public HashSet<TrileEmplacement> Emplacements { get; } = [];
        public bool Dirty { get; set; }
    }

    private record TrileMesh
    {
        public Mesh Visual { get; init; }
        public Mesh Collision { get; init; }
    }

    #endregion
}