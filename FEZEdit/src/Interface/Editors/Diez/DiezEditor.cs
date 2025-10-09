using System.Linq;
using FEZRepacker.Core.Definitions.Game.TrackedSong;
using Godot;

namespace FEZEdit.Interface.Editors.Diez;

public partial class DiezEditor: TypedEditor<TrackedSong>
{
    [Export] private PackedScene _selectedLoopScene;
    
    public override TrackedSong TypedValue { get; set; }
    
    public override bool Disabled
    {
        set
        {
            _songProperties.Disabled = value;
            _loopsList.Disabled = value;
            _selectedLoop.Disabled = value;
        }
    }

    private TrackedSongProperties _songProperties;

    private TrackedSongLoops _loopsList;

    private Container _selectedLoopContainer;

    private TrackedSongLoop _selectedLoop;

    public override void _EnterTree()
    {
        InitializeSongProperties();
        InitializeLoopsList();
        InitializeSelectedLoopContainer();
    }

    private void InitializeSongProperties()
    {
        _songProperties = GetNode<TrackedSongProperties>("%TrackedSongProperties");
        _songProperties.TrackedSong = TypedValue;
        _songProperties.Loader = Loader;
    }

    private void InitializeLoopsList()
    {
        _loopsList = GetNode<TrackedSongLoops>("%TrackedSongLoops");
        _loopsList.SongName = TypedValue.Name;
        _loopsList.LoopsList = TypedValue.Loops.ToList();
        _loopsList.LoopSelected += loop => Callable.From(() => InitializeSelectedLoop(loop)).CallDeferred();
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
            _selectedLoop.SongName = TypedValue.Name;
            _selectedLoop.Loop = loop;
            _selectedLoop.NameChanged += _loopsList.UpdateTree;
            _selectedLoopContainer.AddChild(_selectedLoop, true);
        }
    }
}