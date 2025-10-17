using Godot;

namespace FEZEdit.Core;

public static class InputActions
{
    public const string MoveLeft = "move_left";

    public const string MoveRight = "move_right";

    public const string MoveForward = "move_forward";

    public const string MoveBackward = "move_backward";
    
    public const string GizmoUseLocalSpace = "gizmo_use_local_space";
    
    public const string GizmoAddTarget = "gizmo_add_target";
    
    public const string GizmoMoveMode = "gizmo_move_mode";
    
    public const string GizmoScaleMode = "gizmo_move_mode";
   
    public const string GizmoRotateMode = "gizmo_move_mode";
    
    public static Vector2 Movement => Input.GetVector(MoveLeft, MoveRight, MoveForward, MoveBackward);
    
    public static Shortcut WithShift(this Key key)
    {
        var shortcut = new Shortcut();
        shortcut.Events.Add(new InputEventKey() { Keycode = key, ShiftPressed = true });
        return shortcut;
    }
    
    public static Shortcut WithCtrl(this Key key)
    {
        var shortcut = new Shortcut();
        shortcut.Events.Add(new InputEventKey() { Keycode = key, CtrlPressed = true });
        return shortcut;
    }
    
    public static Shortcut WithCtrlShift(this Key key)
    {
        var shortcut = new Shortcut();
        shortcut.Events.Add(new InputEventKey() { Keycode = key, CtrlPressed = true, ShiftPressed = true });
        return shortcut;
    }
}