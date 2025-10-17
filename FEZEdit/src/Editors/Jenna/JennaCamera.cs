using System;
using Godot;

namespace FEZEdit.Editors.Jenna;

public partial class JennaCamera : Camera3D
{
    public event Action<object> ResultClickedLeft;
    
    public event Action<object> ResultClickedRight;

    [Export] private Node3D _target;

    [Export] private float _rotateSensitivity;

    [Export(PropertyHint.Range, "-90,90,1,radians_as_degrees")] private float _minPitchAngle;

    [Export(PropertyHint.Range, "-90,90,1,radians_as_degrees")] private float _maxPitchAngle;

    [Export] private Vector2 _defaultAngles;

    [Export(PropertyHint.Range, "0.1,3.0,0.1,suffix:seconds")] private Godot.Collections.Array<float> _zoomLevels;

    [Export] private float _zoomDuration;

    [Export] private float _transitionSpeed;

    [Export] private float _panSensitivity;

    [Export(PropertyHint.Range, "0.1,3.0,0.1,suffix:seconds")] private float _panResetDuration;

    private bool _rotating;

    private bool _panning;

    private bool _followingTarget = true;

    private Node3D _pivot;

    private Node3D _pan;

    private Vector2 _currentRotation = Vector2.Zero;

    private Tween _currentTween;

    private Tween _panTween;

    private int _currentZoomIndex;

    private float _orbitDistance = 200f;

    private MaterializerProxy _proxy;

    public override void _Ready()
    {
        Projection = ProjectionType.Orthogonal;
        Callable.From(PostReady).CallDeferred();
    }

    public override void _Process(double delta)
    {
        if (_target != null && IsInstanceValid(_target) && _pan != null)
        {
            var isTweenRunning = _currentTween != null && _currentTween.IsValid();
            if (!isTweenRunning && !_panning && _followingTarget)
            {
                _pan.GlobalPosition = _target.GlobalPosition;
            }

            LookAt(_pivot.GlobalPosition);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left }:
                if (_proxy != null)
                {
                    ResultClickedLeft?.Invoke(_proxy.Object);
                    SetTarget(_proxy.GetParentNode3D());
                }
                else
                {
                    ResultClickedLeft?.Invoke(null);
                }
                GetViewport().SetInputAsHandled();
                return;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton:
                if (_proxy != null)
                {
                    ResultClickedRight?.Invoke(_proxy.Object);
                    Input.SetDefaultCursorShape();
                    _proxy = null;  // Cancels selection on LMB press
                }
                else
                {
                    _rotating = mouseButton.Pressed;
                    ResultClickedRight?.Invoke(null);
                    Input.SetDefaultCursorShape(_panning ? Input.CursorShape.Drag : Input.CursorShape.Arrow);
                }

                GetViewport().SetInputAsHandled();
                return;

            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } mouseButton2:
                _panning = mouseButton2.Pressed;
                if (_panning)
                {
                    _followingTarget = false;
                }
                Input.SetDefaultCursorShape(_panning ? Input.CursorShape.Drag : Input.CursorShape.Arrow);
                GetViewport().SetInputAsHandled();
                return;

            case InputEventMouseButton { Pressed: true } mouseButton3:
                switch (mouseButton3.ButtonIndex)
                {
                    case MouseButton.WheelUp:
                        CycleZoom(+1);
                        break;
                    case MouseButton.WheelDown:
                        CycleZoom(-1);
                        break;
                }

                GetViewport().SetInputAsHandled();
                return;

            case InputEventMouseMotion mouseMotion when _rotating:
                HandleRotation(mouseMotion);
                GetViewport().SetInputAsHandled();
                return;

            case InputEventMouseMotion mouseMotion when _panning:
                HandlePanning(mouseMotion);
                GetViewport().SetInputAsHandled();
                return;

