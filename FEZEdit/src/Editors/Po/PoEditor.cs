using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FEZEdit.Editors.Po;

public partial class PoEditor : Editor
{
    public override event Action ValueChanged; 
    
    public override object Value
    {
        get => _textStorage;
        set => _textStorage = (TextStorage)value;
    }

    public override bool Disabled
    {
        set
        {
            _languagesButton.Disabled = value;
            _addButton.Disabled = value;
            _removeButton.Disabled = value;
            foreach (var item in _root.GetChildren())
            {
                item.SetEditable(1, !value);
            }
        }
    }
    
    public override UndoRedo UndoRedo { get; } = new();
    
    [Export] private Godot.Collections.Dictionary<string, string> _languages = new();

    [Export] private string _sourceLanguage = string.Empty;

    [Export] private int _itemsPerPage = 20;

    private TextStorage _textStorage;

    private OptionButton _languagesButton;

    private Button _addButton;
    
    private Button _removeButton;

    private Tree _tableTree;

    private ConfirmationDialog _removeEntryDialog;

    private TreeItem _root;
    
    private int _selectedLanguageIndex;

    private List<string> _keys;

    public override void _Ready()
    {
        InitializeKeys();
        InitializeLanguages();
        InitializeAddButton();
        InitializeRemoveButton();
        InitializeTableTree();
        InitializeDialogs();
        UpdateTableRows(0);
    }

    private void InitializeKeys()
    {
        _keys = _textStorage[_sourceLanguage].Keys.ToList();
    }

    private void InitializeLanguages()
    {
        _languagesButton ??= GetNode<OptionButton>("%LanguagesButton");
        _languagesButton.ItemSelected += index => UpdateTableRows((int)index);
        var index = 0;
        foreach ((string locale, string displayName) in _languages)
        {
            _languagesButton.AddItem(Tr(displayName), index);
            _languagesButton.SetItemMetadata(index, locale);
            index++;
        }
        if (index > 0)
        {
            _languagesButton.Selected = 0;
        } 
    }

    private void InitializeAddButton()
    {
        _addButton ??= GetNode<Button>("%AddButton");
        _addButton.Pressed += AddNewRowToTable;
    }

    private void InitializeRemoveButton()
    {
        _removeButton ??= GetNode<Button>("%RemoveButton");
        _removeButton.Pressed += PreRemoveRowFromTable;
    }

    private void InitializeTableTree()
    {
        _tableTree ??= GetNode<Tree>("%TableTree");
        _tableTree.ItemEdited += UpdateRowInTable;
        _tableTree.SetColumns(2);
        _tableTree.SetColumnTitle(0, Tr("ID"));
        _tableTree.SetColumnTitle(1, Tr("Message"));
        _tableTree.SetColumnExpand(0, false);
        _tableTree.SetColumnCustomMinimumWidth(0, 240);
        _tableTree.SetColumnExpand(1, true);
    }

    private void InitializeDialogs()
    {
        _removeEntryDialog ??= GetNode<ConfirmationDialog>("%RemoveEntryDialog");
        _removeEntryDialog.Confirmed += PostRemoveRowFromTable;
    }
    
    private void UpdateTableRows(int index)
    {
        _selectedLanguageIndex = index;
        var selectedLanguage = _languages.Keys.ElementAt(_selectedLanguageIndex);
        var selectedStorage = _textStorage[selectedLanguage];
        
        _tableTree.Clear();
        _root = _tableTree.CreateItem();

        foreach (var key in _keys)
        {
            if (selectedStorage.TryGetValue(key, out string message))
            {
                var row = _tableTree.CreateItem(_root);
                row.SetText(0, key);
                row.SetEditable(0, false);
                row.SetSelectable(0, true);
                
                row.SetText(1, message);
                row.SetEditable(1, true);
                row.SetSelectable(0, true);
                row.SetEditMultiline(1, true);
            }
        }
    }
    
    private void AddNewRowToTable()
    {
        var key = $"MESSAGE_{_keys.Count + 1}";
        
        var row = _tableTree.CreateItem(_root);
        row.SetText(0, key);
        row.SetEditable(0, false);
        row.SetSelectable(0, true);
                
        row.SetText(1, string.Empty);
        row.SetEditable(1, true);
        row.SetSelectable(0, true);
        row.SetEditMultiline(1, true);

        var selectedLanguage = _languages.Keys.ElementAt(_selectedLanguageIndex);
        var selectedStorage = _textStorage[selectedLanguage];
        
        UndoRedo.CreateAction("Add new row");
        UndoRedo.AddDoMethod(() =>
        {
            _tableTree.ScrollToItem(row, true);
            selectedStorage.Add(key, string.Empty);
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _root.RemoveChild(row);
            selectedStorage.Remove(key);
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }
    
    private void PreRemoveRowFromTable()
    {
        var row = _tableTree.GetSelected();
        _removeEntryDialog.Title = string.Format(Tr("Remove entry ({0})?"), row.GetText(0)); 
        _removeEntryDialog.DialogText = row.GetText(1).Replace("\r\n", "\n");
        _removeEntryDialog.PopupCentered();
    }
    
    private void PostRemoveRowFromTable()
    {
        var row = _tableTree.GetSelected();
        var key = row.GetText(0).ToUpper();
        
        var selectedLanguage = _languages.Keys.ElementAt(_selectedLanguageIndex);
        var selectedStorage = _textStorage[selectedLanguage];
        
        var value = selectedStorage[key];
        UndoRedo.CreateAction("Remove row");
        UndoRedo.AddDoMethod(() =>
        {
            _root.RemoveChild(row);
            selectedStorage.Remove(key);
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _root.AddChild(row);
            selectedStorage.Add(key, value);
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }

    private void UpdateRowInTable()
    {
        var row = _tableTree.GetEdited();
        var key = row.GetText(0).ToUpper();
        var message = row.GetText(1).Replace("\n", "\r\n");
        
        var selectedLanguage = _languages.Keys.ElementAt(_selectedLanguageIndex);
        var selectedStorage = _textStorage[selectedLanguage];
        if (!selectedStorage.TryGetValue(key, out string oldMessage))
        {
            return;
        }

        UndoRedo.CreateAction("Update row");
        UndoRedo.AddDoMethod(() =>
        {
            selectedStorage[key] = message;
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            selectedStorage[key] = oldMessage;
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }
}