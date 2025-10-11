using FEZEdit.Extensions;
using Godot;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;

namespace FEZEdit.Materializers;

public partial class AnimatedTextureMaterializer: Materializer<AnimatedTexture>
{
    private const float PixelSize = 1f / 16f;
    
    private const string DefaultAnimation = "default";

    public override void CreateNodesFrom(AnimatedTexture animatedTexture)
    {
        var animatedSprite = new AnimatedSprite3D
        {
            Animation = DefaultAnimation,
            Autoplay = DefaultAnimation,
            SpriteFrames = animatedTexture.ToSpriteFrames(),
            PixelSize = PixelSize,
            Shaded = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        animatedSprite.AddChild(MaterializerProxy.CreateEmpty(animatedTexture));
        AddChild(animatedSprite);
    }
}