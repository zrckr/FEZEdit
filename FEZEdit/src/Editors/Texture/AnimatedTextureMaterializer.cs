using FEZEdit.Core;
using FEZEdit.Extensions;
using Godot;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;

namespace FEZEdit.Editors.Texture;

public partial class AnimatedTextureMaterializer : Node3D
{
    private const string DefaultAnimation = "default";

    public void Initialize(AnimatedTexture animatedTexture)
    {
        var animatedSprite = new AnimatedSprite3D
        {
            Animation = DefaultAnimation,
            Autoplay = DefaultAnimation,
            SpriteFrames = animatedTexture.ToSpriteFrames(),
            PixelSize = Mathz.PixelSize,
            Shaded = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        animatedSprite.AddChild(MaterializerProxy.CreateEmpty(animatedTexture));
        AddChild(animatedSprite);
    }
}