using System;
using System.Collections.Generic;
using System.Linq;

namespace FEZEdit.Core;

/// <summary>
/// Provides a high-level interface for implementing undo and redo operations.
/// </summary>
/// <remarks>
/// It's similar to <see cref="Godot.UndoRedo"/>, but is intended for use with pure C# code. 
/// </remarks>
public sealed class UndoRedo
{
    public const int NoLimit = 0;
    
    public string CurrentActionName => _currentAction?.Name;

    public int HistoryCount => _undoQueue.Count;

    public bool HasRedo => _redoStack.Count > 0;

    public bool HasUndo => _undoQueue.Count > 0;
    
    public bool IsCommitting { get; private set; }
    
    public int Version { get; private set; }

    public int MaxHistorySteps
    {
        get => _maxHistorySteps;
        set
        {
            _maxHistorySteps = value;
            EnforceHistoryLimit();
        }
    }

    private readonly LinkedList<UndoRedoAction> _undoQueue = [];

    private Stack<UndoRedoAction> _redoStack = new();

    private UndoRedoAction _currentAction;

    private int _maxHistorySteps = NoLimit;

    public void CreateAction(string name, object tag = null)
    {
        if (_currentAction != null)
        {
            throw new InvalidOperationException("Previous action not committed. Commit action first.");
        }

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Action name cannot be null or empty.", nameof(name));

        _currentAction = new UndoRedoAction(name, tag);
    }

    public void AddDoMethod(Action method)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddDoStep(new MethodStep(method ?? throw new ArgumentNullException(nameof(method))));
    }

    public void AddDoProperty<T>(Func<T> getter, Action<T> setter, T newValue)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddDoStep(new PropertyStep<T>(
            getter ?? throw new ArgumentNullException(nameof(getter)),
            setter ?? throw new ArgumentNullException(nameof(setter)), 
            newValue));
    }

    public void AddUndoMethod(Action method)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddUndoStep(new MethodStep(method ?? throw new ArgumentNullException(nameof(method))));
    }

    public void AddUndoProperty<T>(Func<T> getter, Action<T> setter, T newValue)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddUndoStep(new PropertyStep<T>(
            getter ?? throw new ArgumentNullException(nameof(getter)),
            setter ?? throw new ArgumentNullException(nameof(setter)), 
            newValue));
    }

    public void Clear()
    {
        _undoQueue.Clear();
        _redoStack.Clear();
        _currentAction = null;
        Version = 0;
    }

    public void ClearHistoryForTag(object tag)
    {
        if (tag == null)
        {
            return;
        }

        var current = _undoQueue.First;
        while (current != null)
        {
            var next = current.Next;
            if (tag.Equals(current.Value.Tag))
            {
                _undoQueue.Remove(current);
            }
            current = next;
        }
        
        var newRedoStack = new Stack<UndoRedoAction>();
        foreach (var action in _redoStack.Where(action => !tag.Equals(action.Tag)))
        {
            newRedoStack.Push(action);
        }
        _redoStack = newRedoStack;
    }
    
    public IEnumerable<object> GetTagsInHistory()
    {
        var tags = new HashSet<object>();
        foreach (var action in _undoQueue.Where(action => action.Tag != null))
        {
            tags.Add(action.Tag);
        }
        foreach (var action in _redoStack.Where(action => action.Tag != null))
        {
            tags.Add(action.Tag);
        }
        return tags;
    }
    
    public bool HasHistoryForTag(object tag)
    {
        if (tag == null)
        {
            return false;
        }

        return _undoQueue.Any(a => tag.Equals(a.Tag)) || 
               _redoStack.Any(a => tag.Equals(a.Tag));
    }

    public void CommitAction()
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action to commit.");
        }

        IsCommitting = true;
        try
        {
            _currentAction.ExecuteDo();
            _undoQueue.AddLast(_currentAction);
            Version++;
            _redoStack.Clear();
            EnforceHistoryLimit();
        }
        finally
        {
            _currentAction = null;
            IsCommitting = false;
        }
    }

    public void Redo()
    {
        if (!HasRedo)
        {
            throw new InvalidOperationException("No actions to redo.");
        }

        var action = _redoStack.Pop();
        IsCommitting = true;
        try
        {
            action.ExecuteDo();
            _undoQueue.AddLast(action);
            Version++;
        }
        finally
        {
            IsCommitting = false;
        }
    }

    public void Undo()
    {
        if (!HasUndo)
        {
            throw new InvalidOperationException("No actions to undo.");
        }

        var action = _undoQueue.Last!.Value;
        _undoQueue.RemoveLast();
        
        IsCommitting = true;
        try
        {
            action.ExecuteUndo();
            _redoStack.Push(action);
            Version--;
        }
        finally
        {
            IsCommitting = false;
        }
    }
    
    private void EnforceHistoryLimit()
    {
        if (_maxHistorySteps <= NoLimit)
        {
            return;
        }
        while (_undoQueue.Count > _maxHistorySteps)
        {
            // Remove the oldest action (from the front of the linked list)
            _undoQueue.RemoveFirst();
        }
    }
    
    private interface IStep
    {
        void Execute();
    }

    private class MethodStep(Action method) : IStep
    {
        private readonly Action _method = method ?? throw new ArgumentNullException(nameof(method));

        public void Execute() => _method.Invoke();
    }

    private class PropertyStep<T>(Func<T> getter, Action<T> setter, T value) : IStep
    {
        private readonly Func<T> _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        private readonly Action<T> _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        private T _value = value;

        public void Execute()
        {
            var currentValue = _getter();
            _setter(_value);
            _value = currentValue;
        }
    }

    private class UndoRedoAction(string name, object tag)
    {
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

        public object Tag { get; } = tag;   // nullable

        private readonly List<IStep> _doSteps = [];

        private readonly List<IStep> _undoSteps = [];

        public void AddDoStep(IStep step) => _doSteps.Add(step ?? throw new ArgumentNullException(nameof(step)));

        public void AddUndoStep(IStep step) => _undoSteps.Add(step ?? throw new ArgumentNullException(nameof(step)));

        public void ExecuteDo()
        {
            foreach (var step in _doSteps)
            {
                step.Execute();
            }
        }

        public void ExecuteUndo()
        {
            // Execute undo steps in reverse order for proper undo behavior
            for (int i = _undoSteps.Count - 1; i >= 0; i--)
            {
                _undoSteps[i].Execute();
            }
        }
    }
}