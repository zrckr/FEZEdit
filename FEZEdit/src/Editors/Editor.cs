using System;
using FEZEdit.Memento;
using Godot;

namespace FEZEdit.Editors;

public abstract partial class Editor : Control
{
    public abstract bool Disabled { set; }

    public abstract object Value { get; set; }

    public abstract void _Refresh();
    
    public event Action ValueChanged;

    public bool CanUndo => _memento?.CanUndo ?? false;
    
    public bool CanRedo => _memento?.CanRedo ?? false;

    protected MementoManager Memento
    {
        get => _memento;
        set
        {
            _memento = value;
            if (_memento != null)
            {
                _memento.OnStateChanged += _ => ValueChanged?.Invoke();
            }
        }
    }
    
    public void Undo()
    {
        _memento?.Undo();
        _Refresh();
    }

    public void Redo()
    {
        _memento?.Redo();
        _Refresh();
    }

    private MementoManager _memento;
}