            case InputEventMouseMotion mouseMotion:
                _proxy = PerformRayCast(mouseMotion.Position);
                Input.SetDefaultCursorShape(_proxy != null ? Input.CursorShape.PointingHand : Input.CursorShape.Arrow);
                GetViewport().SetInputAsHandled();
                return;
        }
    }

    public void SetTarget(Node3D newTarget, bool smoothTransition = true)
    {
        if (_pan == null || newTarget == null)
        {
            return;
        }

        if (_currentTween != null && _currentTween.IsValid())
        {
            _currentTween.Kill();
        }

        _target = newTarget;
        _followingTarget = true;
        
        if (smoothTransition)
        {
            _currentTween = CreateTween();
            _currentTween.TweenProperty(_pan, "global_position", newTarget.GlobalPosition, 1.0f / _transitionSpeed)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
        else
        {
            _pan.GlobalPosition = newTarget.GlobalPosition;
        }
    }

    private void PostReady()
    {
        CreateCameraRig();
        _currentZoomIndex = GetClosestZoomIndex(Size);
        Size = _zoomLevels[_currentZoomIndex];
        _currentRotation = _defaultAngles;
        UpdateCameraPosition();
    }

    private int GetClosestZoomIndex(float currentSize)
    {
        var closestIndex = 0;
        var minDiff = Mathf.Abs(currentSize - _zoomLevels[0]);
        for (int i = 1; i < _zoomLevels.Count; i++)
        {
            var diff = Mathf.Abs(currentSize - _zoomLevels[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void CreateCameraRig()
    {
        var originalGlobalTransform = GlobalTransform;
        _pivot = new Node3D { Name = "Pivot" };
        _pan = new Node3D { Name = "Pan" };

        var parent = GetParent();
        if (parent != null)
        {
            parent.RemoveChild(this);
            parent.AddChild(_pan);
            _pan.AddChild(_pivot);
            _pivot.AddChild(this);
            
            _pan.GlobalPosition = _target?.GlobalPosition ?? originalGlobalTransform.Origin;
            _pivot.Position = Vector3.Zero;
            Position = new Vector3(0, 0, _orbitDistance);
            LookAt(_pivot.GlobalPosition);
        }
    }

    private void CycleZoom(int direction)
    {
        if (_currentTween != null && _currentTween.IsValid())
        {
            return;
        }

        _currentZoomIndex = Mathf.Clamp(_currentZoomIndex + direction, 0, _zoomLevels.Count - 1);
        float targetZoom = _zoomLevels[_currentZoomIndex];

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "size", targetZoom, _zoomDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void HandleRotation(InputEventMouseMotion @event)
    {
        if (_pivot == null)
        {
            return;
        }

        _currentRotation.X -= @event.Relative.X * _rotateSensitivity;
        _currentRotation.Y -= @event.Relative.Y * _rotateSensitivity;
        _currentRotation.Y = Mathf.Clamp(_currentRotation.Y, _minPitchAngle, _maxPitchAngle);

        UpdateCameraPosition();
    }

    private void HandlePanning(InputEventMouseMotion @event)
    {
        if (_pan == null)
        {
            return;
        }

        if (_panTween != null && _panTween.IsValid())
        {
            _panTween.Kill();
        }

        var scaledSpeed = _panSensitivity * Size;
        _pan.GlobalPosition -= GlobalTransform.Basis.X * @event.Relative.X * scaledSpeed;
        _pan.GlobalPosition += GlobalTransform.Basis.Y * @event.Relative.Y * scaledSpeed;
    }

    private MaterializerProxy PerformRayCast(Vector2 position)
    {
        var direction = ProjectRayNormal(position);
        var from = ProjectRayOrigin(position);
        var to = from + direction * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;

        var dss = GetWorld3D().DirectSpaceState;
        var result = dss.IntersectRay(query);

        if (result.Count > 0)
        {
            return (MaterializerProxy)result["collider"];
        }

        return null;
    }

    public void ResetPan()
    {
        if (_pan == null || _target == null)
        {
            return;
        }

        if (_panTween != null && _panTween.IsValid())
        {
            _panTween.Kill();
        }

        _panTween = CreateTween();
        _panTween.TweenProperty(_pan, "global_position", _target.GlobalPosition, _panResetDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _panTween.TweenCallback(Callable.From(() => _followingTarget = true));
    }

    private void UpdateCameraPosition()
    {
        if (_pivot == null)
        {
            return;
        }

        var position = new Vector3
        {
            X = _orbitDistance * Mathf.Sin(_currentRotation.X) * Mathf.Cos(_currentRotation.Y),
            Y = _orbitDistance * Mathf.Sin(_currentRotation.Y),
            Z = _orbitDistance * Mathf.Cos(_currentRotation.X) * Mathf.Cos(_currentRotation.Y)
        };

        Position = position;
        LookAt(_pivot.GlobalPosition);
    }
}