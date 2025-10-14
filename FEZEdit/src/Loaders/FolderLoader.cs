using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Core;
using FEZEdit.Interface;
using FEZRepacker.Core.Conversion;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using FEZRepacker.Core.Definitions.Game.XNA;
using FEZRepacker.Core.FileSystem;
using FEZRepacker.Core.XNB;
using Godot;
using Serilog;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Loaders;

public class FolderLoader : ILoader
{
    private static readonly ILogger Logger = LoggerFactory.Create<FolderLoader>();

    public string Root => AssetDirectory.Name;
    
    private DirectoryInfo AssetDirectory { get; init; }

    private readonly Dictionary<string, FileInfo> _files = new();

    public static FolderLoader Open(FileSystemInfo info)
    {
        if (info is not DirectoryInfo directoryInfo)
        {
            throw new DirectoryNotFoundException(info.FullName);
        }

        var loader = new FolderLoader { AssetDirectory = directoryInfo };
        loader.RefreshFiles();
        return loader;
    }

    ~FolderLoader()
    {
        _files.Clear();
    }

    public IEnumerable<string> GetFiles()
    {
        return _files.Keys;
    }

    public Godot.Texture2D GetIcon(string path, IconsResource icons)
    {
        if (!_files.TryGetValue(path, out var file))
        {
            throw new FileNotFoundException(path);
        }

        var index = file.FullName.IndexOf('.');
        var extension = file.FullName[index..];

        return extension switch
        {
            ".fezao.glb" => icons.MeshFile,
            ".fezfont.json" => icons.FontFile,
            ".fezfont.png" => icons.TextureFile,
            ".fezlvl.json" => icons.LevelFile,
            ".fezmap.json" => icons.MapFile,
            ".feznpc.json" => icons.JsonFile,
            ".fezsky.json" => icons.SkyFile,
            ".fezsong.json" => icons.JsonFile,
            ".fezts.glb" => icons.MeshFile,
            ".feztxt.json" => icons.TextFile,
            ".fxb" => icons.FxFile,
            ".fxc" => icons.FxFile,
            ".gif" => icons.AnimatedTextureFile,
            ".json" => icons.JsonFile,
            ".ogg" => icons.AudioFile,
            ".png" => icons.TextureFile,
            ".wav" => icons.AudioFile,
            _ => icons.File
        };
    }

    public bool HasFile(string file)
    {
        return _files.ContainsKey(file.ToLower());
    }

    public void RefreshFiles()
    {
        _files.Clear();
        foreach (var file in AssetDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var path = file.FullName[(AssetDirectory.FullName.Length + 1)..];
            _files[path] = file;
        }
    }

    public object LoadAsset(string path)
    {
        return LoadFromFile<object>(path);
    }

    public ArtObject LoadArtObject(string assetName)
    {
        return LoadFromFile<ArtObject>(Path.Combine("art objects", assetName));
    }

    public TrileSet LoadTrileSet(string assetName)
    {
        return LoadFromFile<TrileSet>(Path.Combine("trile sets", assetName));
    }

    public Texture2D LoadBackgroundPlane(string assetName)
    {
        return LoadFromFile<Texture2D>(Path.Combine("background planes", assetName));
    }

    public AnimatedTexture LoadAnimatedBackgroundPlane(string assetName)
    {
        return LoadFromFile<AnimatedTexture>(Path.Combine("background planes", assetName));
    }

