using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Core;
using FEZEdit.Extensions;
using FEZEdit.Singletons;
using FEZRepacker.Core.Definitions.Game.Level;
using Godot;
using Mesh = Godot.Mesh;

namespace FEZEdit.Editors.Level;

using Level = FEZRepacker.Core.Definitions.Game.Level.Level;

public partial class LevelMaterializer : Node3D
{
    private const string BackgroundPlaneShader = "res://src/Shaders/BackgroundPlane.gdshader";

    private static readonly Orthogonal[] RotationIndices =
    [
        Orthogonal.BackUp,
        Orthogonal.LeftUp,
        Orthogonal.FrontUp,
        Orthogonal.RightUp
    ];

    public void Initialize(Level level)
    {
        Name = level.Name;
        MaterializeTriles(level);
        MaterializeArtObjects(level);
        MaterializeBackgroundPlanes(level);
        MaterializeCharacters(level);
    }

    private void MaterializeTriles(Level level)
    {
        var trileSet = ContentLoader.LoadTrileSet(level.TrileSetName);
        var meshLibrary = new MeshLibrary { ResourceName = level.TrileSetName };
        var material = trileSet.TextureAtlas.ToGodotMaterial();

        foreach ((int trileId, var trile) in trileSet.Triles)
        {
            meshLibrary.CreateItem(trileId);
            meshLibrary.SetItemName(trileId, trile.Name);
            meshLibrary.SetItemMesh(trileId, trile.Geometry.ToGodotMesh(material));
        }

        var gridMap = new TrileMap { Name = "Triles", MeshLibrary = meshLibrary, CellSize = Vector3.One };
        var triles = new Dictionary<Vector3I, TrileInstance>();

        foreach ((TrileEmplacement emplacement, var instance) in level.Triles)
        {
            var position = new Vector3I(emplacement.X, emplacement.Y, emplacement.Z);
            var offset = instance.Position.ToGodot() - new Vector3(emplacement.X, emplacement.Y, emplacement.Z);
            var orientation = RotationIndices[instance.PhiLight];
            gridMap.SetCellItem(position, instance.TrileId, orientation, offset);
            triles.Add(position, instance);
        }

        gridMap.AddChild(MaterializerProxy.CreateEmpty(triles));
        AddChild(gridMap);
    }

    private void MaterializeArtObjects(Level level)
    {
        var levelArtObjects = level.ArtObjects
            .Select(kv => kv.Value.Name)
            .Distinct();

        var meshes = new Dictionary<string, Mesh>();
        foreach (var name in levelArtObjects)
        {
            var artObject = ContentLoader.LoadArtObject(name);
            var material = artObject.Cubemap.ToGodotMaterial();
            var mesh = artObject.Geometry.ToGodotMesh(material);
            meshes.Add(name, mesh);
        }

        var artObjects = new Node3D { Name = "ArtObjects" };
        foreach ((int key, var instance) in level.ArtObjects)
        {
            var meshInstance = new MeshInstance3D { Name = $"{key}_{instance.Name}", Mesh = meshes[instance.Name] };
            meshInstance.Position = instance.Position.ToGodot();
            meshInstance.Quaternion = instance.Rotation.ToGodot();
            meshInstance.Scale = instance.Scale.ToGodot();
            meshInstance.AddChild(MaterializerProxy.CreateFromMesh(instance, meshes[instance.Name]));
            artObjects.AddChild(meshInstance);
        }

        AddChild(artObjects);
    }

    private void MaterializeBackgroundPlanes(Level level)
    {
        var levelBackgroundPlanes = level.BackgroundPlanes
            .Select(kv => kv.Value.TextureName)
            .Distinct();

        var spriteFrames = new Dictionary<string, SpriteFrames>();
        var imageTextures = new Dictionary<string, ImageTexture>();

        foreach (var name in levelBackgroundPlanes)
        {
            try
            {
                var animatedTexture = ContentLoader.LoadBackgroundPlaneAnimated(name);
                spriteFrames.Add(name, animatedTexture.ToSpriteFrames());
            }
            catch (Exception)
            {
                var texture2D = ContentLoader.LoadBackgroundPlane(name);
                imageTextures.Add(name, texture2D.ToImageTexture());
            }
        }

        var backgroundPlanes = new Node3D { Name = "BackgroundPlanes" };
        foreach ((int key, var plane) in level.BackgroundPlanes)
        {
            SpriteBase3D child = null;
            if (spriteFrames.TryGetValue(plane.TextureName, out var frames))
            {
                child = new AnimatedSprite3D
                {
                    Name = $"{key}_{plane.TextureName}",
                    Animation = TextureExtensions.DefaultAnimation,
                    Autoplay = TextureExtensions.DefaultAnimation,
                    SpriteFrames = frames
                };
            }
            else if (imageTextures.TryGetValue(plane.TextureName, out var texture))
            {
                // Fixes z-fighting with Art Objects
                var material = new ShaderMaterial();
                material.Shader = ResourceLoader.Load<Shader>(BackgroundPlaneShader);
                material.SetShaderParameter("TEXTURE", texture);

                child = new Sprite3D
                {
                    Name = $"{key}_{plane.TextureName}", Texture = texture, MaterialOverlay = material
                };
            }

            if (child == null)
            {
                continue;
            }

            backgroundPlanes.AddChild(child);
            child.PixelSize = Mathz.PixelSize;
            child.Billboard = plane.Billboard
                ? BaseMaterial3D.BillboardModeEnum.FixedY
                : BaseMaterial3D.BillboardModeEnum.Disabled;
            child.Shaded = true;
            child.DoubleSided = plane.Doublesided;
            child.AlphaCut = SpriteBase3D.AlphaCutMode.Discard;
            child.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
            child.Modulate = new Color(plane.Filter.ToGodot(), plane.Opacity);
            child.Position = plane.Position.ToGodot();
            child.Quaternion = plane.Rotation.ToGodot();
            child.Scale = plane.Scale.ToGodot();
            child.AddChild(MaterializerProxy.CreateFromBox(plane, child.GetAabb().Size));
        }

        AddChild(backgroundPlanes);
    }

    private void MaterializeCharacters(Level level)
    {
        var levelCharacters = level.NonPlayerCharacters
            .Select(kv => kv.Value.Name)
            .Distinct();

        var spriteFrames = new Dictionary<string, SpriteFrames>();
        foreach (var name in levelCharacters)
        {
            var animations = ContentLoader.LoadCharacterAnimations(name);
            spriteFrames.Add(name, animations.ToSpriteFrames());
        }

        var characters = new Node3D { Name = "Characters" };
        foreach ((int key, var instance) in level.NonPlayerCharacters)
        {
            var animatedSprite = new AnimatedSprite3D
            {
                Name = $"{key}_{instance.Name}",
                SpriteFrames = spriteFrames[instance.Name],
                PixelSize = Mathz.PixelSize,
                Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
                Shaded = true,
                DoubleSided = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Position = instance.Position.ToGodot()
            };

            animatedSprite.AddChild(MaterializerProxy.CreateFromBox(instance, animatedSprite.GetAabb().Size));
            characters.AddChild(animatedSprite);
        }

        AddChild(characters);
    }
}