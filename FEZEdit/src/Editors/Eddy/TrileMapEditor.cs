using System.Collections.Generic;
using System.Linq;
using FEZEdit.Core;
using FEZEdit.Memento;
using Godot;

namespace FEZEdit.Editors.Eddy;

using FEZRepacker.Core.Definitions.Game.Level;

public partial class TrileMapEditor : Control
{
    [Export] private int _gridCursorSize = 50;

    [Export] private Color _gridColor = Colors.Orange;

    [Export] private float _pickDistance = 4000f;
    
    [Export] private Color _defaultColor  = new(0.0f, 0.565f, 1.0f);
    
    [Export] private Color _eraseColor = new(1.0f, 0.2f, 0.2f);
    
    [Export] private Color _pickColor = new(1.0f, 0.7f, 0.0f);

    private LevelCamera _levelCamera;
    
    private LevelScene _levelScene;

    private MementoManager _memento;

    private TrileMap _trileMap;

    private TrileMap _mainTrileMap;

    private CursorState _cursor;

    private SelectionState _selection;
    
    private ClipboardState _clipboard;

    private GridState _grid;

    public TrileMapEditor()
    {
        InitializeCursor();
        InitializeGrid();
        InitializeSelection();
        InitializeClipboard();
    }

    public override void _Ready()
    {
        // Modes
        GetNode<Button>("%InspectMode").Toggled += _ => SwitchToMode(EditMode.Inspect);
        GetNode<Button>("%SelectMode").Toggled += _ => SwitchToMode(EditMode.Select);
        GetNode<Button>("%PickMode").Toggled += _ => SwitchToMode(EditMode.Pick);
        GetNode<Button>("%PaintMode").Toggled += _ => SwitchToMode(EditMode.Paint);
        GetNode<Button>("%EraseMode").Toggled += _ => SwitchToMode(EditMode.Erase);

        // Actions
        GetNode<Button>("%RotateAction").Pressed += RotateCursorOrSelection;
        GetNode<Button>("%FillAction").Pressed += FillSelection;
        GetNode<Button>("%CopyAction").Pressed += () => CopySelection(false);
        GetNode<Button>("%CutAction").Pressed += () => CopySelection(true);
        GetNode<Button>("%DeleteAction").Pressed += DeleteSelection;

        // Grouping
        GetNode<Button>("%GroupAction").Pressed += CreateTrileGroup;
        GetNode<Button>("%UngroupAction").Pressed += RemoveTrileGroup;

        // Emplacement
        GetNode<SpinBox>("%LevelBox").ValueChanged += value => ChangeLevel((int)value);
        GetNode<OptionButton>("%LevelOption").ItemSelected += axis => ShowLevelGrid((Vector3.Axis) axis);
        GetNode<Button>("%ResetOffset").Pressed += ResetOffset;
        GetNode<SpinBox>("%XBox").ValueChanged += value => UpdateOffset((float)value, Vector3.Axis.X);
        GetNode<SpinBox>("%YBox").ValueChanged += value => UpdateOffset((float)value, Vector3.Axis.Y);
        GetNode<SpinBox>("%ZBox").ValueChanged += value => UpdateOffset((float)value, Vector3.Axis.Z);
    }

    public void Initialize(LevelScene levelScene, LevelCamera levelCamera)
    {
        _levelCamera = levelCamera;
        _levelScene = levelScene;
        _levelScene.AddChild(_cursor.Instance);
        _levelScene.AddChild(_selection.Instance);
        _levelScene.AddChild(_clipboard.Instance);
        foreach (var instance in _grid.Instances)
        {
            _levelScene.AddChild(instance);
        }
    }

    public void Deinitialize()
    {
        _levelScene.RemoveChild(_cursor.Instance);
        _levelScene.RemoveChild(_selection.Instance);
        _levelScene.RemoveChild(_clipboard.Instance);
        foreach (var instance in _grid.Instances)
        {
            _levelScene.RemoveChild(instance);
        }
    }

