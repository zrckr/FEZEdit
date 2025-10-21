using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Extensions;
using Godot;

namespace FEZEdit.Editors.Eddy;

public partial class InstanceTable : Control
{
    public event Action<int> InstanceAdded;

    public event Action<int> InstanceCloned;

    public event Action<int> InstanceDeleted;

    public event Action<int> InstanceSelected;

    public event Action Closed;

    public bool Disabled
    {
        set
        {
            _addButton.Disabled = value;
            _cloneButton.Disabled = value;
            _deleteButton.Disabled = value;
            _closeButton.Disabled = value;
        }
    }

    private Button _addButton;

    private Button _editButton;

    private Button _cloneButton;

    private Button _deleteButton;

    private Button _closeButton;

    private Tree _instanceTree;

    private TreeItem _instanceRoot;

    private readonly List<Column> _columns = [];

    public override void _Ready()
    {
        InitializeButtons();
        InitializeTree();
    }

    public int AddColumn(string title, bool expand = true, bool wrapText = true)
    {
        if (_columns.Count == 0)
        {
            _columns.Add(new Column("Id", false, true));
        }
        
        _columns.Add(new Column(title, expand, wrapText));
        RebuildColumns();
        return _columns.Count - 1;
    }

    public void ClearColumns()
    {
        _columns.Clear();
        RebuildColumns();
    }

    public void AddRow(int id, params object[] values)
    {
        if (values == null || values.Length + 1 != _columns.Count)
        {
            throw new ArgumentException($"Invalid number of values, expected {_columns.Count}");
        }

        var rowItem = _instanceRoot.CreateChild();
        rowItem.SetText(0, id.ToString());
        rowItem.SetMetadata(0, id);
        
        for (int i = 0; i < values.Length; i++)
        {
            rowItem.SetText(i + 1, values[i].Stringify());
            if (_columns[i + 1].WrapText)
            {
                rowItem.SetAutowrapMode(i + 1, TextServer.AutowrapMode.WordSmart);
            }
        }
    }

    public void SelectRow(int id)
    {
        foreach (var rowItem in _instanceRoot.GetChildren())
        {
            var rowId = rowItem.GetMetadata(0).AsInt32();
            if (rowId == id)
            {
                rowItem.Select(0);
                _instanceTree.ScrollToItem(rowItem);
                break;
            }
        }
    }

    public void RemoveRow(int id)
    {
        foreach (var rowItem in _instanceRoot.GetChildren())
        {
           var rowId = rowItem.GetMetadata(0).AsInt32();
           if (rowId == id)
           {
               rowItem.Free();
               break;
           }
        }
    }

    public void ClearRows()
    {
        _instanceTree.Clear();
        _instanceRoot = _instanceTree.CreateItem();
    }

    private void InitializeButtons()
    {
        _addButton = GetNode<Button>("%AddButton");
        _addButton.Pressed += RequestNewInstance;

        _cloneButton = GetNode<Button>("%CloneButton");
        _cloneButton.Pressed += () => RequestInstanceAction(InstanceCloned);

        _deleteButton = GetNode<Button>("%DeleteButton");
        _deleteButton.Pressed += () => RequestInstanceAction(InstanceDeleted);

        _closeButton = GetNode<Button>("%CloseButton");
        _closeButton.Pressed += RequestClosing;
    }

    private void InitializeTree()
    {
        _instanceTree = GetNode<Tree>("%InstanceTree");
        _instanceTree.ItemActivated += () => RequestInstanceAction(InstanceSelected);
        _instanceRoot = _instanceTree.CreateItem();
    }

    private void RebuildColumns()
    {
        _instanceTree.Columns = _columns.Count < 1 ? 1 : _columns.Count;
        for (int i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            _instanceTree.SetColumnTitle(i, column.Title);
            _instanceTree.SetColumnExpand(i, column.Expand);
            _instanceTree.SetColumnClipContent(i, column.WrapText);
            if (!column.Expand)
            {
                _instanceTree.SetColumnCustomMinimumWidth(i, 50);
            }
        }

        foreach (var child in _instanceRoot.GetChildren())
        {
            child.Free();
        }
    }

    private void RequestNewInstance()
    {
        var nextId = _instanceRoot.GetChildren()
            .Select(i => i.GetMetadata(0).AsInt32())
            .Max() + 1;
        
        InstanceAdded?.Invoke(nextId);
    }

    private void RequestInstanceAction(Action<int> action)
    {
        var index = _instanceTree.GetSelected()?.GetMetadata(0).AsInt32();
        if (index != null)
        {
            action?.Invoke(index.Value);
        }
    }

    private void RequestClosing()
    {
        if (Visible)
        {
            Closed?.Invoke();
        }
    }

    private record struct Column(string Title, bool Expand, bool WrapText);
}