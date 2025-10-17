using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZRepacker.Core.FileSystem;
using FEZRepacker.Core.XNB;
using Godot;

namespace FEZEdit.Content;

/// <remarks>
/// Paths to the records are stored in the PAK file in lowercase 
/// </remarks>
public sealed class PakProvider : IContentProvider
{
    public string Root => _pakFile.Name;

    public IEnumerable<string> Files => _records.Keys;

    private readonly FileInfo _pakFile;

    private readonly Dictionary<string, string> _records = new();

    public PakProvider(FileSystemInfo info)
    {
        if (info is not FileInfo { Extension: ".pak", Exists: true } pakFile)
        {
            throw new FileNotFoundException(info.FullName);
        }

        _pakFile = pakFile;
        Refresh();
    }

    public bool Exists(string path)
    {
        path = path.ToLowerInvariant();
        return _records.ContainsKey(path);
    }

    public string GetExtension(string path)
    {
        path = path.ToLowerInvariant();
        return _records.TryGetValue(path, out var extension)
            ? extension
            : string.Empty;
    }

    public string GetFullPath(string path)
    {
        path = path.ToLowerInvariant();
        return _records.TryGetValue(path, out var extension)
            ? Path.Combine(_pakFile.DirectoryName!, path + extension)
            : string.Empty;
    }
    
    public PakFileRecord LoadRecord(string path)
    {
        path = path.ToLowerInvariant();
        if (!_records.ContainsKey(path))
        {
            throw new FileNotFoundException(path);
        }
        
        using var stream = _pakFile.OpenRead();
        using var reader = new PakReader(stream);

        return reader.ReadFiles()
            .FirstOrDefault(record => record.Path.Equals(path));
    }

    public T Load<T>(string path) where T : class
    {
        var record = LoadRecord(path);
        if (record == null)
        {
            throw new FileNotFoundException(path);
        }

        using var xnbStream = record.Open();
        var initialPosition = xnbStream.Position;
        try
        {
            return (T)XnbSerializer.Deserialize(xnbStream)!;
        }
        catch
        {
            xnbStream.Seek(initialPosition, SeekOrigin.Begin);
            throw;
        }
    }

    public AudioStreamWav LoadSound(string path)
    {
        throw new NotImplementedException();
    } 

    public void Refresh()
    {
        using var stream = _pakFile.OpenRead();
        using var reader = new PakReader(stream);

        _records.Clear();
        foreach (var record in reader.ReadFiles())
        {
            _records[record.Path] = record.FindExtension();
        }
    }
}