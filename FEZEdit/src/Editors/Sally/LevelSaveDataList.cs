using System;
using Godot;

namespace FEZEdit.Editors.Sally;

public partial class LevelSaveDataList : Control
{
    private const string DefaultLevel = "UNTITLED";
    
    public event Action<string> LevelSelected;
    
    public event Action<string> LevelAdded;

    public event Action<int, string> LevelRenamed;
    
    public event Action<string> LevelRemoved;

    public bool Disabled
    {
        set
        {
            foreach (var item in _levelTreeItem.GetChildren())
            {
                item.SetEditable(0, value);
            }
        }
    }
    
    private Tree _levelTree;

    private TreeItem _levelTreeItem;
    
    private Button _addButton;

    private Button _renameButton;
    
    private Button _removeButton;

    public override void _Ready()
    {
        InitializeTree();
        InitializeButtons();
    }

    public void AddLevel(string level)
    {
        var item = _levelTreeItem.CreateChild();
        item.SetEditable(0, false);
        item.SetText(0, level);
    }

    public void ClearLevels()
    {
        foreach (var item in _levelTreeItem.GetChildren())
        {
            item.Free();
        }
    }
    
    private void InitializeTree()
    {
        _levelTree = GetNode<Tree>("%LevelTree");
        _levelTree.SetColumnExpand(0, true);
        _levelTree.ItemActivated += SelectLevel;
        
        _levelTreeItem = _levelTree.CreateItem();
        _levelTreeItem.SetEditable(0, false);
    }

    private void InitializeButtons()
    {
        _addButton = GetNode<Button>("%AddButton");
        _addButton.Pressed += AddNewLevel;
        
        _renameButton = GetNode<Button>("%RenameButton");
        _renameButton.Pressed += RenameLevel;
        
        _removeButton = GetNode<Button>("%RemoveButton");
        _removeButton.Pressed += RemoveLevel;
    }

    private void AddNewLevel()
    {
        var text = $"{DefaultLevel}_{_levelTreeItem.GetChildCount()}";
        LevelAdded?.Invoke(text);
    }
    
    private void RemoveLevel()
    {
        var clickedItem = _levelTree.GetSelected();
        var levelName = clickedItem?.GetText(0);
        if (!string.IsNullOrEmpty(levelName))
        {
            LevelRemoved?.Invoke(levelName);
            LevelSelected?.Invoke(string.Empty);
        }
    }
    
    private void SelectLevel()
    {
        var clickedItem = _levelTree.GetSelected();
        var levelName = clickedItem?.GetText(0);
        if (!string.IsNullOrEmpty(levelName))
        {
            LevelSelected?.Invoke(levelName);
        }
    }
    
    private void RenameLevel()
    {
        if (_levelTree.EditSelected(true))
        {
            _levelTree.ItemEdited += RenameLevelInternal;
        }
    }

    private void RenameLevelInternal()
    {
        var clickedItem = _levelTree.GetEdited();
        var levelName = clickedItem?.GetText(0);
        var levelIndex = clickedItem?.GetIndex() ?? -1;
        if (!string.IsNullOrEmpty(levelName))
        {
            _levelTree.ItemEdited -= RenameLevelInternal;
            LevelRenamed?.Invoke(levelIndex, levelName);
        }
    }
}