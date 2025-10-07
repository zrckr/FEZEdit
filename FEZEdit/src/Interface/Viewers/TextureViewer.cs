using System;
using System.Collections.Generic;
using FEZEdit.Loaders;
using FEZEdit.Materializers;
using Godot;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Interface.Viewers;

public partial class TextureViewer : Viewer
{
    public override event Action<object> ObjectSelected;
    
    public override Dictionary<Type, Type> Materializers => new()
    {
        { typeof(AnimatedTexture), typeof(AnimatedTextureMaterializer) },
        { typeof(Texture2D), typeof(Texture2DMaterializer) },
    };

    private Camera3D _freelookCamera;

    public override void _Ready()
    {
        base._Ready();
        _freelookCamera = GetNode<Camera3D>("%FreelookCamera");
    }
}