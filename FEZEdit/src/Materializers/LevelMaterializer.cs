using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Core;
using FEZEdit.Extensions;
using FEZRepacker.Core.Definitions.Game.Level;
using Godot;
using Mesh = Godot.Mesh;

namespace FEZEdit.Materializers;

public class LevelMaterializer : Materializer<Level, Node3D>
{
    private const float PixelSize = 1f / 16f;

    private const string BackgroundPlaneShader = "res://src/Shaders/BackgroundPlane.gdshader";

    private static readonly Orthogonal[] RotationIndices =
    [
        Orthogonal.BackUp,
        Orthogonal.LeftUp,
        Orthogonal.FrontUp,
        Orthogonal.RightUp
    ];

    protected override Node3D Materialize(Level level)
    {
        GameTypeRelations.Clear();
        var node = new Node3D { Name = level.Name };

        node.AddChild(MaterializeTriles(level));
        node.AddChild(MaterializeArtObjects(level));
        node.AddChild(MaterializeBackgroundPlanes(level));
        node.AddChild(MaterializeCharacters(level));

        return node;
    }

    private TrileMap MaterializeTriles(Level level)
    {
        var trileSet = AssetLoader.LoadTrileSet(level.TrileSetName);
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

        GameTypeRelations.TryAdd(gridMap, triles);
        return gridMap;
    }

    private Node3D MaterializeArtObjects(Level level)
    {
        var levelArtObjects = level.ArtObjects
            .Select(kv => kv.Value.Name)
            .Distinct();

        var meshes = new Dictionary<string, Mesh>();
        foreach (var name in levelArtObjects)
        {
            var artObject = AssetLoader.LoadArtObject(name);
            var material = artObject.Cubemap.ToGodotMaterial();
            var mesh = artObject.Geometry.ToGodotMesh(material);
            meshes.Add(name, mesh);
        }

        var node3D = new Node3D { Name = "ArtObjects" };
        foreach ((int key, var instance) in level.ArtObjects)
        {
            var meshInstance = new MeshInstance3D { Name = $"{key}_{instance.Name}", Mesh = meshes[instance.Name] };
            node3D.AddChild(meshInstance);
            meshInstance.Position = instance.Position.ToGodot();
            meshInstance.Quaternion = instance.Rotation.ToGodot();
            meshInstance.Scale = instance.Scale.ToGodot();

            var staticBody = new StaticBody3D();
            meshInstance.AddChild(staticBody);
            var collisionShape = new CollisionShape3D { Shape = meshes[instance.Name].CreateConvexShape() };
            staticBody.AddChild(collisionShape);
            GameTypeRelations.TryAdd(staticBody, instance);
        }

        return node3D;
    }

    private Node3D MaterializeBackgroundPlanes(Level level)
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
                var animatedTexture = AssetLoader.LoadAnimatedBackgroundPlane(name);
                spriteFrames.Add(name, animatedTexture.ToSpriteFrames());
            }
            catch (Exception)
            {
                var texture2D = AssetLoader.LoadBackgroundPlane(name);
                imageTextures.Add(name, texture2D.ToImageTexture());
            }
        }

        var node3D = new Node3D { Name = "BackgroundPlanes" };
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

            node3D.AddChild(child);
            child.PixelSize = PixelSize;
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
            
            var staticBody = new StaticBody3D();
            child.AddChild(staticBody);
            var collisionShape = new CollisionShape3D { Shape = new BoxShape3D { Size = child.GetAabb().Size } };
            staticBody.AddChild(collisionShape);
            GameTypeRelations.TryAdd(staticBody, plane);
        }

        return node3D;
    }

    private Node3D MaterializeCharacters(Level level)
    {
        var levelCharacters = level.NonPlayerCharacters
            .Select(kv => kv.Value.Name)
            .Distinct();

        var spriteFrames = new Dictionary<string, SpriteFrames>();
        foreach (var name in levelCharacters)
        {
            var animations = AssetLoader.LoadCharacterAnimations(name);
            spriteFrames.Add(name, animations.ToSpriteFrames());
        }

        var node3D = new Node3D { Name = "Characters" };
        foreach ((int key, var instance) in level.NonPlayerCharacters)
        {
            var animatedSprite = new AnimatedSprite3D
            {
                Name = $"{key}_{instance.Name}",
                SpriteFrames = spriteFrames[instance.Name],
                PixelSize = PixelSize,
                Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
                Shaded = true,
                DoubleSided = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Position = instance.Position.ToGodot()
            };
            node3D.AddChild(animatedSprite);

            var staticBody = new StaticBody3D();
            animatedSprite.AddChild(staticBody);
            var collisionShape = new CollisionShape3D { Shape = new BoxShape3D { Size = animatedSprite.GetAabb().Size } };
            staticBody.AddChild(collisionShape);
            GameTypeRelations.TryAdd(staticBody, instance);
        }

        return node3D;
    }
}