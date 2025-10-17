using FEZEdit.Core;
using FEZEdit.Extensions;
using Godot;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Editors.Texture;

public partial class Texture2DMaterializer: Node3D
{
    public void Initialize(Texture2D texture2D)
    {
        var sprite3D = new Sprite3D
        {
            Texture = texture2D.ToImageTexture(),
            PixelSize = Mathz.PixelSize,
            Shaded = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
        };
        AddChild(sprite3D);
    }
}