using FEZEdit.Extensions;
using Godot;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;

namespace FEZEdit.Materializers;

public class AnimatedTextureMaterializer: Materializer<AnimatedTexture, AnimatedSprite3D>
{
    private const float PixelSize = 1f / 16f;
    
    private const string DefaultAnimation = "default";

    protected override AnimatedSprite3D Materialize(AnimatedTexture input)
    {
        return new AnimatedSprite3D
        {
            Animation = DefaultAnimation,
            Autoplay = DefaultAnimation,
            SpriteFrames = input.ToSpriteFrames(),
            PixelSize = PixelSize,
            Shaded = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
    }
}