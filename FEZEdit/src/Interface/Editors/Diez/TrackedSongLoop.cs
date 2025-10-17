using System;
using System.IO;
using System.Text;
using FEZEdit.Core;
using FEZRepacker.Core.Definitions.Game.TrackedSong;
using Godot;

namespace FEZEdit.Interface.Editors.Diez;

public partial class TrackedSongLoop : Control
{
    public event Action NameChanged;
    
    public string SongName { get; set; }

    public Loop Loop { get; set; }

    public bool Disabled
    {
        set
        {
            _nameChange.Disabled = value;
            _triggerFrom.Editable = !value;
            _triggerTo.Editable = !value;
            _fractionalTime.Disabled = value;
            _timesFrom.Editable = !value;
            _timesTo.Editable = !value;
            _duration.Editable = !value;
            _delay.Editable = !value;
            _oneAtATime.Disabled = value;
            _cutOffTail.Disabled = value;
            _day.Disabled = value;
            _dusk.Disabled = value;
            _night.Disabled = value;
            _dawn.Disabled = value;
        }
    }

    private LineEdit _name;

    private Button _nameChange;

    private SpinBox _triggerFrom;

    private SpinBox _triggerTo;

    private CheckBox _fractionalTime;

    private SpinBox _timesFrom;

    private SpinBox _timesTo;

    private SpinBox _duration;

    private SpinBox _delay;

    private CheckBox _oneAtATime;

    private CheckBox _cutOffTail;

    private CheckBox _day;

    private CheckBox _dusk;

    private CheckBox _night;

    private CheckBox _dawn;

    private FileDialog _loopSelectDialog;

    public void Initialize()
    {
        InitializeName();
        InitializeTrigger();
        InitializeFractionalTime();
        InitializeTimes();
        InitializeDuration();
        InitializeDelay();
        InitializeOneAtATime();
        InitializeCutOffTail();
        InitializeDayPhase();
        InitializeDialogs();
    }

    private void InitializeName()
    {
        _name = GetNode<LineEdit>("%Name");
        _name.Text = Loop.Name;
        _name.TextChanged += name =>
        {
            Loop.Name = name;
            NameChanged?.Invoke();
        };
        
        _nameChange = GetNode<Button>("%NameChange");
        _nameChange.Pressed += () =>
        {
            _loopSelectDialog.CurrentDir = Settings.CurrentFolder;
            _loopSelectDialog.PopupCentered();
        };
    }

    private void InitializeTrigger()
    {
        _triggerFrom = GetNode<SpinBox>("%TriggerFrom");
        _triggerFrom.Value = Loop.TriggerFrom;
        _triggerFrom.ValueChanged += triggerFrom => Loop.TriggerFrom = (int)triggerFrom;

        _triggerTo = GetNode<SpinBox>("%TriggerTo");
        _triggerTo.Value = Loop.TriggerTo;
        _triggerTo.ValueChanged += triggerTo => Loop.TriggerTo = (int)triggerTo;
    }

    private void InitializeFractionalTime()
    {
        _fractionalTime = GetNode<CheckBox>("%FractionalTime");
        _fractionalTime.ButtonPressed = Loop.FractionalTime;
        _fractionalTime.Toggled += fractionalTime => Loop.FractionalTime = fractionalTime;
    }

    private void InitializeTimes()
    {
        _timesFrom = GetNode<SpinBox>("%TimesFrom");
        _timesFrom.Value = Loop.LoopTimesFrom;
        _timesFrom.ValueChanged += timesFrom => Loop.LoopTimesFrom = (int)timesFrom;

        _timesTo = GetNode<SpinBox>("%TimesTo");
        _timesTo.Value = Loop.LoopTimesTo;
        _timesTo.ValueChanged += timesTo => Loop.LoopTimesTo = (int)timesTo;
    }

    private void InitializeDuration()
    {
        _duration = GetNode<SpinBox>("%Duration");
        _duration.Value = Loop.Duration;
        _duration.ValueChanged += duration => Loop.Duration = (int)duration;
    }

    private void InitializeDelay()
    {
        _delay = GetNode<SpinBox>("%Delay");
        _delay.Value = Loop.Delay;
        _delay.ValueChanged += delay => Loop.Delay = (int)delay;
    }

    private void InitializeOneAtATime()
    {
        _oneAtATime = GetNode<CheckBox>("%OneAtATime");
        _oneAtATime.ButtonPressed = Loop.OneAtATime;
        _oneAtATime.Toggled += oneAtATime => Loop.OneAtATime = oneAtATime;
    }

    private void InitializeCutOffTail()
    {
        _cutOffTail = GetNode<CheckBox>("%CutOffTail");
        _cutOffTail.ButtonPressed = Loop.CutOffTail;
        _cutOffTail.Toggled += cutOff => Loop.CutOffTail = cutOff;
    }

    private void InitializeDayPhase()
    {
        _day = GetNode<CheckBox>("%Day");
        _day.ButtonPressed = Loop.Day;
        _day.Toggled += day => Loop.Day = day;

        _dusk = GetNode<CheckBox>("%Dusk");
        _dusk.ButtonPressed = Loop.Dusk;
        _dusk.Toggled += dusk => Loop.Dusk = dusk;

        _night = GetNode<CheckBox>("%Night");
        _night.ButtonPressed = Loop.Night;
        _night.Toggled += night => Loop.Night = night;

        _dawn = GetNode<CheckBox>("%Dawn");
        _dawn.ButtonPressed = Loop.Dawn;
        _dawn.Toggled += dawn => Loop.Dawn = dawn;
    }

    private void InitializeDialogs()
    {
        _loopSelectDialog = GetNode<FileDialog>("%LoopSelectDialog");
        _loopSelectDialog.FileSelected += file =>
        {
            var name = _name.Text = Path.GetFileNameWithoutExtension(file); 
            var originalName = new StringBuilder($"{SongName} ^ ");
            var capitalizeNext = true;
            
            foreach (char c in name)
            {
                if (c == '_')
                {
                    originalName.Append(c);
                    capitalizeNext = true;
                }
                else if (capitalizeNext && char.IsLetter(c))
                {
                    originalName.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    originalName.Append(c);
                }
            }

            _name.Text = originalName.ToString();
            Loop.Name = _name.Text;
            NameChanged?.Invoke();
        };
    }
}