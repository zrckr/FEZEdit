using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Extensions;
using FEZRepacker.Core.Conversion;
using FEZRepacker.Core.FileSystem;
using FEZRepacker.Core.XNB;
using Godot;

namespace FEZEdit.Content;

public sealed class FolderProvider : IContentProvider
{
    public string Root => _assetDirectory.Name;

    public IEnumerable<string> Files => _files.Keys;

    private readonly Dictionary<string, FileInfo> _files = new();

    private readonly DirectoryInfo _assetDirectory;

    public FolderProvider(FileSystemInfo info)
    {
        if (info is not DirectoryInfo directoryInfo)
        {
            throw new DirectoryNotFoundException(info.FullName);
        }

        _assetDirectory = directoryInfo;
        Refresh();
    }

    public bool Exists(string path)
    {
        path = path.ToLowerInvariant();
        return _files.ContainsKey(path);
    }

    public string GetExtension(string path)
    {
        path = path.ToLowerInvariant();
        if (!_files.TryGetValue(path, out var info))
        {
            throw new FileNotFoundException(path);
        }

        (_, string extension) = info.FullName.SplitAtExtension();
        return extension;
    }

    public string GetFullPath(string path)
    {
        path = path.ToLowerInvariant();
        return _files.TryGetValue(path, out var info) 
            ? info.FullName
            : string.Empty;
    }

    public T Load<T>(string path) where T : class
    {
        FileInfo info = null;
        foreach ((string filePath, FileInfo fileInfo) in _files)
        {
            if (filePath.Equals(path, StringComparison.InvariantCultureIgnoreCase))
            {
                info = fileInfo;
                break;
            }
        }

        if (info is not { Exists: true })
        {
            throw new FileNotFoundException(path);
        }

        if (info.Extension == ".xnb")
        {
            using var xnbStream = info.Open(FileMode.Open);
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

        var bundles = FileBundle.BundleFilesAtPath(info.FullName);
        if (bundles.Count == 0)
        {
            throw new FileNotFoundException(info.FullName);
        }

        return (T)FormatConversion.Deconvert(bundles.First())!;
    }

    public AudioStreamWav LoadSound(string path)
    {
        foreach ((string file, var info) in _files)
        {
            if (file.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
            {
                // BUG: Godot reports that the number of bytes counted is less than stated.
                return AudioStreamWav.LoadFromFile(info.FullName);
            }
        }

        throw new FileNotFoundException(path);
    }

    public void Refresh()
    {
        _files.Clear();
        foreach (var file in _assetDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var path = file.FullName.WithoutBaseDirectory(_assetDirectory.FullName);
            (string filePath, _) = path.SplitAtExtension();
            _files[filePath.ToLowerInvariant()] = file;
        }
    }
    
    public string GetRealPath(string path)
    {
        return Path.Combine(_assetDirectory.FullName, path);
    }
}