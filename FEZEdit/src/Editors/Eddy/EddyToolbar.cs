using System;
using Godot;

namespace FEZEdit.Editors.Eddy;

public partial class EddyToolbar : Control
{
    public enum Tool
    {
        Select,
        Translate,
        Rotate,
        Scale,
        Paint,
        Pick,
        Erase,
    }
    
    public enum Action
    {
        Fill,
        Delete,
        Group,
        Ungroup,
    }
    
    public event Action<Tool> ToolChanged;

    public event Action<Action> ActionCalled;

    public event Action<float, Vector3.Axis> GridChanged;

    public event Action<EddyCamera.View> ViewChanged;

    public event Action<TimeSpan> TimeChanged;
    
    public event Action<bool> TimeProcessChanged;
    
    private Button _selectButton;
    
    private Button _translateButton;
    
    private Button _rotateButton;
    
    private Button _scaleButton;
    
    private Button _paintMode;
    
    private Button _pickMode;
    
    private Button _eraseMode;
    
    private Button _fillAction;
    
    private Button _deleteAction;
    
    private Button _groupAction;
    
    private Button _ungroupAction;
    
    private SpinBox _levelBox;
    
    private OptionButton _levelOptionButton;
    
    private OptionButton _viewOptionButton;
    
    private LineEdit _timeEdit;

    private Control _selectionButtons;

    public override void _Ready()
    {
        _selectButton = GetNode<Button>("%SelectButton");
        _selectButton.Toggled += pressed => ChangeTool(pressed, Tool.Select);

        _translateButton = GetNode<Button>("%TranslateButton");
        _translateButton.Toggled += pressed => ChangeTool(pressed, Tool.Translate);

        _rotateButton = GetNode<Button>("%RotateButton");
        _rotateButton.Toggled += pressed => ChangeTool(pressed, Tool.Rotate);

        _scaleButton = GetNode<Button>("%ScaleButton");
        _scaleButton.Toggled += pressed => ChangeTool(pressed, Tool.Scale);

        _paintMode = GetNode<Button>("%PaintMode");
        _paintMode.Toggled += pressed => ChangeTool(pressed, Tool.Paint);

        _pickMode = GetNode<Button>("%PickMode");
        _pickMode.Toggled += pressed => ChangeTool(pressed, Tool.Pick);

        _eraseMode = GetNode<Button>("%EraseMode");
        _eraseMode.Toggled += pressed => ChangeTool(pressed, Tool.Erase);

        _fillAction = GetNode<Button>("%FillAction");
        _fillAction.Pressed += () => ActionCalled?.Invoke(Action.Fill);

        _deleteAction = GetNode<Button>("%DeleteAction");
        _deleteAction.Pressed += () => ActionCalled?.Invoke(Action.Delete);

        _groupAction = GetNode<Button>("%GroupAction");
        _groupAction.Pressed += () => ActionCalled?.Invoke(Action.Group);

        _ungroupAction = GetNode<Button>("%UngroupAction");
        _ungroupAction.Pressed += () => ActionCalled?.Invoke(Action.Ungroup);

        _levelBox = GetNode<SpinBox>("%LevelBox");
        _levelBox.ValueChanged +=
            value => GridChanged?.Invoke((float)value, (Vector3.Axis)_levelOptionButton.Selected);

        _levelOptionButton = GetNode<OptionButton>("%LevelOptionButton");
        _levelOptionButton.ItemSelected += axis => GridChanged?.Invoke((float)_levelBox.Value, (Vector3.Axis)axis);

        _viewOptionButton = GetNode<OptionButton>("%ViewOptionButton");
        _viewOptionButton.ItemSelected += view => ViewChanged?.Invoke((EddyCamera.View)view);

        _timeEdit = GetNode<LineEdit>("%TimeEdit");
        _timeEdit.FocusEntered += () => TimeProcessChanged?.Invoke(false);
        _timeEdit.FocusExited += () => TimeProcessChanged?.Invoke(true);
        _timeEdit.TextChanged += time =>
        {
            if (TimeSpan.TryParse(time, out var span))
            {
                TimeChanged?.Invoke(span);
            }
        };
        
        _selectionButtons = GetNode<Control>("%SelectionButtons");
        _selectionButtons.Hide();

        return;

        void ChangeTool(bool pressed, Tool tool)
        {
            if (pressed)
            {
                ToolChanged?.Invoke(tool);
            }
        }
    }

    public void SetTime(TimeSpan time)
    {
        _timeEdit.Text = time.ToString(@"hh\:mm");
    }

    public void SetGridLevel(float level, Vector3.Axis axis)
    {
        _levelBox.SetValueNoSignal(level);
        _levelOptionButton.Selected = (int)axis;
    }
     
    public void SetButtonsSelectionVisibility(bool visible)
    {
        _selectionButtons.Visible = visible;
    }

    public void SetToolButton(Tool tool)
    {
        var button = tool switch
        {
            Tool.Translate => _translateButton,
            Tool.Rotate => _rotateButton,
            Tool.Scale => _scaleButton,
            Tool.Paint => _paintMode,
            Tool.Pick => _pickMode,
            Tool.Erase => _eraseMode,
            _ => _selectButton
        };
        button.ButtonPressed = true;
    }
}