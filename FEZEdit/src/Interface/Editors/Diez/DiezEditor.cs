using System;
using System.Linq;
using FEZRepacker.Core.Definitions.Game.TrackedSong;
using Godot;

namespace FEZEdit.Interface.Editors.Diez;

public partial class DiezEditor: Editor
{
    [Export] private PackedScene _selectedLoopScene;

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
            _songProperties.Disabled = value;
            _loopsList.Disabled = value;
            if (_selectedLoop != null) _selectedLoop.Disabled = value;
        }
    }

    private TrackedSong _trackedSong;

    private TrackedSongProperties _songProperties;

    private TrackedSongLoops _loopsList;

    private Container _selectedLoopContainer;

    private TrackedSongLoop _selectedLoop;

    public override void _Ready()
    {
        InitializeSongProperties();
        InitializeLoopsList();
        InitializeSelectedLoopContainer();
    }

    private void InitializeSongProperties()
    {
        _songProperties = GetNode<TrackedSongProperties>("%TrackedSongProperties");
        _songProperties.TrackedSong = _trackedSong;
        _songProperties.Loader = Loader;
        _songProperties.Initialize();
    }

    private void InitializeLoopsList()
    {
        _loopsList = GetNode<TrackedSongLoops>("%TrackedSongLoops");
        _loopsList.SongName = _trackedSong.Name;
        _loopsList.LoopsList = _trackedSong.Loops.ToList();
        _loopsList.LoopSelected += loop => Callable.From(() => InitializeSelectedLoop(loop)).CallDeferred();
        _loopsList.Initialize();
    }

    private void InitializeSelectedLoopContainer()
    {
        _selectedLoopContainer = GetNode<Container>("%TrackedSongLoopContainer");
    }

    private void InitializeSelectedLoop(Loop loop)
    {
        _selectedLoop?.QueueFree();
        if (loop != null)
        {
            _selectedLoop = _selectedLoopScene.Instantiate<TrackedSongLoop>();
            _selectedLoop.SongName = _trackedSong.Name;
            _selectedLoop.Loop = loop;
            _selectedLoop.NameChanged += _loopsList.UpdateTree;
            _selectedLoopContainer.AddChild(_selectedLoop, true);
            _selectedLoop.Initialize();
        }
    }
}