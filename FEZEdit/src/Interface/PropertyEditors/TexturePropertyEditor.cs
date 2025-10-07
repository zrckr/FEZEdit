using FEZEdit.Extensions;
using Godot;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Interface.PropertyEditors;

public class TexturePropertyEditor : PropertyEditor<Texture2D>
{
    private const float MinimumSize = 120f;
    
    private TextureRect _textureRect;

    public override void SetTypedValue(Texture2D value) => _textureRect.Texture = value.ToImageTexture();

    public override Texture2D GetTypedValue() => (_textureRect.Texture as ImageTexture).ToXna();
    
    public override Control CreateControl()
    {
        _textureRect = new TextureRect
        {
            CustomMinimumSize = Vector2.Down * MinimumSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        return _textureRect;
    }
}