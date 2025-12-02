using System;

namespace FEZEdit.Memento;

public sealed class MementoScope : IDisposable
{
    private readonly MementoManager _manager;
    
    private bool _disposed;

    public MementoScope(MementoManager manager, string description)
    {
        _manager = manager;
        _manager.StartScope(description);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _manager.EndScope();
            _disposed = true;
        }
    }
}