    public void Edit(TrileMap trileMap, TrileMap mainTrileMap = null)
    {
        if (_cursor.Mode != EditMode.Inspect)
        {
            GetNode<Button>("%InspectMode").ButtonPressed = true;
        }
        
        UpdateCursor();
        SetSelection(SelectMode.None);

        _trileMap = trileMap;
        _mainTrileMap = mainTrileMap;
        _memento = _trileMap?.Memento;

        if (_trileMap == null)
        {
            HideGrid();
        }
        else
        {
            UpdateGrid();
        }
    }
    
    #region Initialization

    private void InitializeCursor()
    {
        var corners = new Vector3[]
        {
            new(-0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, 0.5f),
            new(-0.5f, -0.5f, 0.5f),
            new(-0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, 0.5f),
            new(-0.5f, 0.5f, 0.5f)
        };
        
        _cursor.Mesh = new ArrayMesh();
        _cursor.Instance = new MeshInstance3D { Name = "Cursor" };
        _cursor.Instance.Mesh = _cursor.Mesh;
        
        _cursor.InnerMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(_defaultColor, 0.2f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            DisableFog = true
        };

        var triangles = new List<Vector3>();
        var faces = new int[][]
        {
            [0, 1, 2, 0, 2, 3],  // bottom
            [4, 5, 6, 4, 6, 7],  // top
            [0, 1, 5, 0, 5, 4],  // front
            [2, 3, 7, 2, 7, 6],  // back
            [0, 3, 7, 0, 7, 4],  // left
            [1, 2, 6, 1, 6, 5] // right
        };

        foreach (var face in faces)
        {
            for (int i = 0; i < 6; i += 3)
            {
                triangles.Add(corners[face[i]]);
                triangles.Add(corners[face[i + 1]]);
                triangles.Add(corners[face[i + 2]]);
            }
        }

        var triArrays = new Godot.Collections.Array();
        triArrays.Resize((int)Mesh.ArrayType.Max);
        triArrays[(int)Mesh.ArrayType.Vertex] = triangles.ToArray();

        _cursor.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, triArrays);
        _cursor.Mesh.SurfaceSetMaterial(0, _cursor.InnerMaterial);

        // Outer mesh
        _cursor.OuterMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(_defaultColor, 0.8f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass,
            DisableFog = true,
            RenderPriority = 1
        };

        var lines = new List<Vector3>();
        var edges = new[]
        {
            0, 1, 1, 2, 2, 3, 3, 0,  // bottom
            4, 5, 5, 6, 6, 7, 7, 4,  // top
            0, 4, 1, 5, 2, 6, 3, 7   // vertical
        };

        for (int i = 0; i < edges.Length; i += 2)
        {
            lines.Add(corners[edges[i]]);
            lines.Add(corners[edges[i + 1]]);
        }

        var lineArrays = new Godot.Collections.Array();
        lineArrays.Resize((int)Mesh.ArrayType.Max);
        lineArrays[(int)Mesh.ArrayType.Vertex] = lines.ToArray();

