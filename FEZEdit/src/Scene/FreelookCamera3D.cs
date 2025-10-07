using FEZEdit.Core;
using Godot;

namespace FEZEdit.Scene;

public partial class FreelookCamera3D : Camera3D
{
    private const float MoveSpeed = 4.0f;

    private const float MinMoveSpeed = 0.1f;

    private const float MaxMoveSpeed = 16.0f;

    private const float SpeedChange = 0.1f;

    private const float MouseSensitivity = 0.25f;

    public bool IsMoving { get; set; }

    private float _moveSpeed = MoveSpeed;

    public override void _Process(double delta)
    {
        var input = InputActions.Movement;
        var move = Basis * new Vector3(input.X, 0, input.Y);
        Position += move.Normalized() * _moveSpeed * (float) delta;
        IsMoving = input.Length() > 0.01f || Input.MouseMode == Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton button:
                {
                    switch (button.ButtonIndex)
                    {
                        case MouseButton.Right:
                            Input.MouseMode = button.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                            break;
                        
                        case MouseButton.WheelUp:
                            _moveSpeed = Mathf.Clamp(_moveSpeed + SpeedChange, MinMoveSpeed, MaxMoveSpeed);
                            break;
                        
                        case MouseButton.WheelDown:
                            _moveSpeed = Mathf.Clamp(_moveSpeed - SpeedChange, MinMoveSpeed, MaxMoveSpeed);
                            break;
                    }
                    break;
                }

            case InputEventMouseMotion motion when Input.MouseMode == Input.MouseModeEnum.Captured:
                {
                    var pitch = Mathf.Clamp(motion.Relative.Y * MouseSensitivity, -90, 90);
                    RotateY(Mathf.DegToRad(-motion.Relative.X * MouseSensitivity));
                    RotateObjectLocal(Vector3.Right, Mathf.DegToRad(-pitch));
                    break;
                }
        }
    }
}