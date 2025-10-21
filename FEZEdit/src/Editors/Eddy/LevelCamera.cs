using System;
using Godot;

namespace FEZEdit.Editors.Eddy;

public partial class LevelCamera : Camera3D
{
    public enum View
    {
        Perspective,
        Front,
        Right,
        Back,
        Left
    }

    private const string MoveLeft = "move_left";

    private const string MoveRight = "move_right";

    private const string MoveForward = "move_forward";

    private const string MoveBackward = "move_backward";

    public event Action<CollisionObject3D> ObjectPicked;

    public View CurrentView
    {
        get => _currentView;
        set => SetCurrentView(value);
    }

    public bool IsMoving { get; private set; }

    [ExportGroup("Perspective Properties")]
    
    [Export] private float _moveSpeed;

    [Export] private float _speedChange;

    [Export] private float _mouseSensitivity;

    [ExportGroup("Orthogonal Properties")]
    
    [Export] private float _maxDistance;

    [Export] private float _panSensitivity;

    [Export] private float _zoomChange;

    [Export] private float _minimumOrthogonalSize;

    [Export] private float _maximumOrthogonalSize;

    private Transform3D _perspectiveTransform;

    private View _currentView = View.Perspective;

    public override void _Ready()
    {
        _perspectiveTransform = Transform;
    }

    public override void _Process(double delta)
    {
        HandleMovement((float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } rmb:
                Input.MouseMode = rmb.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                IsMoving = rmb.Pressed;
                GetViewport().SetInputAsHandled();
                break;
            
            case InputEventMouseButton { Pressed: true } button:
                switch (button.ButtonIndex)
                {
                    case MouseButton.WheelUp:
                        HandleZoom(-1);
                        GetViewport().SetInputAsHandled();
                        break;
                    case MouseButton.WheelDown:
                        HandleZoom(+1);
                        GetViewport().SetInputAsHandled();
                        break;
                }
                break;

            case InputEventMouseMotion motion when Input.IsMouseButtonPressed(MouseButton.Right):
                HandleLook(motion.Relative);
                HandlePan(motion.Relative, (float)GetProcessDeltaTime());
                GetViewport().SetInputAsHandled();
                break;
            
            case InputEventMouseMotion motion when Input.IsMouseButtonPressed(MouseButton.Left):
                HandlePicking(motion.Position);
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    public Vector3.Axis GetViewAxis()
    {
        return _currentView switch
        {
            View.Perspective => Vector3.Axis.Y,
            View.Front => Vector3.Axis.Z,
            View.Right => Vector3.Axis.X,
            View.Back => Vector3.Axis.Z,
            View.Left => Vector3.Axis.X,
            _ => throw new ArgumentOutOfRangeException(nameof(_currentView), _currentView, null)
        };
    }

    private void SetCurrentView(View value)
    {
        if (_currentView == value)
        {
            return;
        }

        if (_currentView == View.Perspective)
        {
            _perspectiveTransform = Transform;
            Size = Mathf.Ceil(Position.Length());
        }

        _currentView = value;
        switch (_currentView)
        {
            case View.Perspective:
                Projection = ProjectionType.Perspective;
                Transform = _perspectiveTransform;
                break;

            case View.Front:
                SetOrthogonalView(0);
                break;

            case View.Right:
                SetOrthogonalView(-Mathf.Pi / 2f);
                break;

            case View.Back:
                SetOrthogonalView(Mathf.Pi);
                break;

            case View.Left:
                SetOrthogonalView(Mathf.Pi / 2f);
                break;
        }
    }

    private void SetOrthogonalView(float angles)
    {
        Rotation = new Vector3(0, angles, 0);
        Position = Transform.Basis.Z * _maxDistance;
        Projection = ProjectionType.Orthogonal;
    }

    private void HandleMovement(float delta)
    {
        if (Projection == ProjectionType.Perspective && Input.IsMouseButtonPressed(MouseButton.Right))
        {
            var movement = Input.GetVector(MoveLeft, MoveRight, MoveForward, MoveBackward);
            var direction = (Basis * new Vector3(movement.X, 0, movement.Y)).Normalized();
            Position += direction * _moveSpeed * delta;
        }
    }

    private void HandleLook(Vector2 relativeMotion)
    {
        if (Projection == ProjectionType.Perspective)
        {
            var pitch = Mathf.Clamp(relativeMotion.Y * _mouseSensitivity, -90, 90);
            RotateY(Mathf.DegToRad(-relativeMotion.X * _mouseSensitivity));
            RotateObjectLocal(Vector3.Right, Mathf.DegToRad(-pitch));
        }
    }

    private void HandlePan(Vector2 relativeMotion, float delta)
    {
        if (Projection == ProjectionType.Orthogonal)
        {
            var panAmount = relativeMotion * _panSensitivity * Size;
            Position -= Transform.Basis.X * panAmount.X * delta;
            Position += Transform.Basis.Y * panAmount.Y * delta;
        }
    }

    private void HandleZoom(int direction)
    {
        if (Projection == ProjectionType.Orthogonal)
        {
            Size += _zoomChange * direction;
            Size = Mathf.Clamp(Size, _minimumOrthogonalSize, _maximumOrthogonalSize);
        }
    }

    private void HandlePicking(Vector2 position)
    {
        var direction = ProjectRayNormal(position);
        var from = ProjectRayOrigin(position);
        var to = from + direction * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;

        var dss = GetWorld3D().DirectSpaceState;
        var result = dss.IntersectRay(query);

        if (result.Count < 1)
        {
            ObjectPicked?.Invoke(null);
            return;
        }
        
        var @object = (CollisionObject3D)result["collider"];
        ObjectPicked?.Invoke(@object);
    }
}