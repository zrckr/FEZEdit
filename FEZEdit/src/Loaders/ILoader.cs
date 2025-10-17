using System.Collections.Generic;
using FEZEdit.Core;
using FEZEdit.Main;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Loaders;

public interface ILoader
{
    string Root { get; }
    
    IEnumerable<string> GetFiles();
    
    string GetFileExtension(string file);

    Godot.Texture2D GetIcon(string file, IconsResource icons);

    string GetFilePath(string file);

    bool HasFile(string file);

    void RefreshFiles();
    
    object LoadAsset(string path);
    
    ArtObject LoadArtObject(string assetName);

    TrileSet LoadTrileSet(string assetName);

    Texture2D LoadBackgroundPlane(string assetName);

    AnimatedTexture LoadAnimatedBackgroundPlane(string assetName);

    IDictionary<string, AnimatedTexture> LoadCharacterAnimations(string assetName);
    
    AudioStreamWav LoadSound(string assetName);
    
    void RepackAsset(string path, string targetDirectory, RepackingMode mode);

    void SaveAsset(object @object, string path);
}