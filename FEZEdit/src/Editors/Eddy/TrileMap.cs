using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Core;
using Godot;
using Vector3 = Godot.Vector3;
using static Godot.RenderingServer;

namespace FEZEdit.Editors.Eddy;

[Tool]
public partial class TrileMap : Node3D
{
    #region Constants

    private const int InvalidCellItem = -1;

    #endregion

    #region Properties

    [Export] public Vector3 CellSize { get; set; } = new(1, 1, 1);

    [Export] public int OctantSize { get; set; } = 8;

    [Export] public bool CenterX { get; set; } = true;

    [Export] public bool CenterY { get; set; } = true;

    [Export] public bool CenterZ { get; set; } = true;

    [Export] public float CellScale { get; set; } = 1.0f;

    [Export]
    public MeshLibrary MeshLibrary
    {
        get => _meshLibrary;
        set
        {
            if (_meshLibrary != null)
                _meshLibrary.Changed -= OnMeshLibraryChanged;

            _meshLibrary = value;

            if (_meshLibrary != null)
                _meshLibrary.Changed += OnMeshLibraryChanged;

            RecreateOctantData();
            EmitSignal(SignalName.Changed);
        }
    }

    [Export]
    private byte[] CellData
    {
        get
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(_cellMap.Count);
            foreach (var pair in _cellMap)
            {
                writer.Write(pair.Key.X);
                writer.Write(pair.Key.Y);
                writer.Write(pair.Key.Z);
                writer.Write(pair.Value.Item);
                writer.Write((byte)pair.Value.Rotation);
                writer.Write(pair.Value.Offset.X);
                writer.Write(pair.Value.Offset.Y);
                writer.Write(pair.Value.Offset.Z);
            }

            return stream.ToArray();
        }

        set
        {
            if (value == null || value.Length == 0)
                return;

            using var stream = new MemoryStream(value.ToArray());
            using var reader = new BinaryReader(stream);

            ClearInternal();
            int cellCount = reader.ReadInt32();

            for (int i = 0; i < cellCount; i++)
            {
                var position = new Vector3I(
                    reader.ReadInt16(),
                    reader.ReadInt16(),
                    reader.ReadInt16()
                );
                var item = reader.ReadUInt16();
                var rotation = (Orthogonal)reader.ReadByte();
                var offset = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );

                SetCellItemInternal(position, item, rotation, offset);
            }

