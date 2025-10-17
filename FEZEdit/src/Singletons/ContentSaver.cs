using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZRepacker.Core.Conversion;
using FEZRepacker.Core.FileSystem;
using FEZRepacker.Core.XNB;
using Serilog;

namespace FEZEdit.Singletons;

public static class ContentSaver
{
    private static readonly ILogger Logger = LoggerFactory.Create(nameof(ContentSaver));

    private const string XnbExtension = ".xnb";
    
    public static bool CanConvert => ContentProvider is FolderProvider;
    
    public static IContentProvider ContentProvider { private get; set; }

    public static void Save(object data, string path)
    {
        if (data is SaveData saveData)
        {
            try
            {
                SaveDataProvider.Write(path, saveData);
                EventBus.Success("Save slot writen: {0}", path);
            }
            catch (Exception exception)
            {
                EventBus.Error("Failed to write to save slot: {0}", path);
                Logger.Error(exception, "Failed to write to save slot '{0}'", path);
            }
            return;
        }
        
        if (ContentProvider == null)
        {
            EventBus.Error("Files are not loaded in FEZEdit: {0}", path);
            Logger.Error("Files are not loaded in FEZEdit '{0}'", path);
            return;
        }

        try
        {
            using var bundle = FormatConversion.Convert(data);
            bundle.BundlePath = path;

            var progress = new ProgressValue(0, 0, bundle.Files.Count, 1);
            EventBus.Progress(progress);

            foreach (var outputFile in bundle.Files)
            {
                var fileOutputPath = bundle.BundlePath + bundle.MainExtension + outputFile.Extension;
                using var fileOutputStream = new FileInfo(fileOutputPath).Create();
                outputFile.Data.CopyTo(fileOutputStream);

                progress.Next();
                EventBus.Progress(progress);
            }

            EventBus.Progress(ProgressValue.Complete);
            EventBus.Success("Assets converted at {0}", path);
        }
        catch (Exception exception)
        {
            EventBus.Progress(ProgressValue.Complete);
            EventBus.Error("Failed to save asset at: {0}", path);
            Logger.Error(exception, "Failed to save asset at '{0}'", path);
        }
    }

    public static void Repack(string path, string targetDirectory, RepackingMode repackingMode)
    {
        switch (repackingMode)
        {
            case RepackingMode.UnpackRaw:
            case RepackingMode.UnpackDecompressXnb:
            case RepackingMode.UnpackConverted:
                Unpack(path, targetDirectory, repackingMode);
                break;
            
            case RepackingMode.ConvertFromXnb:
                ConvertFromXnb(path, targetDirectory);
                break;
            
            case RepackingMode.ConvertToXnb:
                ConvertToXnb(path, targetDirectory);
                break;
            
            case RepackingMode.PackAssets:
                PackAssets(path, targetDirectory);
                break;
            
            default:
                EventBus.Error("Unsupported saving mode: {0}", repackingMode);
                return;
        }
    }

    private static void Unpack(string path, string targetDirectory, RepackingMode mode)
    {
        if (ContentProvider is not PakProvider pakProvider)
        {
            EventBus.Error("Only unpacking from PAK file supported: {0}", path);
            Logger.Error("Only unpacking from PAK file supported '{0}'", path);
            return;
        }
        
        var filesToUnpack = pakProvider.Files
            .Where(kv => kv.Length >= path.Length)
            .Where(kv => kv[..path.Length] == path)
            .ToArray();

        if (filesToUnpack.Length == 0)
        {
            EventBus.Progress(ProgressValue.Complete);
            EventBus.Error("No files to unpack: {0}", path);
            Logger.Error("No files to unpack: {0}", path);
            return;
        }

        var progress = new ProgressValue(0, 0, filesToUnpack.Length, 1);
        foreach (var file in filesToUnpack)
        {
            var record = pakProvider.LoadRecord(file);
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
                EventBus.Progress(progress, "'{0}' converted into '{1}' format", file, bundle.MainExtension);
            }
            catch (Exception exception)
            {
                EventBus.Error("Unable to unpack: {0}", file);
                Logger.Error(exception, "Unable to unpack '{0}'", file);
            }
        }

        EventBus.Success("Assets unpacked: {0}", progress.Value);
    }
    
    private static FileBundle UnpackFile(string extension, Stream stream, RepackingMode repackingMode)
    {
        if (extension != XnbExtension)
        {
            return FileBundle.Single(stream, extension);
        }

        switch (repackingMode)
        {
            case RepackingMode.UnpackRaw:
                return FileBundle.Single(stream, extension);

            case RepackingMode.UnpackDecompressXnb:
                return FileBundle.Single(XnbCompressor.Decompress(stream), XnbExtension);

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

            default:
                return new FileBundle();
        }
    }

    private static void ConvertFromXnb(string path, string targetDirectory)
    {
        if (ContentProvider is not FolderProvider folderProvider)
        {
            EventBus.Progress(ProgressValue.Complete);
            EventBus.Error("Only converting files from folder are supported: {0}", path);
            Logger.Error("Only converting files from folder are supported '{0}'", path);
            return;
        }

        path = folderProvider.GetRealPath(path);
        var xnbs = new List<FileInfo>();
        if (File.Exists(path))
        {
            if (Path.GetExtension(path).Equals(XnbExtension, StringComparison.OrdinalIgnoreCase))
            {
                xnbs.Add(new FileInfo(path));
            }
        }
        else if (Directory.Exists(path))
        {
            xnbs.AddRange(Directory
                .EnumerateFiles(path, "*" + XnbExtension, SearchOption.AllDirectories)
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
                bundle = FileBundle.Single(xnbStream, XnbExtension);
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
        if (ContentProvider is not FolderProvider folderProvider)
        {
            EventBus.Progress(ProgressValue.Complete);
            EventBus.Error("Only converting files from folder are supported: {0}", path);
            Logger.Error("Only converting files from folder are supported '{0}'", path);
            return;
        }
        
        path = folderProvider.GetRealPath(path);
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
        if (ContentProvider is not FolderProvider folderProvider)
        {
            EventBus.Progress(ProgressValue.Complete);
            EventBus.Error("Only converting files from folder are supported: {0}", path);
            Logger.Error("Only converting files from folder are supported '{0}'", path);
            return;
        }
        
        path = folderProvider.GetRealPath(path);
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
                results.Add(new ConversionResult(bundle.BundlePath, XnbExtension, stream, true));
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
    
    public enum RepackingMode
    {
        UnpackRaw,
        UnpackDecompressXnb,
        UnpackConverted,
        ConvertFromXnb,
        ConvertToXnb,
        PackAssets
    }
    
    private record struct ConversionResult(string Path, string Extension, Stream Stream, bool Converted);
}