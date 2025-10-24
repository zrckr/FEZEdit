using Godot;

namespace FEZEdit.Extensions;

public static class InputKeyExtensions
{
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