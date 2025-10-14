using System.Collections.Generic;
using System.IO;
using FEZEdit.Core;
using FEZEdit.Interface;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using FEZRepacker.Core.Definitions.Game.XNA;
using Godot;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Loaders;

public interface ILoader
{
    string Root { get; }
    
    IEnumerable<string> GetFiles();

    Godot.Texture2D GetIcon(string file, IconsResource icons);

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
}