    public AudioStreamWav LoadSound(string assetName)
    {
        var assetPath = Path.Combine("sounds", assetName);
        foreach ((string path, var file) in _files)
        {
            if (path.StartsWith(assetPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return AudioStreamWav.LoadFromFile(file.FullName);
            }
        }

        throw new FileNotFoundException(assetPath);
    }

    public IDictionary<string, AnimatedTexture> LoadCharacterAnimations(string assetName)
    {
        var assetDirectory = Path.Combine("character animations", assetName).ToLower();

        var animations = new Dictionary<string, AnimatedTexture>();
        foreach (string path in _files.Keys)
        {
            var found = path.StartsWith(assetDirectory, StringComparison.InvariantCultureIgnoreCase);
            var metadata = path.Contains("metadata");
            if (found && !metadata)
            {
                var @object = LoadFromFile<AnimatedTexture>(path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                animations.Add(fileName, @object);
            }
        }

        return animations;
    }

    public void RepackAsset(string path, string targetDirectory, RepackingMode mode)
    {
        if (mode is < RepackingMode.ConvertFromXnb or > RepackingMode.PackAssets)
        {
            EventBus.Error("Unsupported repacking mode: {0}", mode);
            return;
        }

        var originalPath = path.Replace("/", "\\");
        switch (mode)
        {
            case RepackingMode.ConvertFromXnb:
                ConvertFromXnb(originalPath, targetDirectory);
                break;

            case RepackingMode.ConvertToXnb:
                ConvertToXnb(originalPath, targetDirectory);
                break;

            case RepackingMode.PackAssets:
                PackAssets(originalPath, targetDirectory);
                break;
        }
    }

    private static void ConvertFromXnb(string path, string targetDirectory)
    {
        var xnbs = new List<FileInfo>();
        if (File.Exists(path))
        {
            if (Path.GetExtension(path).Equals(".xnb", StringComparison.OrdinalIgnoreCase))
            {
                xnbs.Add(new FileInfo(path));
            }
        }
        else if (Directory.Exists(path))
        {
            xnbs.AddRange(Directory
                .EnumerateFiles(path, "*.xnb", SearchOption.AllDirectories)
                .Select(file => new FileInfo(file)));
        }
        else
        {
            EventBus.Error("Path not found: {0}", path);
            return;
        }

        if (xnbs.Count == 0)
        {
            EventBus.Error("XNBs not found: {0}", path);
            return;
        }

        var progress = new ProgressValue(0, 0, xnbs.Count, 1);
        EventBus.Progress(progress, "Converting: {0}", path);

        foreach (var xnb in xnbs)
        {
            using var xnbStream = xnb.OpenRead();
            var initialStreamPosition = xnbStream.Position;
            FileBundle bundle;

            try
            {
                var outputData = XnbSerializer.Deserialize(xnbStream)!;
                bundle = FormatConversion.Convert(outputData);
            }
            catch (Exception)
            {
                xnbStream.Seek(initialStreamPosition, SeekOrigin.Begin);
                bundle = FileBundle.Single(xnbStream, ".xnb");
            }

            bundle.BundlePath = Path.Combine(targetDirectory, xnb.Name + bundle.MainExtension);
            var outputDirectory = Path.GetDirectoryName(bundle.BundlePath) ?? "";

            Directory.CreateDirectory(outputDirectory);
            foreach (var outputFile in bundle.Files)
            {
                using var fileOutputStream = new FileInfo(bundle.BundlePath + outputFile.Extension).Create();
                outputFile.Data.CopyTo(fileOutputStream);
            }

            progress.Next();
            EventBus.Progress(progress, "Converting: {0}", xnb.FullName);
            bundle.Dispose();
        }

        EventBus.Progress(ProgressValue.Complete);
        EventBus.Success("Assets converted at {0}", targetDirectory);
    }

    private static void ConvertToXnb(string path, string targetDirectory)
    {
        var bundles = FileBundle.BundleFilesAtPath(path);
        var progress = new ProgressValue(0, 0, bundles.Count, 1);
        EventBus.Progress(progress, "Converting Back: {0}", path);

        foreach (var conversion in PerformBatchConversion(bundles))
        {
            if (conversion.Converted)
            {
                var assetOutputFullPath = Path.Combine(targetDirectory, $"{conversion.Path}{conversion.Extension}");
                Directory.CreateDirectory(Path.GetDirectoryName(assetOutputFullPath) ?? "");

                using var assetFile = new FileInfo(assetOutputFullPath).Create();
                conversion.Stream.CopyTo(assetFile);

                progress.Next();
                EventBus.Progress(progress, "Converting Back: {0}", conversion.Path);
            }
        }

        EventBus.Progress(ProgressValue.Complete);
        EventBus.Success("Assets converted at {0}", targetDirectory);
    }

    private static void PackAssets(string path, string targetDirectory)
    {
        var bundles = FileBundle.BundleFilesAtPath(path);
        bundles.Sort((a, b) =>
        {
            var converterA = FormatConverters.FindByExtension(a.MainExtension);
            var converterB = FormatConverters.FindByExtension(b.MainExtension);

            if (converterA != null && converterB == null) return 1;
            if (converterA == null && converterB != null) return -1;
            return String.Compare(a.BundlePath, b.BundlePath, StringComparison.InvariantCultureIgnoreCase);
        });

        var progress = new ProgressValue(0, 0, bundles.Count, 1);
        EventBus.Progress(progress, "Packing: {0}", path);

        var tempPakPath = Path.GetTempPath() + "fezedit-" + Guid.NewGuid() + ".pak";
        using (var tempPakStream = File.Create(tempPakPath))
        {
            using var pakWriter = new PakWriter(tempPakStream);
            foreach (var conversion in PerformBatchConversion(bundles))
            {
                pakWriter.WriteFile(conversion.Path, conversion.Stream, conversion.Extension);
                progress.Next();
                EventBus.Progress(progress, "Packing: {0}", conversion.Path);
            }
        }

        var untitledPath = Path.Combine(targetDirectory, "Untitled.pak");
        File.Move(tempPakPath, untitledPath, overwrite: true);

        EventBus.Progress(ProgressValue.Complete);
        EventBus.Success("Assets packed: {0}", untitledPath);
    }

    private static IEnumerable<ConversionResult> PerformBatchConversion(List<FileBundle> bundles)
    {
        foreach (var bundle in bundles)
        {
            var results = new List<ConversionResult>();
            try
            {
                object @object = FormatConversion.Deconvert(bundle)!;
                var stream = XnbSerializer.Serialize(@object);
                results.Add(new ConversionResult(bundle.BundlePath, ".xnb", stream, true));
            }
            catch (Exception)
            {
                foreach (var file in bundle.Files)
                {
                    file.Data.Seek(0, SeekOrigin.Begin);
                    var ext = bundle.MainExtension + file.Extension;
                    results.Add(new ConversionResult(bundle.BundlePath, ext, file.Data, false));
                }
            }

            foreach (var result in results)
            {
                yield return result;
            }

            bundle.Dispose();
        }
    }

    private T LoadFromFile<T>(string path)
    {
        FileInfo info = null;
        foreach ((string filePath, FileInfo fileInfo) in _files)
        {
            if (filePath.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
            {
                info = fileInfo;
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
            catch (Exception)
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

    private record struct ConversionResult(string Path, string Extension, Stream Stream, bool Converted);
}