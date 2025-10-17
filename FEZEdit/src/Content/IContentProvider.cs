using System.Collections.Generic;
using Godot;

namespace FEZEdit.Content;

public interface IContentProvider
{
    string Root { get; }
    
    IEnumerable<string> Files { get; }
    
    bool Exists(string path);

    string GetExtension(string path);

    string GetFullPath(string path);

    T Load<T>(string path) where T : class;

    AudioStreamWav LoadSound(string path);

    void Refresh();
}