            RecreateOctantData();
        }
    }

    [Signal]
    public delegate void CellSizeChangedEventHandler(Vector3 cellSize);

    [Signal]
    public delegate void ChangedEventHandler();

    #endregion

    private MeshLibrary _meshLibrary;

    private bool _recreatingOctants;

    private bool _awaitingUpdate;

    private readonly Dictionary<OctantKey, Octant> _octantMap = new();

    private readonly Dictionary<IndexKey, Cell> _cellMap = new();

    private Transform3D _lastTransform = Transform3D.Identity;

    private Aabb _cachedAabb;

    private bool _aabbDirty = true;

    public override void _Ready()
    {
        _lastTransform = GlobalTransform;
        UpdateVisibility();
    }

    public override void _EnterTree()
    {
        UpdateVisibility();
    }

    public override void _Process(double delta)
    {
        if (_awaitingUpdate)
        {
            UpdateOctantsCallback();
        }
    }

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationEnterWorld:
                _lastTransform = GlobalTransform;
                foreach (var pair in _octantMap)
                {
                    OctantEnterWorld(pair.Key);
                }

                break;

            case NotificationEnterTree:
                UpdateVisibility();
                break;

            case NotificationTransformChanged:
                Transform3D newTransform = GlobalTransform;
                if (newTransform == _lastTransform)
                    break;

                foreach (var pair in _octantMap)
                {
                    OctantTransform(pair.Key);
                }

                _lastTransform = newTransform;
                break;

            case NotificationExitWorld:
                foreach (var pair in _octantMap)
                {
                    OctantExitWorld(pair.Key);
                }

                break;

            case NotificationVisibilityChanged:
                UpdateVisibility();
                break;
        }
    }


    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        if (property["name"].AsStringName() == nameof(CellData))
        {
            property["usage"] = (int)(PropertyUsageFlags.Storage | PropertyUsageFlags.NoEditor);
        }
    }

    private void OnMeshLibraryChanged()
    {
        RecreateOctantData();
    }

    public void SetCellItem(Vector3I position, int item, Orthogonal orientation = Orthogonal.FrontUp,
        Vector3? offset = null)
    {
        if (Mathf.Abs(position.X) >= (1 << 20) || Mathf.Abs(position.Y) >= (1 << 20) ||
            Mathf.Abs(position.Z) >= (1 << 20))
        {
            GD.PushError("GridMap cell position out of bounds");
            return;
        }

        var key = new IndexKey(position);
        var hadOldCell = _cellMap.ContainsKey(key);

        if (item < 0)
        {
            if (!hadOldCell)
            {
                return;
            }

            RemoveCell(key);
            _aabbDirty = true;
            return;
        }

        var newCell = new Cell((ushort)item, orientation, offset ?? Vector3.Zero);
        if (hadOldCell && !_aabbDirty)
        {
            var oldCell = _cellMap[key];
            UpdateAabbForCellChange(key, oldCell, newCell);
        }
        else
        {
            _aabbDirty = true;
        }

        SetCellItemInternal(position, item, orientation, offset ?? Vector3.Zero);
        QueueOctantsDirty();
    }

    public int GetCellItem(Vector3I position)
    {
        if (Mathf.Abs(position.X) >= (1 << 20) || Mathf.Abs(position.Y) >= (1 << 20) ||
            Mathf.Abs(position.Z) >= (1 << 20))
            return InvalidCellItem;

        var key = new IndexKey(position);
        return _cellMap.TryGetValue(key, out Cell cell) ? cell.Item : InvalidCellItem;
    }

    public Orthogonal GetCellItemOrientation(Vector3I position)
    {
        if (Mathf.Abs(position.X) >= (1 << 20) || Mathf.Abs(position.Y) >= (1 << 20) ||
            Mathf.Abs(position.Z) >= (1 << 20))
            return Orthogonal.Invalid;

        var key = new IndexKey(position);
        return _cellMap.TryGetValue(key, out Cell cell) ? cell.Rotation : Orthogonal.Invalid;
    }

    public Basis GetCellItemBasis(Vector3I position)
    {
        var orientation = GetCellItemOrientation(position);
        return orientation == Orthogonal.Invalid ? new Basis() : orientation.GetBasis();
    }

    public Vector3I LocalToMap(Vector3 localPosition)
    {
        Vector3 mapPosition = (localPosition / CellSize).Floor();
        return new Vector3I((int)mapPosition.X, (int)mapPosition.Y, (int)mapPosition.Z);
    }

    public Vector3 MapToLocal(Vector3I mapPosition)
    {
        Vector3 offset = GetOffset();
        return new Vector3(
            mapPosition.X * CellSize.X + offset.X,
            mapPosition.Y * CellSize.Y + offset.Y,
            mapPosition.Z * CellSize.Z + offset.Z
        );
    }

    public Godot.Collections.Array<Vector3I> GetUsedCells()
    {
        var cells = new Godot.Collections.Array<Vector3I>();
        foreach (var pair in _cellMap)
        {
            cells.Add(pair.Key.ToVector3I());
        }

        return cells;
    }

    public Godot.Collections.Array<Vector3I> GetUsedCellsByItem(int item)
    {
        var cells = new Godot.Collections.Array<Vector3I>();
        foreach (var pair in _cellMap)
        {
            if (pair.Value.Item == item)
            {
                cells.Add(pair.Key.ToVector3I());
            }
        }

        return cells;
    }

    public Godot.Collections.Array GetMeshes()
    {
        if (MeshLibrary == null)
            return new Godot.Collections.Array();

        var meshes = new Godot.Collections.Array();
        var offset = GetOffset();

        foreach (var pair in _cellMap)
        {
            int id = pair.Value.Item;
            if (!MeshLibrary.GetItemList().Contains(id))
                continue;

            var mesh = MeshLibrary.GetItemMesh(id);
            if (mesh == null)
                continue;

            var key = pair.Key;
            var cellPos = new Vector3(key.X, key.Y, key.Z);

            var xform = new Transform3D
            {
                Basis = pair.Value.Rotation.GetBasis().Scaled(Vector3.One * CellScale),
                Origin = cellPos * CellSize + offset + pair.Value.Offset
            };

            meshes.Add(xform * MeshLibrary.GetItemMeshTransform(id));
            meshes.Add(mesh);
        }

        return meshes;
    }

    public Aabb GetAabb()
    {
        if (_aabbDirty)
        {
            RecalculateAabb();
        }

        return _cachedAabb;
    }

    public void ForceAabbRecalculation()
    {
        _aabbDirty = true;
    }

    public void Clear()
    {
        ClearInternal();
        _cachedAabb = new Aabb();
        _aabbDirty = true;
    }

    private void SetCellItemInternal(Vector3I position, int item, Orthogonal rotation, Vector3 offset)
    {
        if (item < 0)
            return;

        var key = new IndexKey(position);
        var octantKey = new OctantKey(
            (short)(position.X / OctantSize),
            (short)(position.Y / OctantSize),
            (short)(position.Z / OctantSize)
        );

        // Create octant if it doesn't exist
        if (!_octantMap.ContainsKey(octantKey))
        {
            _octantMap[octantKey] = new Octant { Dirty = true };
        }

        var octantRef = _octantMap[octantKey];
        octantRef.Cells.Add(key);
        octantRef.Dirty = true;

        _cellMap[key] = new Cell((ushort)item, rotation, offset);
    }

    private void RemoveCell(IndexKey key)
    {
        var octantKey = new OctantKey(
            (short)(key.X / OctantSize),
            (short)(key.Y / OctantSize),
            (short)(key.Z / OctantSize)
        );

        if (_octantMap.TryGetValue(octantKey, out Octant octant))
        {
            octant.Cells.Remove(key);
            octant.Dirty = true;
        }

        _cellMap.Remove(key);
    }

    private Vector3 GetOffset()
    {
        return new Vector3(
            CellSize.X * 0.5f * (CenterX ? 1 : 0),
            CellSize.Y * 0.5f * (CenterY ? 1 : 0),
            CellSize.Z * 0.5f * (CenterZ ? 1 : 0)
        );
    }

    private void QueueOctantsDirty()
    {
        if (_awaitingUpdate)
            return;

        _awaitingUpdate = true;
    }

    private void UpdateOctantsCallback()
    {
        if (!_awaitingUpdate)
            return;

        var toDelete = new List<OctantKey>();
        foreach (var pair in _octantMap)
        {
            if (OctantUpdate(pair.Key))
            {
                toDelete.Add(pair.Key);
            }
        }

        foreach (var key in toDelete)
        {
            OctantCleanUp(key);
            _octantMap.Remove(key);
        }

        UpdateVisibility();
        _awaitingUpdate = false;
    }

    private bool OctantUpdate(OctantKey key)
    {
        if (!_octantMap.TryGetValue(key, out Octant octant))
            return false;

        if (!octant.Dirty)
            return false;

        // Clear existing multimeshes
        foreach (var instance in octant.MultimeshInstances)
        {
            FreeRid(instance.Instance);
            FreeRid(instance.MultiMesh);
        }

        octant.MultimeshInstances.Clear();

        if (octant.Cells.Count == 0)
        {
            return true; // Octant no longer needed
        }

        // Group cells by item for multimesh instances
        var multimeshItems = new Dictionary<int, List<(Transform3D, IndexKey)>>();

        foreach (IndexKey cellKey in octant.Cells)
        {
            if (!_cellMap.TryGetValue(cellKey, out Cell cell))
                continue;

            if (MeshLibrary == null || !MeshLibrary.GetItemList().Contains(cell.Item))
                continue;

            var cellPos = new Vector3(cellKey.X, cellKey.Y, cellKey.Z);
            var offset = GetOffset();

            var xform = new Transform3D
            {
                Basis = cell.Rotation.GetBasis().Scaled(Vector3.One * CellScale),
                Origin = cellPos * CellSize + offset + cell.Offset
            };

            if (MeshLibrary.GetItemMesh(cell.Item) == null)
                continue;

            if (!multimeshItems.ContainsKey(cell.Item))
                multimeshItems[cell.Item] = [];

            multimeshItems[cell.Item].Add((xform * MeshLibrary.GetItemMeshTransform(cell.Item), cellKey));
        }

        // Create multimesh instances
        foreach (var pair in multimeshItems)
        {
            var mm = MultimeshCreate();
            MultimeshAllocateData(mm, pair.Value.Count, MultimeshTransformFormat.Transform3D);
            MultimeshSetMesh(mm, MeshLibrary!.GetItemMesh(pair.Key).GetRid());

            int idx = 0;
            foreach (var item in pair.Value)
            {
                MultimeshInstanceSetTransform(mm, idx, item.Item1);
                idx++;
            }

            var instance = InstanceCreate();
            InstanceSetBase(instance, mm);
            if (IsInsideTree())
            {
                InstanceSetScenario(instance, GetWorld3D().Scenario);
                InstanceSetTransform(instance, GlobalTransform);
            }

            var mmi = new Octant.MultimeshInstance { MultiMesh = mm, Instance = instance };
            octant.MultimeshInstances.Add(mmi);
        }

        octant.Dirty = false;
        return false;
    }

    private void OctantEnterWorld(OctantKey key)
    {
        if (!_octantMap.TryGetValue(key, out Octant octant))
            return;

        foreach (var instance in octant.MultimeshInstances)
        {
            InstanceSetScenario(instance.Instance, GetWorld3D().Scenario);
            InstanceSetTransform(instance.Instance, GlobalTransform);
        }
    }

    private void OctantExitWorld(OctantKey key)
    {
        if (!_octantMap.TryGetValue(key, out Octant octant))
            return;

        foreach (var instance in octant.MultimeshInstances)
        {
            InstanceSetScenario(instance.Instance, new Rid());
        }
    }

    private void OctantTransform(OctantKey key)
    {
        if (!_octantMap.TryGetValue(key, out Octant octant))
            return;

        foreach (var instance in octant.MultimeshInstances)
        {
            InstanceSetTransform(instance.Instance, GlobalTransform);
        }
    }

    private void OctantCleanUp(OctantKey key)
    {
        if (!_octantMap.TryGetValue(key, out Octant octant))
            return;

        foreach (var instance in octant.MultimeshInstances)
        {
            FreeRid(instance.Instance);
            FreeRid(instance.MultiMesh);
        }

        octant.MultimeshInstances.Clear();
    }

    private void UpdateVisibility()
    {
        if (!IsInsideTree())
            return;

        foreach (var instance in _octantMap.Values.SelectMany(octant => octant.MultimeshInstances))
        {
            InstanceSetVisible(instance.Instance, IsVisibleInTree());
        }
    }

    private void RecreateOctantData()
    {
        _recreatingOctants = true;
        var cellCopy = new Dictionary<IndexKey, Cell>(_cellMap);
        ClearInternal();
        foreach (var pair in cellCopy)
        {
            SetCellItem(pair.Key.ToVector3I(), pair.Value.Item, pair.Value.Rotation, pair.Value.Offset);
        }

        _recreatingOctants = false;
    }

    private void ClearInternal()
    {
        foreach (var pair in _octantMap)
        {
            if (IsInsideTree())
            {
                OctantExitWorld(pair.Key);
            }

            OctantCleanUp(pair.Key);
        }

        _octantMap.Clear();
        _cellMap.Clear();
        _aabbDirty = true;
    }

    private void UpdateAabbForCellChange(IndexKey key, Cell oldCell, Cell newCell)
    {
        if (MeshLibrary == null || !_cachedAabb.HasVolume())
        {
            _aabbDirty = true;
            return;
        }

        var itemList = new HashSet<int>(MeshLibrary.GetItemList());

        // Remove the old cell's contribution
        if (itemList.Contains(oldCell.Item))
        {
            Mesh oldMesh = MeshLibrary.GetItemMesh(oldCell.Item);
            if (oldMesh != null)
            {
                Aabb oldMeshAabb = oldMesh.GetAabb();
                if (oldMeshAabb.HasVolume())
                {
                    Aabb oldCellAabb = GetTransformedCellAabb(key, oldCell, oldMeshAabb);
                    _cachedAabb = RemoveAabbFromUnion(_cachedAabb, oldCellAabb);
                }
            }
        }

        // Add the new cell's contribution
        if (!itemList.Contains(newCell.Item))
        {
            return;
        }

        Mesh newMesh = MeshLibrary.GetItemMesh(newCell.Item);
        if (newMesh == null)
        {
            return;
        }

        Aabb newMeshAabb = newMesh.GetAabb();
        if (!newMeshAabb.HasVolume())
        {
            return;
        }

        Aabb newCellAabb = GetTransformedCellAabb(key, newCell, newMeshAabb);
        _cachedAabb = _cachedAabb.Merge(newCellAabb);
    }

    private Aabb GetTransformedCellAabb(IndexKey key, Cell cell, Aabb meshAabb)
    {
        var offset = GetOffset();
        var cellPos = new Vector3(key.X, key.Y, key.Z);

        var xform = new Transform3D(
            cell.Rotation.GetBasis().Scaled(Vector3.One * CellScale),
            cellPos * CellSize + offset + cell.Offset
        );

        var meshTransform = MeshLibrary.GetItemMeshTransform(cell.Item);
        var finalTransform = xform * meshTransform;

        return meshAabb * finalTransform;
    }

    private Aabb RemoveAabbFromUnion(Aabb union, Aabb toRemove)
    {
        // This is an approximation - if the removed AABB was contributing to the union's bounds,
        // we need to recalculate from scratch
        if (union.Position.IsEqualApprox(toRemove.Position) ||
            (union.Position + union.Size).IsEqualApprox(toRemove.Position + toRemove.Size))
        {
            _aabbDirty = true;
        }

        return union;
    }

    private void RecalculateAabb()
    {
        if (MeshLibrary == null || _cellMap.Count == 0)
        {
            _cachedAabb = new Aabb(Vector3.Zero, Vector3.Zero);
            _aabbDirty = false;
            return;
        }

        var aabb = new Aabb();
        var first = true;

        var itemList = new HashSet<int>(MeshLibrary.GetItemList());
        foreach (var pair in _cellMap)
        {
            int itemId = pair.Value.Item;
            if (!itemList.Contains(itemId))
                continue;

            Mesh mesh = MeshLibrary.GetItemMesh(itemId);
            if (mesh == null)
                continue;

            Aabb meshAabb = mesh.GetAabb();
            if (!meshAabb.HasVolume())
                continue;

            Aabb transformedAabb = GetTransformedCellAabb(pair.Key, pair.Value, meshAabb);

            if (first)
            {
                aabb = transformedAabb;
                first = false;
            }
            else
            {
                aabb = aabb.Merge(transformedAabb);
            }
        }

        _cachedAabb = aabb;
        _aabbDirty = false;
    }

    #region Internal classes

    private readonly record struct IndexKey(short X, short Y, short Z)
    {
        public IndexKey(Vector3I vector) : this((short)vector.X, (short)vector.Y, (short)vector.Z) { }

        public Vector3I ToVector3I() => new(X, Y, Z);
    }

    private record struct Cell(ushort Item, Orthogonal Rotation, Vector3 Offset);

    private class Octant
    {
        public class MultimeshInstance
        {
            public Rid Instance;
            public Rid MultiMesh;
        }

        public readonly List<MultimeshInstance> MultimeshInstances = [];
        public readonly HashSet<IndexKey> Cells = [];
        public bool Dirty;
    }

    private record struct OctantKey(short X, short Y, short Z);

    #endregion
}