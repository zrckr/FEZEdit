using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Gizmos;
using Godot;

namespace FEZEdit.Editors.Eddy;

using FEZRepacker.Core.Definitions.Game.Level;

public partial class LevelScene : Control
{
    private static readonly Dictionary<string, Gizmo3D.ToolMode> GizmoActions = new()
    {
        ["gizmo_move_mode"] = Gizmo3D.ToolMode.Move,
        ["gizmo_rotate_mode"] = Gizmo3D.ToolMode.Rotate,
        ["gizmo_scale_mode"] = Gizmo3D.ToolMode.Scale,
    };

    private const string GizmoAddTarget = "gizmo_add_target";

    private const string GizmoDeselect = "gizmo_deselect";

    public event Action<object> ValueInspected;
    
    public Level Level { private get; set; }

    private Node3D _rootNode;

    private Gizmo3D _gizmo;

    private Grid3D _grid;

    private LevelCamera _camera;

    private TimeNode _time;

    private readonly Dictionary<Gizmo3D.ToolMode, Button> _gizmoButtons = new();

    private OptionButton _viewOptionButton;

    private LineEdit _timeEdit;
    
    private Node3D _selectedNode;

    public override void _Ready()
    {
        InitializeRootNode();
        InitializeGizmo();
        InitializeGrid();
        InitializeCamera();
        InitializeTime();
    }

    public override void _Process(double delta)
    {
        if (_camera.IsMoving)
        {
            return;
        }

        foreach ((string action, var mode) in GizmoActions)
        {
            if (Input.IsActionJustPressed(action))
            {
                _gizmoButtons[mode].ButtonPressed = true;
                break;
            }
        }

        if (Input.IsActionJustPressed(GizmoDeselect))
        {
            _gizmo.ClearSelection();
            ValueInspected?.Invoke(null);
        }
    }
    
    public void FindAndSelectNode(object obj)
    {
        var proxy = _rootNode
            .FindChildren("*", nameof(MaterializerProxy))
            .OfType<MaterializerProxy>()
            .FirstOrDefault(proxy => proxy.Object == obj);

        var parent = proxy?.GetParentNode3D();
        if (parent != null)
        {
            _selectedNode = parent;
            _gizmo.ClearSelection();
            _gizmo.Select(_selectedNode);
        }
    }

    public void Materialize()
    {
        // TODO: remove this later
        var proxy = _rootNode.GetNode<MaterializerProxy>("Node3D/MaterializerProxy");
        proxy.Object = Level.ArtObjects.Values.LastOrDefault();
    }

    private void InitializeRootNode()
    {
        _rootNode = GetNode<Node3D>("%RootNode");
    }

    private void InitializeGizmo()
    {
        _gizmo = GetNode<Gizmo3D>("%Gizmo");
        InitializeToolMode(Gizmo3D.ToolMode.All, "%SelectButton");
        InitializeToolMode(Gizmo3D.ToolMode.Move, "%TranslateButton");
        InitializeToolMode(Gizmo3D.ToolMode.Rotate, "%RotateButton");
        InitializeToolMode(Gizmo3D.ToolMode.Scale, "%ScaleButton");
    }

    private void InitializeGrid()
    {
        _grid = GetNode<Grid3D>("%Grid");
    }

    private void InitializeToolMode(Gizmo3D.ToolMode mode, NodePath path)
    {
        var button = GetNode<Button>(path);
        _gizmoButtons.Add(mode, button);

        button.Toggled += pressed =>
        {
            if (pressed)
            {
                _gizmo.Mode ^= mode;
                _gizmo.UseLocalSpace = false;
            }
        };
    }

    private void InitializeCamera()
    {
        _camera = GetNode<LevelCamera>("%Camera");
        _camera.ObjectPicked += AddParentNodeToGizmo;

        _viewOptionButton = GetNode<OptionButton>("%ViewOptionButton");
        _viewOptionButton.ItemSelected += view => ChangeCameraAndGridView((LevelCamera.View)view);
        _viewOptionButton.Selected = (int)LevelCamera.View.Perspective;
    }

    private void InitializeTime()
    {
        _time = GetNode<TimeNode>("%Time");
        _time.Tick += () =>
        {
            _timeEdit.Text = _time.CurrentTime.ToString(@"hh\:mm");
        };

        _timeEdit = GetNode<LineEdit>("%TimeEdit");
        _timeEdit.FocusEntered += () => _time.SetRunning(false);
        _timeEdit.FocusExited += () => _time.SetRunning(true);
        _timeEdit.TextChanged += time =>
        {
            var currentTime = _time.CurrentTime;
            _time.CurrentTime = TimeSpan.TryParse(time, out var span) ? span : currentTime;
        };
    }

    private void AddParentNodeToGizmo(CollisionObject3D @object)
    {
        if (@object == null)
        {
            _selectedNode = null;
            _gizmo.ClearSelection();
            return;
        }

        _selectedNode = @object.GetParentNode3D();
        var selected = false;

        if (!Input.IsActionPressed(GizmoAddTarget))
        {
            _gizmo.ClearSelection();
            _gizmo.Select(_selectedNode);
            selected = true;
        }
        else if (!_gizmo.Deselect(_selectedNode))
        {
            _gizmo.Select(_selectedNode);
            selected = true;
        }

        var valueToInspect = selected && @object is MaterializerProxy proxy
            ? proxy.Object
            : null;

        ValueInspected?.Invoke(valueToInspect);
    }

    private void ChangeCameraAndGridView(LevelCamera.View view)
    {
        _camera.CurrentView = view;
        _grid.SetPlane(_camera.GetViewAxis());
    }
}