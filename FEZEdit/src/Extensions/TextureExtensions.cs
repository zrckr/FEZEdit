using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.XNA;
using Godot;

namespace FEZEdit.Extensions;

public static class TextureExtensions
{
    public const string DefaultAnimation = "default";

    private const string ShaderPath = "res://src/Shaders/TrixelMaterial.gdshader";

    public static Material ToGodotMaterial(this FEZRepacker.Core.Definitions.Game.XNA.Texture2D texture2D)
    {
        var material = new ShaderMaterial();
        material.Shader = GD.Load<Shader>(ShaderPath);
        material.SetShaderParameter("TEXTURE", texture2D.ToImageTexture());
        return material;
    }

    public static FEZRepacker.Core.Definitions.Game.XNA.Texture2D ToXna(this ImageTexture texture)
    {
        return new FEZRepacker.Core.Definitions.Game.XNA.Texture2D
        {
            Format = SurfaceFormat.Color,
            Width = texture.GetWidth(),
            Height = texture.GetHeight(),
            MipmapLevels = 1,
            TextureData = texture.GetImage().GetData()
        };
    }

    public static ImageTexture ToImageTexture(this FEZRepacker.Core.Definitions.Game.XNA.Texture2D texture2D)
    {
        return ImageTexture.CreateFromImage(
            Image.CreateFromData(
                texture2D.Width,
                texture2D.Height,
                false,
                Image.Format.Rgba8,
                texture2D.TextureData)
        );
    }
    
    private static ImageTexture ToImageTexture(this FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture animatedTexture)
    {
        return ImageTexture.CreateFromImage(
            Image.CreateFromData(
                animatedTexture.AtlasWidth,
                animatedTexture.AtlasHeight,
                false,
                Image.Format.Rgba8,
                animatedTexture.TextureData)
        );
    }

    public static SpriteFrames ToSpriteFrames(
        this FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture animatedTexture)
    {
        var spriteFrames = new SpriteFrames();
        spriteFrames.SetAnimationSpeed(DefaultAnimation, animatedTexture.Frames.Count);
        spriteFrames.SetAnimationLoop(DefaultAnimation, true);

        foreach (var frame in animatedTexture.Frames)
        {
            var atlasTexture = new AtlasTexture();
            atlasTexture.Atlas = animatedTexture.ToImageTexture();
            atlasTexture.Region = frame.Rectangle.ToGodot();
            var duration = (float)frame.Duration.TotalSeconds * animatedTexture.Frames.Count;
            spriteFrames.AddFrame(DefaultAnimation, atlasTexture, duration);
        }

        return spriteFrames;
    }

    public static SpriteFrames ToSpriteFrames(
        this IDictionary<string, FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture> animatedTextures)
    {
        var spriteFrames = new SpriteFrames();
        spriteFrames.RemoveAnimation(DefaultAnimation);

        foreach ((string name, var animatedTexture) in animatedTextures)
        {
            spriteFrames.AddAnimation(name);
            spriteFrames.SetAnimationSpeed(name, animatedTexture.Frames.Count);
            
            foreach (var frame in animatedTexture.Frames)
            {
                var atlasTexture = new AtlasTexture();
                atlasTexture.Atlas = animatedTexture.ToImageTexture();
                atlasTexture.Region = frame.Rectangle.ToGodot();
                var duration = (float)frame.Duration.TotalSeconds * animatedTexture.Frames.Count;
                spriteFrames.AddFrame(name, atlasTexture, duration);
            }
        }
        
        return spriteFrames;
    }
}