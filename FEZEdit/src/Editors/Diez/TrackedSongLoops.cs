using System;
using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.TrackedSong;
using Godot;

namespace FEZEdit.Editors.Diez;

public partial class TrackedSongLoops : Control
{
    public event Action<Loop> LoopSelected;

    public string SongName { get; set; }

    public List<Loop> LoopsList { get; set; }

    public bool Disabled
    {
        set
        {
            _up.Disabled = value;
            _down.Disabled = value;
            _add.Disabled = value;
            _remove.Disabled = value;
        }
    }

    private Button _up;

    private Button _down;

    private Button _add;

    private Button _remove;

    private Tree _loopsTree;

    private TreeItem _root;

    public void Initialize()
    {
        InitializeUp();
        InitializeDown();
        InitializeAdd();
        InitializeRemove();
        InitializeLoopsTree();
        UpdateTree();
    }

    public void UpdateTree()
    {
        _loopsTree.Clear();
        _root = _loopsTree.CreateItem();
        foreach (var loop in LoopsList)
        {
            var loopItem = _loopsTree.CreateItem(_root);
            loopItem.SetText(0, loop.Name);
        }
    }

    private void InitializeUp()
    {
        _up = GetNode<Button>("%UpButton");
        _up.Pressed += () =>
        {
            var item = _loopsTree.GetSelected();
            if (item == null) return;
            
            var index = item.GetIndex();
            if (index == 0) return; // Already at top

            var previousIndex = index - 1;
            MoveLoopTo(index, previousIndex);
        };
    }

    private void InitializeDown()
    {
        _down = GetNode<Button>("%DownButton");
        _down.Pressed += () =>
        {
            var item = _loopsTree.GetSelected();
            if (item == null) return;
            
            var index = item.GetIndex();
            if (index == LoopsList.Count - 1) return; // Already at bottom

            var nextIndex = index + 1;
            MoveLoopTo(index, nextIndex);
        };
    }

    private void InitializeAdd()
    {
        _add = GetNode<Button>("%AddButton");
        _add.Pressed += () =>
        {
            var newLoop = new Loop { Name = $"{SongName} ^ Untitled" };
            var addIndex = LoopsList.Count;
            LoopsList.Add(newLoop);
            UpdateTree();
            SelectLoop(addIndex);
            LoopSelected?.Invoke(newLoop);
        };
    }

    private void InitializeRemove()
    {
        _remove = GetNode<Button>("%RemoveButton");
        _remove.Pressed += () =>
        {
            var item = _loopsTree.GetSelected();
            if (item == null) return;
            var index = item.GetIndex();
            LoopsList.RemoveAt(index);
            UpdateTree();
            LoopSelected?.Invoke(null);
        };
    }

    private void InitializeLoopsTree()
    {
        _loopsTree = GetNode<Tree>("%LoopsTree");
        _loopsTree.ItemSelected += () =>
        {
            var index = _loopsTree.GetSelected().GetIndex();
            LoopSelected?.Invoke(LoopsList[index]);
        };
    }

    private void MoveLoopTo(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0 ||
            fromIndex >= LoopsList.Count || toIndex >= LoopsList.Count)
        {
            return;
        }

        var item = LoopsList[fromIndex];
        LoopsList.RemoveAt(fromIndex);
        LoopsList.Insert(toIndex, item);

        UpdateTree();
        SelectLoop(toIndex);
    }

    private void SelectLoop(int index)
    {
        if (index >= 0 && index < _root.GetChildCount())
        {
            var item = _root.GetChild(index);
            item.Select(0);
        }
    }
}