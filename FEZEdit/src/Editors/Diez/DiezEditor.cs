using System;
using FEZEdit.Main;
using FEZEdit.Singletons;
using FEZRepacker.Core.Definitions.Game.TrackedSong;
using Godot;

namespace FEZEdit.Editors.Diez;

public partial class DiezEditor : Editor
{
    private static readonly string[] SongProperties =
    [
        nameof(TrackedSong.Name),
        nameof(TrackedSong.Tempo),
        nameof(TrackedSong.TimeSignature),
        nameof(TrackedSong.AssembleChord),
        nameof(TrackedSong.Notes),
        nameof(TrackedSong.RandomOrdering),
        nameof(TrackedSong.CustomOrdering)
    ];

    private static readonly string[] LoopProperties =
    [
        nameof(Loop.Name),
        nameof(Loop.TriggerFrom),
        nameof(Loop.TriggerTo),
        nameof(Loop.FractionalTime),
        nameof(Loop.LoopTimesFrom),
        nameof(Loop.LoopTimesTo),
        nameof(Loop.Duration),
        nameof(Loop.Delay),
        nameof(Loop.OneAtATime),
        nameof(Loop.CutOffTail),
        nameof(Loop.Night),
        nameof(Loop.Day),
        nameof(Loop.Dusk),
        nameof(Loop.Dawn)
    ];

    private const string AssembleChordPath = @"collects\splitupcube\assemble_{0}";

    public override event Action ValueChanged;

    public override object Value
    {
        get => _trackedSong;
        set => _trackedSong = (TrackedSong)value;
    }

    public override bool Disabled
    {
        set
        {
            _songInspector.Disabled = value;
            _overlayLoopList.Disabled = value;
            _loopInspector.Disabled = value;
        }
    }

    public override UndoRedo UndoRedo { get; } = new();

    private TrackedSong _trackedSong;

    private Inspector _songInspector;

    private OverlayLoopList _overlayLoopList;

    private Inspector _loopInspector;

    private Button _assembleChordButton;
    
    private AudioStreamPlayer _assembleChordPlayer;

    public override void _Ready()
    {
        InitializeSongInspector();
        InitializeLoopsList();
        InitializeLoopInspector();
        InitializeAssembleChordPreview();
    }

    private void InitializeSongInspector()
    {
        _songInspector = GetNode<Inspector>("%SongInspector");
        _songInspector.UndoRedo = UndoRedo;
        _songInspector.TargetChanged += _ => ValueChanged?.Invoke();
        _songInspector.ClearProperties();
        foreach (string propertyName in SongProperties)
        {
            _songInspector.InspectProperty(_trackedSong, propertyName);
        }
    }

    private void InitializeLoopsList()
    {
        _overlayLoopList = GetNode<OverlayLoopList>("%TrackedSongLoops");
        _overlayLoopList.InspectList(_trackedSong.Loops);
        _overlayLoopList.LoopCreated += AddNewLoop;
        _overlayLoopList.LoopRemoved += RemoveLoop;
        _overlayLoopList.LoopMoved += MoveLoop;
        _overlayLoopList.LoopSelected += InspectLoop;
    }

    private void InitializeLoopInspector()
    {
        _loopInspector = GetNode<Inspector>("%LoopInspector");
        _loopInspector.UndoRedo = UndoRedo;
        InspectLoop(-1);
    }
    
    private void InitializeAssembleChordPreview()
    {
        _assembleChordButton = GetNode<Button>("%AssembleChordButton");
        _assembleChordButton.Pressed += PlayAssembleChord;
        
        _assembleChordPlayer =  GetNode<AudioStreamPlayer>("%AssembleChordPlayer");
        _assembleChordPlayer.Finished += () => _assembleChordButton.Disabled = false;
    }

    private void AddNewLoop()
    {
        var loop = new Loop { Name = $"{_trackedSong.Name} ^ Loop{_trackedSong.Loops.Count}" };
        var index = _trackedSong.Loops.Count;
        
        UndoRedo.CreateAction("Add new loop");
        UndoRedo.AddDoMethod(() =>
        {
            _trackedSong.Loops.Insert(index, loop);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(index);
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _trackedSong.Loops.RemoveAt(index);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(index);
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }

    private void RemoveLoop(int index)
    {
        var loop = _trackedSong.Loops[index];
        UndoRedo.CreateAction("Remove loop");
        UndoRedo.AddDoMethod(() =>
        {
            _trackedSong.Loops.RemoveAt(index);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(index);
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _trackedSong.Loops.Insert(index, loop);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(index);
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }

    private void MoveLoop(int oldIndex, int newIndex)
    {
        var item = _trackedSong.Loops[oldIndex];
        UndoRedo.CreateAction($"Move loop from {oldIndex} to {newIndex}");
        UndoRedo.AddDoMethod(() =>
        {
            _trackedSong.Loops.RemoveAt(oldIndex);
            _trackedSong.Loops.Insert(newIndex, item);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(newIndex);
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _trackedSong.Loops.RemoveAt(newIndex);
            _trackedSong.Loops.Insert(oldIndex, item);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(oldIndex);
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }

    private void InspectLoop(int index)
    {
        _loopInspector.ClearProperties();
        
        if (index == -1)
        {
            _loopInspector.TargetChanged -= LoopUpdate;
            _loopInspector.Visible = false;
            return;
        }

        _loopInspector.TargetChanged += LoopUpdate;
        _loopInspector.Visible = true;

        var loop = _trackedSong.Loops[index];
        foreach (var propertyName in LoopProperties)
        {
            _loopInspector.InspectProperty(loop, propertyName);
        }
    }

    private void LoopUpdate(object @object)
    {
        if (@object is Loop)
        {
            _overlayLoopList.InspectList(_trackedSong.Loops);
            ValueChanged?.Invoke();
        }
    }
    
    private void PlayAssembleChord()
    {
        var chord = _trackedSong.AssembleChord;
        var path = string.Format(AssembleChordPath, chord);
        _assembleChordPlayer.Stream = ContentLoader.LoadSound(path);
        _assembleChordPlayer.Play();
        _assembleChordButton.Disabled = true;
    }
}