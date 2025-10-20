using System;
using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.TrackedSong;
using Godot;

namespace FEZEdit.Editors.Diez;

public partial class OverlayLoopList : Control
{
    public event Action<int> LoopSelected;

    public event Action LoopCreated;
    
    public event Action<int> LoopRemoved;
    
    public event Action<int, int> LoopMoved;

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

    private Tree _loopTree;

    private TreeItem _treeRoot;

    public override void _Ready()
    {
        InitializeUp();
        InitializeDown();
        InitializeAdd();
        InitializeRemove();
        InitializeLoopsTree();
    }

    public void InspectList(IEnumerable<Loop> loops)
    {
        _loopTree.Clear();
        _treeRoot = _loopTree.CreateItem();
        foreach (var loop in loops)
        {
            var loopItem = _loopTree.CreateItem(_treeRoot);
            loopItem.SetText(0, loop.Name);
        }
    }
    
    public void SelectLoop(int index)
    {
        if (index >= 0 && index < _treeRoot.GetChildCount())
        {
            var item = _treeRoot.GetChild(index);
            item.Select(0);
        }
    }

    private void InitializeUp()
    {
        _up = GetNode<Button>("%UpButton");
        _up.Pressed += () =>
        {
            var item = _loopTree.GetSelected();
            if (item == null) return;
            
            var index = item.GetIndex();
            if (index == 0) return; // Already at top

            var previousIndex = index - 1;
            LoopMoved?.Invoke(index, previousIndex);
        };
    }

    private void InitializeDown()
    {
        _down = GetNode<Button>("%DownButton");
        _down.Pressed += () =>
        {
            var item = _loopTree.GetSelected();
            if (item == null) return;
            
            var index = item.GetIndex();
            var total = _loopTree.GetChildCount();
            if (index == total - 1) return; // Already at bottom

            var nextIndex = index + 1;
            LoopMoved?.Invoke(index, nextIndex);
        };
    }

    private void InitializeAdd()
    {
        _add = GetNode<Button>("%AddButton");
        _add.Pressed += () =>
        {
            LoopCreated?.Invoke();
        };
    }

    private void InitializeRemove()
    {
        _remove = GetNode<Button>("%RemoveButton");
        _remove.Pressed += () =>
        {
            var item = _loopTree.GetSelected();
            if (item == null) return;
            var index = item.GetIndex();
            LoopRemoved?.Invoke(index);
        };
    }

    private void InitializeLoopsTree()
    {
        _loopTree = GetNode<Tree>("%LoopTree");
        _loopTree.SetColumnExpand(0, true);
        _loopTree.ItemSelected += () =>
        {
            var index = _loopTree.GetSelected().GetIndex();
            LoopSelected?.Invoke(index);
        };
        
        _treeRoot = _loopTree.CreateItem();
    }
}