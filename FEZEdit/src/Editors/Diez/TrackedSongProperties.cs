using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Loaders;
using FEZRepacker.Core.Definitions.Game.TrackedSong;
using Godot;

namespace FEZEdit.Editors.Diez;

public partial class TrackedSongProperties : Control
{
    public TrackedSong TrackedSong { get; set; }

    public ILoader Loader { get; set; }

    public bool Disabled
    {
        set
        {
            _name.Editable = !value;
            _tempo.Editable = !value;
            _timeSignature.Editable = !value;
            _assembleChord.Disabled = value;
            _assembleChordPreview.Disabled = value;
            foreach (var button in _shardNotes) button.Disabled = value;
            _randomOrdering.Disabled = value;
            _customOrdering.Editable = !value;
        }
    }

    private LineEdit _name;

    private SpinBox _tempo;

    private SpinBox _timeSignature;

    private OptionButton _assembleChord;

    private Button _assembleChordPreview;

    private AudioStreamPlayer _assembleChordPlayer;

    private List<OptionButton> _shardNotes;

    private CheckBox _randomOrdering;

    private TextEdit _customOrdering;

    public void Initialize()
    {
        InitializeName();
        InitializeTempo();
        InitializeTimeSignature();
        InitializeAssembleChord();
        InitializeShardNotes();
        InitializeRandomOrdering();
        InitializeCustomOrdering();
    }

    private void InitializeName()
    {
        _name = GetNode<LineEdit>("%Name");
        _name.Text = TrackedSong.Name;
        _name.TextChanged += name => TrackedSong.Name = name;
    }

    private void InitializeTempo()
    {
        _tempo = GetNode<SpinBox>("%Tempo");
        _tempo.Value = TrackedSong.Tempo;
        _tempo.ValueChanged += tempo => TrackedSong.Tempo = (int)tempo;
    }

    private void InitializeTimeSignature()
    {
        _timeSignature = GetNode<SpinBox>("%TimeSignature");
        _timeSignature.Value = TrackedSong.TimeSignature;
        _timeSignature.ValueChanged += timeSignature => TrackedSong.TimeSignature = (int)timeSignature;
    }

    private void InitializeAssembleChord()
    {
        _assembleChord = GetNode<OptionButton>("%AssembleChord");
        foreach (var chord in Enum.GetValues<AssembleChords>())
        {
            _assembleChord.AddItem(chord.ToString());
            _assembleChord.SetItemMetadata((int)chord, $@"collects\splitupcube\assemble_{chord}".ToLower());
        }

        _assembleChord.Selected = (int)TrackedSong.AssembleChord;
        _assembleChord.ItemSelected += chord => TrackedSong.AssembleChord = (AssembleChords)chord;

        _assembleChordPreview = GetNode<Button>("%AssembleChordPreview");
        _assembleChordPreview.Pressed += PlayAssembleChord;

        _assembleChordPlayer = GetNode<AudioStreamPlayer>("%AssembleChordPlayer");
        _assembleChordPlayer.Finished += () => _assembleChordPreview.Disabled = false;
    }

    private void InitializeShardNotes()
    {
        _shardNotes = GetNode("%ShardNotes").GetChildren().OfType<OptionButton>().ToList();
        for (int i = 0; i < _shardNotes.Count; i++)
        {
            var shardNote = _shardNotes[i];
            foreach (var notes in Enum.GetValues<ShardNotes>())
            {
                shardNote.AddItem(notes.ToString(), (int)notes);
            }

            shardNote.Selected = (int)TrackedSong.Notes[i];
            shardNote.ItemSelected += note => TrackedSong.Notes[_shardNotes.IndexOf(shardNote)] = (ShardNotes)note;
        }
    }

    private void InitializeRandomOrdering()
    {
        _randomOrdering = GetNode<CheckBox>("%RandomOrdering");
        _randomOrdering.ButtonPressed = TrackedSong.RandomOrdering;
        _randomOrdering.Toggled += randomOrdering => TrackedSong.RandomOrdering = randomOrdering;
    }

    private void InitializeCustomOrdering()
    {
        _customOrdering = GetNode<TextEdit>("%CustomOrdering");
        _customOrdering.Text = string.Join(",", TrackedSong.CustomOrdering);
        _customOrdering.TextChanged += () => TrackedSong.CustomOrdering = GetCustomOrdering();
    }

    private void PlayAssembleChord()
    {
        var index = _assembleChord.GetSelected();
        var chordPath = _assembleChord.GetItemMetadata(index).AsString();
        _assembleChordPlayer.Stream = Loader.LoadSound(chordPath);
        _assembleChordPlayer.Play();
        _assembleChordPreview.Disabled = true;
    }

    private int[] GetCustomOrdering()
    {
        return _customOrdering.Text.Split(",")
            .Where(i => i.IsValidInt())
            .Select(int.Parse)
            .ToArray();
    }
}