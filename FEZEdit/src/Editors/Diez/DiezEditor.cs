using System;
using FEZEdit.Main;
using FEZEdit.Content;
using FEZEdit.Memento;
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

    public override object Value
    {
        get => _trackedSong;
        set
        {
            _trackedSong = (TrackedSong)value;
            Memento = new MementoManager(_trackedSong);
        }
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

    private TrackedSong _trackedSong;

    private Inspector _songInspector;

    private OverlayLoopList _overlayLoopList;

    private Inspector _loopInspector;

    private Button _assembleChordButton;
    
    private AudioStreamPlayer _assembleChordPlayer;

    private int _overlayLoopIndex;

    public override void _Ready()
    {
        InitializeSongInspector();
        InitializeLoopsList();
        InitializeLoopInspector();
        InitializeAssembleChordPreview();
    }

    public override void _Refresh()
    {
        _songInspector.ClearProperties();
        foreach (string propertyName in SongProperties)
        {
            _songInspector.InspectProperty(_trackedSong, propertyName);
        }
        
        _overlayLoopList.InspectList(_trackedSong.Loops);
        if (_overlayLoopIndex > -1)
        {
            InspectLoop(_overlayLoopIndex);
        }
    }

    private void InitializeSongInspector()
    {
        _songInspector = GetNode<Inspector>("%SongInspector");
        _songInspector.Memento = Memento;
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
        _loopInspector.Memento = Memento;
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
        using (Memento.BeginScope("Add new loop"))
        {
            var loop = new Loop { Name = $"{_trackedSong.Name} ^ Loop{_trackedSong.Loops.Count}" };
            var index = _trackedSong.Loops.Count;
            _trackedSong.Loops.Insert(index, loop);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(index);
        }
    }

    private void RemoveLoop(int index)
    {
        using (Memento.BeginScope("Remove loop"))
        {
            _trackedSong.Loops.RemoveAt(index);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(index);
        }
    }

    private void MoveLoop(int oldIndex, int newIndex)
    {
        using (Memento.BeginScope($"Move loop from {oldIndex} to {newIndex}"))
        {
            var item = _trackedSong.Loops[oldIndex];
            _trackedSong.Loops.RemoveAt(oldIndex);
            _trackedSong.Loops.Insert(newIndex, item);
            _overlayLoopList.InspectList(_trackedSong.Loops);
            _overlayLoopList.SelectLoop(newIndex);
        }
    }

    private void InspectLoop(int index)
    {
        _loopInspector.ClearProperties();
        _overlayLoopIndex = index;
        
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