using System;
using System.Collections.Generic;
using System.Linq;

namespace FEZEdit.Memento;

public sealed class MementoManager(object target)
{
    public event Action<string> OnStateChanged;

    public event Action<Exception> OnError;

    public int UndoCount => _undoStack.Count;

    public int RedoCount => _redoStack.Count;

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public int MaxUndoSteps { get; set; } = 100;

    private readonly Stack<Memento> _undoStack = new();

    private readonly Stack<Memento> _redoStack = new();

    private readonly Stack<Memento> _scopeStack = new();

    private readonly object _target = target ?? throw new ArgumentNullException(nameof(target));

    public IDisposable BeginScope(string description = null)
    {
        return new MementoScope(this, description);
    }

    internal void StartScope(string description)
    {
        _scopeStack.Push(new Memento(_target, description));
    }

    internal void EndScope()
    {
        if (_scopeStack.Count > 0)
        {
            var memento = _scopeStack.Pop();
            if (memento.HasChanges(_target))
            {
                PushToUndoStack(memento);
                OnStateChanged?.Invoke($"Action: {memento.Description}");
            }
        }
    }

    public void SaveState(string description = null)
    {
        var memento = new Memento(_target, description);
        if (_undoStack.Count == 0 || memento.HasChanges(_target))
        {
            PushToUndoStack(memento);
            OnStateChanged?.Invoke($"Saved: {memento.Description}");
        }
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        try
        {
            var undoMemento = _undoStack.Pop();
            var redoMemento = new Memento(_target, $"Redo: {undoMemento.Description}");
            undoMemento.RestoreState(_target);
            PushToRedoStack(redoMemento);
            OnStateChanged?.Invoke($"Undone: {undoMemento.Description}");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            throw;
        }
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        try
        {
            var redoMemento = _redoStack.Pop();
            var undoMemento = new Memento(_target, $"Undo: {redoMemento.Description}");
            redoMemento.RestoreState(_target);
            PushToUndoStack(undoMemento);
            OnStateChanged?.Invoke($"Redone: {redoMemento.Description}");
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            throw;
        }
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _scopeStack.Clear();
    }

    public IEnumerable<string> GetUndoHistory()
    {
        return _undoStack.Select(m => $"{m.Timestamp:HH:mm:ss} - {m.Description}").ToList();
    }

    public IEnumerable<string> GetRedoHistory()
    {
        return _redoStack.Select(m => $"{m.Timestamp:HH:mm:ss} - {m.Description}").ToList();
    }

    private void PushToUndoStack(Memento memento)
    {
        _undoStack.Push(memento);
        _redoStack.Clear();
        TrimStackIfNeeded(_undoStack);
    }

    private void PushToRedoStack(Memento memento)
    {
        _redoStack.Push(memento);
        TrimStackIfNeeded(_redoStack);
    }

    private void TrimStackIfNeeded(Stack<Memento> stack)
    {
        if (stack.Count > MaxUndoSteps)
        {
            var items = stack.Take(MaxUndoSteps).ToList();
            stack.Clear();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                stack.Push(items[i]);
            }
        }
    }
}