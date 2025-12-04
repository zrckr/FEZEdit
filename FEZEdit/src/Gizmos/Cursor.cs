using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Gizmos;

public partial class Cursor : Node
{
    private static readonly Color DefaultColor = new(0.0f, 0.565f, 1.0f);

    public event Action Pressed;

    public event Action Canceled;

    public event Action Deleted;

    public event Action<float, Vector3.Axis> LevelChanged;

    [Export] private Vector3 _cursorOffset = Vector3.One / 2f;
    
    [Export] private Color _cursorColor = DefaultColor;

    [Export] private Color _gridColor = Colors.Orange;

    [Export] private int _gridCursorSize = 50;

    [Export] private float _gridPickDistance = 4000f;

    private CursorState _cursor;

    private GridState _grid;

    private SelectionState _selection;

    public override void _EnterTree()
    {
        InitializeCursor();
        InitializeGrid();
        InitializeSelection();
    }

    #region Cursor

    private void InitializeCursor()
    {
        var corners = new Vector3[]
        {
            new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, 0.5f), new(-0.5f, -0.5f, 0.5f),
            new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f)
        };

        var meshInstance3D = new MeshInstance3D { Name = "Cursor" };
        _cursor.DefaultMesh = new ArrayMesh();
        meshInstance3D.Mesh = _cursor.DefaultMesh;
        _cursor.Instance = meshInstance3D;
        AddChild(_cursor.Instance, true);

        _cursor.InnerMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(_cursorColor, 0.2f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            DisableFog = true
        };

        var triangles = new List<Vector3>();
        var faces = new int[][]
        {
            [0, 1, 2, 0, 2, 3], // bottom
            [4, 5, 6, 4, 6, 7], // top
            [0, 1, 5, 0, 5, 4], // front
            [2, 3, 7, 2, 7, 6], // back
            [0, 3, 7, 0, 7, 4], // left
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

        _cursor.DefaultMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, triArrays);
        _cursor.DefaultMesh.SurfaceSetMaterial(0, _cursor.InnerMaterial);

        // Outer mesh
        _cursor.OuterMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(_cursorColor, 0.8f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass,
            DisableFog = true,
            RenderPriority = 1
        };

        var lines = new List<Vector3>();
        var edges = new[]
        {
            0, 1, 1, 2, 2, 3, 3, 0, // bottom
            4, 5, 5, 6, 6, 7, 7, 4, // top
            0, 4, 1, 5, 2, 6, 3, 7 // vertical
        };

        for (int i = 0; i < edges.Length; i += 2)
        {
            lines.Add(corners[edges[i]]);
            lines.Add(corners[edges[i + 1]]);
        }

        var lineArrays = new Godot.Collections.Array();
        lineArrays.Resize((int)Mesh.ArrayType.Max);
        lineArrays[(int)Mesh.ArrayType.Vertex] = lines.ToArray();

        _cursor.DefaultMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, lineArrays);
        _cursor.DefaultMesh.SurfaceSetMaterial(1, _cursor.OuterMaterial);
    }

    public Transform3D GetCursorEmplacement()
    {
        return _cursor.Emplacement;
    }
    
    public void SetCursorInstance(GeometryInstance3D instance, Color? cursorColor = null)
    {
        var emplacement = _cursor.Emplacement;
        emplacement.Basis = Basis.Identity;
        _cursor.Instance.QueueFree();
        
        if (instance != null)
        {
            _cursor.Instance = instance;
        }
        else
        {
            _cursor.InnerMaterial.AlbedoColor = new Color(cursorColor ?? DefaultColor, 0.2f);
            _cursor.OuterMaterial.AlbedoColor = new Color(cursorColor ?? DefaultColor, 0.8f);
            var meshInstance = new MeshInstance3D { Name = "Cursor" };
            meshInstance.Mesh = _cursor.DefaultMesh;
            _cursor.Instance = meshInstance;
        }
        
        AddChild(_cursor.Instance, true);
        SetCursorEmplacement(emplacement);
    }

    public void SetCursorEmplacement(Transform3D emplacement)
    {
        if (_cursor.Instance.Visible)
        {
            _cursor.Emplacement = emplacement;
            _cursor.Instance.Position = emplacement.Origin + _cursorOffset;
            _cursor.Instance.Basis = emplacement.Basis;
        }
    }

    public void SetCursorRotation(Quaternion quaternion)
    {
        if (_cursor.Instance.Visible)
        {
            _cursor.Instance.Quaternion = quaternion;
        }
    }

    public void SetCursorSelecting(bool selecting)
    {
        _cursor.Selecting = selecting;
        _cursor.Instance.Visible = !selecting;
    }

    public void RotateCursor()
    {
        if (_cursor.Instance.Visible)
        {
            var emplacement = _cursor.Emplacement;
            emplacement.Basis = emplacement.Basis.Rotated(Vector3.Up, Mathf.Pi / 2f);
            _cursor.Emplacement = emplacement;
                
        }
    }

    #endregion

    #region Grid

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

        _grid.Axis = Vector3.Axis.Y;
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
            _grid.Instances[i].Visible = i == (int)_grid.Axis;
            AddChild(_grid.Instances[i], true);
        }
    }

    public void ShowGridAxis(Vector3.Axis axis)
    {
        _grid.Axis = axis;
        UpdateGrid();
        LevelChanged?.Invoke(_grid.Level[(int)axis], axis);
    }

    public void HideGrid()
    {
        foreach (var instance in _grid.Instances)
        {
            instance.Hide();
        }
    }

    public void SetGridLevel(float value)
    {
        var level = _grid.Level;
        level[(int)_grid.Axis] = value;
        _grid.Level = level;
        UpdateGrid();
        LevelChanged?.Invoke(value, _grid.Axis);

        if (_selection.CurrentState == SelectionState.State.Active && _cursor.Selecting)
        {
            var current = _selection.Current;
            current[(int)_grid.Axis] = value;
            _selection.Current = current;
            _selection.Validate();
            UpdateSelection();
        }
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
            
            xform.Origin[i] = _grid.Level[i];
            xform.Origin[axisN1] = _cursor.Emplacement.Origin[axisN1];
            xform.Origin[axisN2] = _cursor.Emplacement.Origin[axisN2];
            xform.Basis = axis switch
            {
                Vector3.Axis.X => Basis.Identity.Rotated(Vector3.Right, Mathf.Pi / 2f),
                Vector3.Axis.Z => Basis.Identity.Rotated(Vector3.Back, Mathf.Pi / 2f),
                _ => Basis.Identity
            };

            instance.Transform = xform;
            instance.Visible = axis == _grid.Axis;
        }
    }

    #endregion

    #region Selection

    private void InitializeSelection()
    {
        _selection.CurrentState = SelectionState.State.None;
        _selection.Instance = new MeshInstance3D { Name = "Selection" };
        _selection.Instance.Mesh = _cursor.DefaultMesh.Duplicate() as ArrayMesh;
        AddChild(_selection.Instance, true);
        UpdateSelection();
    }

    public (Vector3 Begin, Vector3 End)? GetSelection()
    {
        if (_selection.CurrentState == SelectionState.State.Active)
        {
            return (_selection.Begin, _selection.End);
        }

        return null;
    }

    private void StartSelection(Vector3 position)
    {
        if (_selection.CurrentState != SelectionState.State.Selecting)
        {
            _selection.Current = position;
            _selection.Click = position;
            _selection.CurrentState = SelectionState.State.Selecting;
            _selection.Validate();
            UpdateSelection();
        }
    }

    private void ResizeSelection(Vector3 position)
    {
        if (_selection.CurrentState == SelectionState.State.Selecting)
        {
            _selection.Current = position;
            _selection.Validate();
            UpdateSelection();
        }
    }

    private void FinishSelection()
    {
        if (_selection.CurrentState == SelectionState.State.Selecting)
        {
            _selection.CurrentState = SelectionState.State.Active;
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        if (_selection.CurrentState == SelectionState.State.None)
        {
            _selection.Instance.Hide();
            return;
        }

        var size = _selection.End - _selection.Begin + Vector3.One;
        var xform = Transform3D.Identity;
        xform.Origin = _selection.Begin + size * 0.5f;
        xform.Basis = xform.Basis.Scaled(size);

        _selection.Instance.Transform = xform;
        _selection.Instance.Show();
    }

    public void CancelSelection()
    {
        if (_selection.CurrentState == SelectionState.State.Active)
        {
            _selection.CurrentState = SelectionState.State.None;
            _selection.Click = Vector3.Zero;
            _selection.Current = Vector3.Zero;
            _selection.Validate();
            UpdateSelection();
        }
    }

    #endregion

    #region Input handling

    public override void _UnhandledInput(InputEvent @event)
    {
        var handled = @event switch
        {
            InputEventMouseMotion motion => HandleMouseMotion(motion),
            InputEventMouseButton button => HandleMouseButton(button),
            InputEventKey key => HandleKeyInput(key),
            _ => false
        };

        if (handled)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private bool HandleMouseMotion(InputEventMouseMotion motion)
    {
        var camera = GetViewport().GetCamera3D();
        var from = camera.ProjectRayOrigin(motion.Position);
        var normal = camera.ProjectRayNormal(motion.Position);
        
        var planeNormal = Vector3.Zero;
        planeNormal[(int)_grid.Axis] = 1.0f;
        var p = new Plane(planeNormal, _grid.Level[(int)_grid.Axis]);

        var intersection = p.IntersectsSegment(from, from + normal * _gridPickDistance);
        if (!intersection.HasValue)
        {
            return false;
        }

        // Make sure the intersection is inside the frustum planes,
        // to avoid painting on invisible regions
        if (camera.GetFrustum().Any(plane => plane.IsPointOver(intersection.Value)))
        {
            return false;
        }

        var position = Vector3.Zero;
        for (int i = 0; i < 3; i++)
        {
            if ((Vector3.Axis)i == _grid.Axis)
            {
                position[i] = (int)_grid.Level[i];
                continue;
            }

            position[i] = (int)intersection.Value[i]; // Drop fractional part
            if (position[i] < 0f)
            {
                position[i] -= 1f; // Compensate negative
            }
        }

        var emplacement = _cursor.Emplacement;
        emplacement.Origin = position;
        SetCursorEmplacement(emplacement);
        UpdateGrid();

        if (_cursor.Pressed)
        {
            Pressed?.Invoke();
            if (_cursor.Selecting)
            {
                ResizeSelection(position);
            }
        }

        return true;
    }

    private bool HandleMouseButton(InputEventMouseButton button)
    {
        if (button.ButtonIndex == MouseButton.Middle && button.Pressed)
        {
            RotateCursor();
            return true;
        }
        
        if (button.ButtonIndex != MouseButton.Left)
        {
            _cursor.Pressed = false;
            return false;
        }

        _cursor.Pressed = button.Pressed;
        if (_cursor is { Pressed: false, Selecting: true })
        {
            FinishSelection();
            return true;
        }
        
        if (_cursor.Pressed)
        {
            Pressed?.Invoke();
            if (_cursor.Selecting)
            {
                StartSelection(_cursor.Emplacement.Origin);
            }
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
                Canceled?.Invoke();
                CancelSelection();
                return true;

            case Key.Delete:
                Deleted?.Invoke();
                return true;

            default:
                return false;
        }
    }

    #endregion

    #region Internal types

    private struct CursorState
    {
        public bool Selecting { get; set; }
        public bool Pressed { get; set; }
        public Transform3D Emplacement { get; set; }
        public GeometryInstance3D Instance { get; set; }
        public ArrayMesh DefaultMesh { get; set; }
        public StandardMaterial3D InnerMaterial { get; set; }
        public StandardMaterial3D OuterMaterial { get; set; }
    }

    private struct GridState
    {
        public MeshInstance3D[] Instances { get; set; }
        public Vector3 Level { get; set; }
        public Vector3.Axis Axis { get; set; }
        public StandardMaterial3D Material { get; set; }
    }

    private struct SelectionState
    {
        public enum State
        {
            None,
            Selecting,
            Active
        }

        public State CurrentState { get; set; }
        public Vector3 Begin { get; private set; }
        public Vector3 End { get; private set; }
        public Vector3 Click { get; set; }
        public Vector3 Current { get; set; }
        public MeshInstance3D Instance { get; set; }

        public void Validate()
        {
            if (CurrentState == State.None)
            {
                Begin = Vector3.Zero;
                End = Vector3.Zero;
                return;
            }

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

    #endregion
}