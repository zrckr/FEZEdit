using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Main;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;
using Serilog;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Singletons;

public static class ContentLoader
{
    private static readonly ILogger Logger = LoggerFactory.Create(nameof(ContentLoader));
    
    public static string Root => ContentProvider.Root;
    
    public static IEnumerable<string> Files => ContentProvider.Files;
 
    public static IContentProvider ContentProvider { private get; set; }
    
    public static bool Exists(string path)
    {
        if (ContentProvider == null)
        {
            Logger.Error("Failed to check if file exists: {0}", path);
            return false;
        }
        
        return ContentProvider.Exists(path);
    }

    public static string GetExtension(string path)
    {
        if (ContentProvider == null)
        {
            Logger.Error("Failed to get file extension: {0}", path);
            return string.Empty;
        }
        
        return ContentProvider.GetExtension(path);
    }

    public static string GetFullPath(string path)
    {
        if (ContentProvider == null)
        {
            Logger.Error("Failed to get full file path: {0}", path);
            return string.Empty;
        }
        
        return ContentProvider.GetFullPath(path);
    }
    
    public static Godot.Texture2D GetIcon(string path, IconsResource icons)
    {
        if (ContentProvider == null || icons == null)
        {
            Logger.Error("Failed to get file icon: {0}", path);
            return null;
        }
        
        return ContentProvider.GetExtension(path) switch
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
            ".xnb" => icons.XnbFile,
            _ => icons.File
        };
    }
    
    public static ArtObject LoadArtObject(string assetName)
    {
        return Load<ArtObject>(Path.Combine("art objects", assetName));
    }

    public static TrileSet LoadTrileSet(string assetName)
    {
        return Load<TrileSet>(Path.Combine("trile sets", assetName));
    }

    public static Texture2D LoadBackgroundPlane(string assetName)
    {
        return Load<Texture2D>(Path.Combine("background planes", assetName));
    }

    public static AnimatedTexture LoadBackgroundPlaneAnimated(string assetName)
    {
        return Load<AnimatedTexture>(Path.Combine("background planes", assetName));
    }
    
    public static Texture2D LoadOtherTexture(string assetName)
    {
        return Load<Texture2D>(Path.Combine("other textures", assetName));
    }
    
    public static IDictionary<string, AnimatedTexture> LoadCharacterAnimations(string assetName)
    {
        var characterDirectory = Path.Combine("character animations", assetName).ToLower();

        var animations = new Dictionary<string, AnimatedTexture>();
        foreach (string file in ContentProvider?.Files ?? [])
        {
            var found = file.StartsWith(characterDirectory, StringComparison.InvariantCultureIgnoreCase);
            var metadata = file.Contains("metadata");
            if (found && !metadata)
            {
                var @object = Load<AnimatedTexture>(file);
                var fileName = Path.GetFileNameWithoutExtension(file);
                animations.Add(fileName, @object);
            }
        }

        return animations;
    }

    public static AudioStreamWav LoadSound(string path)
    {
        try
        {
            path = Path.Combine("sounds", path);
            return ContentProvider.LoadSound(path);
        }
        catch (Exception exception)
        {
            EventBus.Error("Failed to load sound file: {0}", path);
            Logger.Error(exception, "Failed to load sound file '{0}'", path);
            return null;
        }
    }

    public static T Load<T>(string path) where T : class
    {
        try
        {
            return ContentProvider.Load<T>(path);
        }
        catch (Exception exception)
        {
            EventBus.Error("Failed to load file: {0}", path);
            Logger.Error(exception, "Failed to load file '{0}'", path);
            return null;
        }
    }

    public static void Refresh()
    {
        try
        {
            if (ContentProvider?.Files.Any() == true)
            {
                ContentProvider.Refresh();
            }
        }
        catch (Exception exception)
        {
            EventBus.Error("Failed to refresh: {0}", Root);
            Logger.Error(exception, "Failed to refresh '{0}'", ContentLoader.Root);
        }
    }
}