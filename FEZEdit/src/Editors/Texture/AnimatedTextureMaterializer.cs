using FEZEdit.Core;
using FEZEdit.Singletons;
using Godot;

namespace FEZEdit.Editors.Texture;

using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;

public partial class AnimatedTextureMaterializer : Node3D
{
    private const string DefaultAnimation = "default";

    public void Initialize(AnimatedTexture animatedTexture)
    {
        var animatedSprite = new AnimatedSprite3D
        {
            Animation = DefaultAnimation,
            Autoplay = DefaultAnimation,
            SpriteFrames = ContentConversion.ConvertToSpriteFrames(animatedTexture),
            PixelSize = Mathz.PixelSize,
            Shaded = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        animatedSprite.AddChild(MaterializerProxy.CreateEmpty(animatedTexture));
        AddChild(animatedSprite);
    }
}