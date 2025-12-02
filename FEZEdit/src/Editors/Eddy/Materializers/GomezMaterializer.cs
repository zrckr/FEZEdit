using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Extensions;
using Godot;

namespace FEZEdit.Editors.Eddy.Materializers;

using FEZRepacker.Core.Definitions.Game.Level;

public partial class GomezMaterializer : Node
{
    private const string Path = "gomez";

    private const string Animation = "idlewink";

    private static readonly Vector3 Offset = new(-0.09375f, 1.625f, 0f);

    private Transform3D _transform;

    public static GomezMaterializer Create(TrileFace face)
    {
        var materializer = new GomezMaterializer();
        materializer._transform.Basis = face.Face.AsBasis();
        materializer._transform.Origin = Offset + face.Id.ToGodot();
        return materializer;
    }
    
    public override void _Ready()
    {
        var characterAnimations = ContentLoader.LoadCharacterAnimations(Path);
        var frames = ContentConversion.ConvertToSpriteFrames(characterAnimations.Animations);
        
        var animatedSprite = new AnimatedSprite3D
        {
            SpriteFrames = frames,
            PixelSize = Mathz.PixelSize,
            Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            Transform = _transform
        };
        AddChild(animatedSprite, true);
        animatedSprite.Play(Animation);
    }
}