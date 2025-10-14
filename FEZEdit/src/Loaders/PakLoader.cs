using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Core;
using FEZEdit.Interface;
using FEZRepacker.Core.Conversion;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using FEZRepacker.Core.FileSystem;
using FEZRepacker.Core.XNB;
using Godot;
using Serilog;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Loaders;

public class PakLoader : ILoader
{
    private static readonly ILogger Logger = LoggerFactory.Create<PakLoader>();

    public string Root => PakFile.Name;

    private FileInfo PakFile { get; init; }

    private readonly Dictionary<string, PakFileRecord> _files = new();

    public static PakLoader Open(FileSystemInfo info)
    {
        if (info is not FileInfo { Extension: ".pak", Exists: true } pakFile)
        {
            throw new FileLoadException(info.ToString());
        }

        var loader = new PakLoader { PakFile = pakFile };
        loader.RefreshFiles();
        return loader;
    }

    ~PakLoader()
    {
        _files.Clear();
    }

    public IEnumerable<string> GetFiles()
    {
        return _files.Keys;
    }

    public Godot.Texture2D GetIcon(string file, IconsResource icons)
    {
        if (!_files.TryGetValue(file.ToLower(), out var record))
        {
            throw new FileLoadException(file);
        }

        return record.FindExtension() switch
        {
            ".xnb" => icons.XnbFile,
            ".ogg" => icons.AudioFile,
            ".fxc" => icons.FxFile,
            _ => icons.File
        };
    }

    public bool HasFile(string file)
    {
        return _files.ContainsKey(file.ToLower());
    }

    public void RefreshFiles()
    {
        using var stream = PakFile.OpenRead();
        using var reader = new PakReader(stream);

        _files.Clear();
        foreach (var record in reader.ReadFiles())
        {
            _files[record.Path] = record;
        }
    }

    public object LoadAsset(string path)
    {
        return LoadFromRecord<object>(path);
    }

    public ArtObject LoadArtObject(string assetName)
    {
        return LoadFromRecord<ArtObject>(Path.Combine("art objects", assetName));
    }

    public TrileSet LoadTrileSet(string assetName)
    {
        return LoadFromRecord<TrileSet>(Path.Combine("trile sets", assetName));
    }

    public Texture2D LoadBackgroundPlane(string assetName)
    {
        return LoadFromRecord<Texture2D>(Path.Combine("background planes", assetName));
    }

    public AnimatedTexture LoadAnimatedBackgroundPlane(string assetName)
    {
        return LoadFromRecord<AnimatedTexture>(Path.Combine("background planes", assetName));
    }

    public AudioStreamWav LoadSound(string assetName)
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, AnimatedTexture> LoadCharacterAnimations(string assetName)
    {
        var assetDirectory = Path.Combine("character animations", assetName).ToLower();

        var animations = new Dictionary<string, AnimatedTexture>();
        foreach ((string path, var record) in _files)
        {
            var found = path.StartsWith(assetDirectory, StringComparison.InvariantCultureIgnoreCase);
            var metadata = path.Contains("metadata");
            if (found && !metadata)
            {
                var @object = DeserializeObject<AnimatedTexture>(record);
                var fileName = Path.GetFileNameWithoutExtension(path);
                animations.Add(fileName, @object);
            }
        }

        return animations;
    }

    public void RepackAsset(string path, string targetDirectory, RepackingMode mode)
    {
        if (mode is < RepackingMode.UnpackRaw or > RepackingMode.UnpackConverted)
        {
            EventBus.Error("Unsupported repacking mode: {0}", mode);
            return;
        }

        var recordsToRepack = _files
            .Where(kv => kv.Key.Length >= path.Length)
            .Where(kv => kv.Key[..path.Length] == path)
            .Select(kv => kv.Value)
            .ToArray();

        var progress = new ProgressValue(0, 0, recordsToRepack.Length, 1);
        foreach (var record in recordsToRepack)
        {
            var extension = record.FindExtension();
            try
            {
                using var stream = record.Open();
                using var bundle = UnpackFile(extension, stream, mode);

                bundle.BundlePath = Path.Combine(targetDirectory, record.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(bundle.BundlePath) ?? "");

                foreach (var outputFile in bundle.Files)
                {
                    var fileName = bundle.BundlePath + bundle.MainExtension + outputFile.Extension;
                    using var fileOutputStream = File.Open(fileName, FileMode.Create);
                    outputFile.Data.CopyTo(fileOutputStream);
                }

                progress.Next();
                EventBus.Progress(progress, "'{0}' converted into '{1}' format", record.Path, bundle.MainExtension);
            }
            catch (Exception exception)
            {
                EventBus.Error("Unable to unpack: {0}", record.Path);
                Logger.Error(exception, "Unable to unpack '{0}'", record.Path);
            }
        }

        EventBus.Success($"Assets unpacked: {progress.Value}");
    }

    public static FileBundle UnpackFile(string extension, Stream stream, RepackingMode mode)
    {
        if (extension != ".xnb")
        {
            return FileBundle.Single(stream, extension);
        }

        switch (mode)
        {
            case RepackingMode.UnpackRaw:
                return FileBundle.Single(stream, extension);

            case RepackingMode.UnpackDecompressXnb:
                return FileBundle.Single(XnbCompressor.Decompress(stream), ".xnb");

            case RepackingMode.UnpackConverted:
                var initialStreamPosition = stream.Position;
                try
                {
                    var outputData = XnbSerializer.Deserialize(stream)!;
                    return FormatConversion.Convert(outputData);
                }
                catch (Exception exception)
                {
                    EventBus.Error("Cannot deserialize XNB file. Saving raw file instead");
                    Logger.Error(exception, "Cannot deserialize XNB file'");
                    stream.Seek(initialStreamPosition, SeekOrigin.Begin);
                    return FileBundle.Single(stream, extension);
                }
        }

        return new FileBundle();
    }

    private T LoadFromRecord<T>(string path)
    {
        return !_files.TryGetValue(path.ToLower(), out var record)
            ? throw new FileNotFoundException(path)
            : DeserializeObject<T>(record);
    }

    private static T DeserializeObject<T>(PakFileRecord record)
    {
        using var xnbStream = record.Open();
        var initialPosition = xnbStream.Position;
        try
        {
            return (T)XnbSerializer.Deserialize(xnbStream)!;
        }
        catch (Exception)
        {
            xnbStream.Seek(initialPosition, SeekOrigin.Begin);
            throw;
        }
    }
}