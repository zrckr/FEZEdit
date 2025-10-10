using System;
using System.Collections.Generic;

namespace FEZEdit.Interface.Editors;

public class EditorHistory
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

    private readonly Stack<UndoRedoAction> _redoStack = new();

    private UndoRedoAction _currentAction;

    private int _maxHistorySteps = NoLimit;

    public void CreateAction(string name)
    {
        if (_currentAction != null )
        {
            throw new InvalidOperationException("Previous action not committed. Commit action first.");
        }

        _currentAction = new UndoRedoAction(name);
    }

    public void AddDoMethod(Action method)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddDoStep(new MethodStep(method));
    }

    public void AddDoProperty<T>(Func<T> getter, Action<T> setter, T newValue)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddDoStep(new PropertyStep<T>(getter, setter, newValue));
    }

    public void AddUndoMethod(Action method)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddUndoStep(new MethodStep(method));
    }

    public void AddUndoProperty<T>(Func<T> getter, Action<T> setter, T newValue)
    {
        if (_currentAction == null)
        {
            throw new InvalidOperationException("No active action. Create action first.");
        }

        _currentAction.AddUndoStep(new PropertyStep<T>(getter, setter, newValue));
    }

    public void Clear()
    {
        _undoQueue.Clear();
        _redoStack.Clear();
        _currentAction = null;
        Version = 0;
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
            // Remove the oldest action
            _undoQueue.RemoveLast();
        }
    }
    
    private interface IStep
    {
        void Execute();
    }

    private class MethodStep(Action method) : IStep
    {
        public void Execute() => method?.Invoke();
    }

    private class PropertyStep<T>(Func<T> getter, Action<T> setter, T value) : IStep
    {
        private T _value = value;

        public void Execute()
        {
            var currentValue = getter();
            setter(_value);
            _value = currentValue;
        }
    }

    private class UndoRedoAction(string name)
    {
        public string Name { get; } = name;

        private readonly List<IStep> _doSteps = [];

        private readonly List<IStep> _undoSteps = [];

        public void AddDoStep(IStep step) => _doSteps.Add(step);

        public void AddUndoStep(IStep step) => _undoSteps.Add(step);

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