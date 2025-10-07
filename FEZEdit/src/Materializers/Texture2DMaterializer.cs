using FEZEdit.Extensions;
using Godot;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Materializers;

public class Texture2DMaterializer: Materializer<Texture2D, Sprite3D>
{
    private const float PixelSize = 1f / 16f;

    protected override Sprite3D Materialize(Texture2D input)
    {
        return new Sprite3D
        {
            Texture = input.ToImageTexture(),
            PixelSize = PixelSize,
            Shaded = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
    }
}