        _cursor.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, lineArrays);
        _cursor.Mesh.SurfaceSetMaterial(1, _cursor.OuterMaterial);
    }

    private void InitializeGrid()
    {
        _grid.Material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            VertexColorIsSrgb = true,
            VertexColorUseAsAlbedo = true,
            DisableFog = true,
            AlbedoColor = _gridColor
        };

        _grid.Instances = new MeshInstance3D[3];
        for (int i = 0; i < 3; i++)
        {
            var axis = Vector3.Zero;
            axis[i] = 1.0f;
            var axisN1 = Vector3.Zero;
            axisN1[(i + 1) % 3] = 1.0f;
            var axisN2 = Vector3.Zero;
            axisN2[(i + 2) % 3] = 1.0f;

            var vertices = new List<Vector3>();
            var colors = new List<Color>();

            for (int j = -_gridCursorSize; j <= _gridCursorSize; j++)
            {
                for (int k = -_gridCursorSize; k <= _gridCursorSize; k++)
                {
                    var p = axisN1 * j + axisN2 * k;
                    var trans = Mathf.Pow(Mathf.Max(0.0f, 1.0f - (new Vector2(j, k).Length() / _gridCursorSize)), 2.0f);

                    var pj = axisN1 * (j + 1) + axisN2 * k;
                    var transj = Mathf.Pow(Mathf.Max(0, 1.0f - (new Vector2(j + 1, k).Length() / _gridCursorSize)),
                        2.0f);

                    var pk = axisN1 * j + axisN2 * (k + 1);
                    var transk = Mathf.Pow(Mathf.Max(0, 1.0f - (new Vector2(j, k + 1).Length() / _gridCursorSize)),
                        2.0f);

                    vertices.Add(p);
                    vertices.Add(pk);
                    colors.Add(new Color(1, 1, 1, trans));
                    colors.Add(new Color(1, 1, 1, transk));

                    vertices.Add(p);
                    vertices.Add(pj);
                    colors.Add(new Color(1, 1, 1, trans));
                    colors.Add(new Color(1, 1, 1, transj));
                }
            }
            
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
            arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();

            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
            mesh.SurfaceSetMaterial(0, _grid.Material);

            _grid.Instances[i] = new MeshInstance3D { Name = $"Grid Plane {(Vector3.Axis)i}" };
            _grid.Instances[i].Mesh = mesh;
        }
    }
    
    private void InitializeSelection()
    {
        _selection.Instance = new MeshInstance3D { Name = "Selection" };
        _selection.Instance.Mesh = _cursor.Mesh.Duplicate() as ArrayMesh;
    }

    private void InitializeClipboard()
    {
        _clipboard.Instance = new Node3D { Name = "Clipboard" };
        _clipboard.Items = [];
    }
    
    #endregion
    
    #region Input handling

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_trileMap != null && _cursor.Mode != EditMode.Inspect)
        {
            switch (@event)
            {
                case InputEventMouseMotion motion when HandleMouseMotion(motion):
                case InputEventMouseButton button when HandleMouseButton(button):
                case InputEventKey key when HandleKeyInput(key):
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }
    }

    private bool HandleMouseMotion(InputEventMouseMotion motion)
    {
        var from = _levelCamera.ProjectRayOrigin(motion.Position);
        var normal = _levelCamera.ProjectRayNormal(motion.Position);
        
        var localXform = _trileMap.GlobalTransform.AffineInverse();
        from = localXform * from;
        normal = (localXform.Basis * normal).Normalized();

        var planeNormal = Vector3.Zero;
        planeNormal[(int)_grid.Axis] = 1.0f;
        var p = new Plane(planeNormal, _grid.Level[(int)_grid.Axis]);

        var intersection = p.IntersectsSegment(from, from + normal * _pickDistance);
        if (!intersection.HasValue)
        {
            return false;
        }
        
        // Make sure the intersection is inside the frustum planes,
        // to avoid painting on invisible regions
        foreach (var plane in _levelCamera.GetFrustum())
        {
            var frustumPlane = localXform * plane;
            if (frustumPlane.IsPointOver(intersection.Value))
            {
                return false;
            }
        }

        var emplacement = Vector3I.Zero;
        for (int i = 0; i < 3; i++)
        {
            var axis = (Vector3.Axis)i;
            if (axis == _grid.Axis)
            {
                emplacement[i] = (int)_grid.Level[i];
                continue;
            }

            emplacement[i] = (int)intersection.Value[i];  // Drop fractional part
            if (emplacement[i] < 0)
            {
                emplacement[i] -= 1;    // Compensate negative
            }
        }

        _cursor.Emplacement = emplacement;
        UpdateCursor();
        UpdateClipboard();
        UpdateGrid();

        switch (_cursor.Mode)
        {
            case EditMode.Select:
                ResizeSelection(emplacement);
                return true;
            
            case EditMode.Paint when _cursor.Holding:
                PaintTrile(_cursor);
                return true;
            
            case EditMode.Erase when _cursor.Holding:
                EraseTrile(_cursor);
                return true;
        }

        return false;
    }

    private bool HandleMouseButton(InputEventMouseButton button)
    {
        if (button.ButtonIndex != MouseButton.Left)
        {
            _cursor.Holding = false;
            return false;
        }

        _cursor.Holding = button.Pressed;
        if (!button.Pressed && _cursor.Mode == EditMode.Select)
        {
            FinishSelection();
            return true;
        }

        if (button.Pressed && _clipboard.Items.Count > 0)
        {
            PasteClipboard();
            return true;
        }

        switch (_cursor.Mode)
        {
            case EditMode.Select:
                StartSelection(_cursor.Emplacement);
                return true;
            
            case EditMode.Pick:
                PickTrile(_cursor);
                return true;
            
            case EditMode.Paint:
                PaintTrile(_cursor);
                return true;
            
            case EditMode.Erase:
                EraseTrile(_cursor);
                return true;
        }
        
        return false;
    }

    private bool HandleKeyInput(InputEventKey key)
    {
        if (!key.Pressed || key.Echo)
        {
            return false;
        }

        switch (key.Keycode)
        {
            case Key.Escape:
                {
                    if (_selection.Mode == SelectMode.Active)
                    {
                        SetSelection(SelectMode.None);
                        return true;
                    }
                    
                    if (_clipboard.Items.Count > 0)
                    {
                        ClearClipboard();
                        return true;
                    }
                    
                    if (_cursor.Mode != EditMode.Inspect)
                    {
                        GetNode<Button>("%InspectMode").ButtonPressed = true;
                        return true;
                    }
                    
                    break;
                }

            case Key.Delete:
                DeleteSelection();
                return true;
        }
        
        return false;
    }

    #endregion
    
    #region Update visuals

    private void UpdateCursor()
    {
        if (_clipboard.Items.Count > 0 || _trileMap == null || _cursor.Mode == EditMode.Inspect)
        {
            _cursor.Instance.Hide();
            return;
        }
        
        var xform = Transform3D.Identity;
        xform.Origin = new Vector3(_cursor.Emplacement.X, _cursor.Emplacement.Y, _cursor.Emplacement.Z) +
                       TrileMap.EmplacementCenter +
                       _cursor.Offset;

        if (_cursor.Mode == EditMode.Paint)
        {
            xform.Basis = TrileMap.GetPhiBasis(_cursor.Phi);
            
            _cursor.Instance.Transform = xform;
            _cursor.Instance.Mesh = _trileMap.GetTrileVisualMesh(_cursor.Id);
            _cursor.Instance.Show();
            return;
        }

        var currentColor = _cursor.Mode switch
        {
            EditMode.Erase => _eraseColor,
            EditMode.Pick => _pickColor,
            _ => _defaultColor
        };

        _cursor.InnerMaterial.AlbedoColor = new Color(currentColor, 0.2f);
        _cursor.OuterMaterial.AlbedoColor = new Color(currentColor, 0.8f);
        _cursor.Instance.Transform = xform;
        _cursor.Instance.Mesh = _cursor.Mesh;
        _cursor.Instance.Show();
    }

    private void UpdateGrid()
    {
        for (int i = 0; i < 3; i++)
        {
            var axis = (Vector3.Axis)i;
            var axisN1 = (i + 1) % 3;
            var axisN2 = (i + 2) % 3;
            
            var instance = _grid.Instances[i];
            var xform = Transform3D.Identity;
            if (axis == _grid.Axis)
            {
                xform.Origin[i] = _grid.Level[i];
            }

            xform.Basis = axis switch
            {
                Vector3.Axis.X => Basis.Identity.Rotated(Vector3.Right, Mathf.Pi / 2f),
                Vector3.Axis.Z => Basis.Identity.Rotated(Vector3.Back, Mathf.Pi / 2f),
                _ => Basis.Identity
            };

            if (_cursor.Mode != EditMode.Inspect)
            {
                xform.Origin[axisN1] = _cursor.Emplacement[axisN1];
                xform.Origin[axisN2] = _cursor.Emplacement[axisN2];
            }
            else
            {
                xform.Origin[axisN1] = 0f;
                xform.Origin[axisN2] = 0f;
            }

            instance.Transform = xform;
            instance.Visible = axis == _grid.Axis;
        }
    }

    private void UpdateSelection()
    {
        if (_selection.Mode == SelectMode.None)
        {
            _selection.Instance.Hide();
            return;
        }

        var size = _selection.End - _selection.Begin + Vector3.One;
        var xform =  Transform3D.Identity;
        xform.Origin = _selection.Begin + size * 0.5f;
        xform.Basis = xform.Basis.Scaled(size);
        
        _selection.Instance.Transform = xform;
        _selection.Instance.Show();
    }

    private void UpdateClipboard()
    {
        if (_clipboard.Items.Count < 1)
        {
            _clipboard.Instance.Hide();
            return;
        }
        
        var xform =  Transform3D.Identity;
        xform.Origin = new Vector3(_cursor.Emplacement.X, _cursor.Emplacement.Y, _cursor.Emplacement.Z) +
                       TrileMap.EmplacementCenter +
                       _cursor.Offset;
        xform.Basis = TrileMap.GetPhiBasis(_cursor.Phi);
        _clipboard.Instance.Transform = xform;
        _clipboard.Instance.Show();

        var instances = _clipboard.Instance.GetChildren();
        foreach (var item in _clipboard.Items.Where(item => !instances.Contains(item.Instance)))
        {
            _clipboard.Instance.AddChild(item.Instance);
            item.Instance.Position = item.RelativeOffset;
            item.Instance.Basis = TrileMap.GetPhiBasis(item.Original.PhiLight);
        }
    }

    private void HideGrid()
    {
        foreach (var instance in _grid.Instances)
        {
            instance.Hide();
        }
    }
    
    #endregion

    #region Trile operations

    public void SetTrile(int trileId)
    {
        _cursor.Id = trileId;
        if (_cursor.Mode != EditMode.Paint)
        {
            GetNode<Button>("%PaintMode").ButtonPressed = true;
        }
        else
        {
            UpdateCursor();
        }
    }

    private void SwitchToMode(EditMode mode)
    {
        if (_cursor.Mode != mode)
        {
            _cursor.Mode = mode;
            UpdateCursor();
            if (mode == EditMode.Inspect)
            {
                UpdateGrid();
            }
        }
    }

    private void PickTrile(CursorState cursor)
    {
        var instance = _trileMap.GetTrile(cursor.Emplacement.ToXna());
        if (instance != null)
        {
            _cursor.Id = instance.TrileId;
            if (_cursor.Mode != EditMode.Paint)
            {
                GetNode<Button>("%PaintMode").ButtonPressed = true;
            }
            else
            {
                UpdateCursor();
            }
        }
    }

    private void PaintTrile(CursorState cursor)
    {
        using (_memento.BeginScope("Paint trile"))
        {
            _trileMap.SetTrile(cursor.Emplacement.ToXna(), cursor.ToInstance());
        }
    }

    private void EraseTrile(CursorState cursor)
    {
        using (_memento.BeginScope("Erase trile"))
        {
            _trileMap.SetTrile(cursor.Emplacement.ToXna(), null);
        }
    }

    #endregion

    #region Selection operations

    private void SetSelection(SelectMode mode, Vector3I? click = null, Vector3I? current = null)
    {
        _selection.Mode = mode;
        _selection.Click = click ?? Vector3I.Zero;
        _selection.Current = current ?? Vector3I.Zero;
        if (_trileMap?.IsVisibleInTree() ?? false)
        {
            UpdateSelection();
        }
    }

    private void StartSelection(Vector3I emplacement)
    {
        if (_selection.Mode != SelectMode.Selecting)
        {
            _selection.Current = emplacement;
            _selection.Click = emplacement;
            _selection.Mode = SelectMode.Selecting;
            _selection.Validate();
            UpdateSelection();
        }
    }
    
    private void ResizeSelection(Vector3I emplacement)
    {
        if (_selection.Mode == SelectMode.Selecting)
        {
            _selection.Current = emplacement;
            _selection.Validate();
            UpdateSelection();
        }
    }

    private void FinishSelection()
    {
        if (_selection.Mode == SelectMode.Selecting)
        {
            using (_memento.BeginScope("Select triles"))
            {
                _selection.Mode = SelectMode.Active;
                UpdateSelection();
            }
        }
    }
    
    private void RotateCursorOrSelection()
    {
        _cursor.Phi = TrileMap.RotatePhi(_cursor.Phi, Vector3.Up, Mathf.Pi / 2f);
        UpdateCursor();
        UpdateClipboard();
    }

    private void FillSelection()
    {
        var emplacements = GetEmplacementsInSelection();
        if (emplacements.Count < 1)
        {
            return;
        }
        
        using (_memento.BeginScope("Fill selection"))
        {
            foreach (var emplacement in emplacements)
            {
                _trileMap.SetTrile(emplacement, _cursor.ToInstance());
            }
            SetSelection(SelectMode.None);
        }
    }

    private void CopySelection(bool cutAction)
    {
        var emplacements = GetEmplacementsInSelection();
        if (emplacements.Count < 1)
        {
            return;
        }
        
        ClearClipboard();
        SetClipboard(emplacements);

        if (cutAction)
        {
            DeleteSelection();
        }

        _selection = new SelectionState();
        UpdateSelection();
    }

    private void DeleteSelection()
    {
        var emplacements = GetEmplacementsInSelection();
        if (emplacements.Count < 1)
        {
            return;
        }
        
        using (_memento.BeginScope("Delete selection"))
        {
            foreach (var emplacement in emplacements)
            {
                _trileMap.SetTrile(emplacement, null);
            }
            SetSelection(SelectMode.None);
        }
    }

    private HashSet<TrileEmplacement> GetEmplacementsInSelection()
    {
        var emplacements = new HashSet<TrileEmplacement>();
        if (_selection.Mode != SelectMode.Active)
        {
            return emplacements;
        }
        
        for (int x = _selection.Begin.X; x <= _selection.End.X; x++)
        {
            for (int y = _selection.Begin.Y; y <= _selection.End.Y; y++)
            {
                for (int z = _selection.Begin.Z; z <= _selection.End.Z; z++)
                {
                    emplacements.Add(new TrileEmplacement(x, y, z));
                }
            }
        }
        
        return emplacements;
    }

    #endregion

    #region Grouping operations

    private void CreateTrileGroup()
    {
        var emplacements = GetEmplacementsInSelection();
        if (emplacements.Count < 1)
        {
            return;
        }
        
        var groupTrileMap = new TrileMap { Name = "Triles", TrileSet = _trileMap.TrileSet };
        
        using (_memento.BeginScope("Create trile group"))
        {
            _levelScene.AddChild(groupTrileMap, true);
            
            foreach (var emplacement in emplacements)
            {
                var instance = _trileMap.GetTrile(emplacement);
                if (instance != null)
                {
                    groupTrileMap.SetTrile(emplacement, instance);
                    _trileMap.SetTrile(emplacement, null);
                }
            }
            
            SetSelection(SelectMode.None);
        }
    }

    private void RemoveTrileGroup()
    {
        var emplacements = GetEmplacementsInSelection();
        if (emplacements.Count < 1)
        {
            return;
        }
        
        using (_memento.BeginScope("Destroy trile group"))
        {
            foreach (var emplacement in emplacements)
            {
                var instance = _trileMap.GetTrile(emplacement);
                if (instance != null)
                {
                    _trileMap.SetTrile(emplacement, null);
                    _mainTrileMap.SetTrile(emplacement, instance);
                }
            }
            
            _levelScene.RemoveChild(_trileMap);
            SetSelection(SelectMode.None);
        }
    }

    #endregion
    
    #region Clipboard operations

    private void SetClipboard(IEnumerable<TrileEmplacement> emplacements)
    {
        foreach (var emplacement in emplacements)
        {
            var instance = _trileMap.GetTrile(emplacement);
            if (instance == null)
            {
                continue;
            }

            var item = new ClipboardItem
            {
                Original = instance,
                RelativeOffset = emplacement.ToGodot() - _selection.Begin,
                Instance = new MeshInstance3D { Mesh = _trileMap.GetTrileVisualMesh(instance.TrileId) }
            };

            _clipboard.Items.Add(item);
        }
    }
    
    private void PasteClipboard()
    {
        var basis = TrileMap.GetPhiBasis(_cursor.Phi);
        
        using (_memento.BeginScope("Paste selection"))
        {
            foreach (var item in _clipboard.Items)
            {
                var itemEmplacement = basis * item.RelativeOffset + _cursor.Emplacement;
                var itemBasis = basis * TrileMap.GetPhiBasis(item.Original.PhiLight);
                var itemPosition = basis * item.Original.Position.ToGodot();

                var newInstance = new TrileInstance
                {
                    TrileId = item.Original.TrileId,
                    PhiLight = TrileMap.FindPhi(itemBasis),
                    Position = itemPosition.ToXna(),
                    ActorSettings = item.Original.ActorSettings
                };
                
                var emplacement = new TrileEmplacement((int)itemEmplacement.X, (int)itemEmplacement.Y, (int)itemEmplacement.Z);
                _trileMap.SetTrile(emplacement, newInstance);
            }
        }
        
        ClearClipboard();
    }

    private void ClearClipboard()
    {
        foreach (var item in _clipboard.Items.Where(item => item.Instance?.IsInsideTree() == true))
        {
            item.Instance?.QueueFree();
        }
        _clipboard.Items.Clear();
    }

    #endregion

    #region Grid and emplacement parameters

    private void ChangeLevel(int value)
    {
        var level = _grid.Level;
        level[(int)_grid.Axis] = value;
        _grid.Level = level;
        UpdateGrid();

        if (_selection.Mode == SelectMode.Active && _cursor.Mode == EditMode.Select)
        {
            var current = _selection.Current;
            current[(int)_grid.Axis] = value;
            _selection.Current = current;
            _selection.Validate();
            UpdateSelection();
        }
    }

    private void ShowLevelGrid(Vector3.Axis axis)
    {
        _grid.Axis = axis;
        UpdateGrid();
    }

    private void UpdateOffset(float value, Vector3.Axis axis)
    {
        var offset = _cursor.Offset;
        offset[(int)axis] = value;
        _cursor.Offset = offset;
        GetNode<Button>("%ResetOffset").Disabled = _cursor.Offset.IsZeroApprox();
        UpdateCursor();
    }

    private void ResetOffset()
    {
        _cursor.Offset = Vector3.Zero;
        GetNode<Button>("%ResetOffset").Disabled = false;
        UpdateCursor();
    }

    #endregion

    #region Internal types

    private enum EditMode
    {
        Inspect,
        Select,
        Pick,
        Paint,
        Erase
    }

    private enum SelectMode
    {
        None,
        Selecting,
        Active
    }

    private struct CursorState
    {
        public EditMode Mode { get; set; }
        public bool Holding { get; set; }
        public int Id { get; set; }
        public byte Phi { get; set; }
        public Vector3I Emplacement { get; set; }
        public Vector3 Offset { get; set; }
        public MeshInstance3D Instance { get; set; }
        public ArrayMesh Mesh { get; set; }
        public StandardMaterial3D InnerMaterial { get; set; }
        public StandardMaterial3D OuterMaterial { get; set; }

        public TrileInstance ToInstance()
        {
            return new TrileInstance
            {
                TrileId = Id, PhiLight = Phi, Position = (Emplacement + Offset).ToXna(), ActorSettings = null
            };
        }
    }

    private struct SelectionState
    {
        public SelectMode Mode { get; set; }
        public Vector3I Begin { get; private set; }
        public Vector3I End { get; private set; }
        public Vector3I Click { get; set; }
        public Vector3I Current { get; set; }
        public MeshInstance3D Instance { get; set; }

        public void Validate()
        {
            if (Mode != SelectMode.None)
            {
                var begin = Click;
                var end = Current;

                for (int i = 0; i < 3; i++)
                {
                    if (begin[i] > end[i])
                    {
                        (begin[i], end[i]) = (end[i], begin[i]);
                    }
                }
                
                Begin = begin;
                End = end;
            }
        }
    }

    private struct GridState
    {
        public MeshInstance3D[] Instances { get; set; }
        public Vector3 Level { get; set; }
        public Vector3.Axis Axis { get; set; }
        public StandardMaterial3D Material { get; set; }
    }

    private struct ClipboardState
    {
        public List<ClipboardItem> Items { get; set; }
        public Node3D Instance { get; set; }
    }

    private struct ClipboardItem
    {
        public TrileInstance Original { get; init; }
        public Vector3 RelativeOffset { get; init; }
        public MeshInstance3D Instance { get; init; }
    }

    #endregion
}