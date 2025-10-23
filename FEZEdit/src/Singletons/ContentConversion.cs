using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Core;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.Graphics;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;

namespace FEZEdit.Singletons;

using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

public static class ContentConversion
{
    private const string DefaultAnimationName = "default";

    private const string TrixelMaterialShader = "res://src/Shaders/TrixelMaterial.gdshader";

    public static IDictionary<string, Mesh> ConvertToMesh(TrileSet trileSet)
    {
        var texture = ConvertToTexture(trileSet.TextureAtlas);
        var material = CreateTrixelMaterial(texture);

        var meshes = new Dictionary<string, Mesh>();
        foreach (var trile in trileSet.Triles.Values)
        {
            var mesh = CreateArrayMesh(trile.Geometry, material);
            if (mesh != null) mesh.ResourceName = trile.Name;
            meshes.Add(trile.Name, mesh);
        }

        return meshes;
    }

    public static Mesh ConvertToMesh(ArtObject artObject)
    {
        var texture = ConvertToTexture(artObject.Cubemap);
        var material = CreateTrixelMaterial(texture);
        var mesh = CreateArrayMesh(artObject.Geometry, material);
        mesh.ResourceName = artObject.Name;
        return mesh;
    }

    public static Mesh ConvertToMesh(Texture2D texture2D)
    {
        var size = new Vector2(texture2D.Width, texture2D.Height);
        var texture = ConvertToTexture(texture2D);
        var mesh = CreatePlaneMesh(texture, size);
        return mesh;
    }

    public static Mesh ConvertToMesh(AnimatedTexture animatedTexture)
    {
        var size = new Vector2(animatedTexture.FrameWidth, animatedTexture.FrameHeight);
        var region = animatedTexture.Frames.First().Rectangle.ToGodot();
        var texture = ConvertToTexture(animatedTexture, region);
        var mesh = CreatePlaneMesh(texture, size);
        return mesh;
    }

    public static ImageTexture ConvertToTexture(Texture2D texture2D)
    {
        var image = Image.CreateFromData(
            texture2D.Width,
            texture2D.Height,
            false,
            Image.Format.Rgba8,
            texture2D.TextureData);
        return ImageTexture.CreateFromImage(image);
    }

    public static ImageTexture ConvertToTexture(AnimatedTexture animatedTexture, Rect2I? region = null)
    {
        var image = Image.CreateFromData(
            animatedTexture.AtlasWidth,
            animatedTexture.AtlasHeight,
            false,
            Image.Format.Rgba8,
            animatedTexture.TextureData);

        if (region.HasValue)
        {
            image = image.GetRegion(region.Value);
        }

        return ImageTexture.CreateFromImage(image);
    }

    public static SpriteFrames ConvertToSpriteFrames(AnimatedTexture animatedTexture)
    {
        var totalDuration = new TimeSpan(animatedTexture.Frames.Sum(f => f.Duration.Ticks));
        var fps = animatedTexture.Frames.Count / totalDuration.TotalSeconds;
        
        var spriteFrames = new SpriteFrames();
        spriteFrames.SetAnimationSpeed(DefaultAnimationName, fps);
        spriteFrames.SetAnimationLoop(DefaultAnimationName, true);
        
        var atlas = ConvertToTexture(animatedTexture);
        foreach (var frame in animatedTexture.Frames)
        {
            var atlasTexture = new AtlasTexture { Atlas = atlas, Region = frame.Rectangle.ToGodot() };
            var duration = (float)(frame.Duration / totalDuration);
            spriteFrames.AddFrame(DefaultAnimationName, atlasTexture, duration);
        }

        return spriteFrames;
    }

    public static SpriteFrames ConvertToSpriteFrames(IDictionary<string, AnimatedTexture> animatedTextures)
    {
        var spriteFrames = new SpriteFrames();
        spriteFrames.RemoveAnimation(DefaultAnimationName);

        foreach ((string name, var animatedTexture) in animatedTextures)
        {
            var totalDuration = new TimeSpan(animatedTexture.Frames.Sum(f => f.Duration.Ticks));
            var fps = animatedTexture.Frames.Count / totalDuration.TotalSeconds;
            
            spriteFrames.AddAnimation(name);
            spriteFrames.SetAnimationSpeed(name, fps);
            
            var atlas = ConvertToTexture(animatedTexture);
            foreach (var frame in animatedTexture.Frames)
            {
                var atlasTexture = new AtlasTexture { Atlas = atlas, Region = frame.Rectangle.ToGodot() };
                var duration = (float)(frame.Duration / totalDuration);
                spriteFrames.AddFrame(name, atlasTexture, duration);
            }
        }
        
        return spriteFrames;
    }

    private static ArrayMesh CreateArrayMesh<T>(IndexedPrimitives<VertexInstance, T> geometry, Material material)
    {
        if (geometry.Vertices.Length < 1)
        {
            return null;
        }

        var geometryPrimitiveType = geometry.PrimitiveType.ToGodot();
        var geometryVertices = geometry.Vertices.Select(vi => vi.Position.ToGodot()).ToArray();
        var geometryNormals = geometry.Vertices.Select(vi => vi.Normal.ToGodot()).ToArray();
        var geometryTexCoords = geometry.Vertices.Select(vi => vi.TextureCoordinate.ToGodot()).ToArray();
        var geometryIndices = geometry.Indices.Select(i => (int)i).ToArray();

        var vertices = new Vector3[geometryIndices.Length]; // PackedVector3Array
        var normals = new Vector3[geometryIndices.Length]; // PackedVector3Array
        var texCoords = new Vector2[geometryIndices.Length]; // PackedVector2Array

        var pairSize = geometryPrimitiveType is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip
            ? 2
            : 1;
        var k = 0;

        for (var i = 0; i < geometryIndices.Length; i += 3)
        {
            for (var j = 0; j <= pairSize; j++)
            {
                var face = geometryIndices[i + j];
                vertices[k] = geometryVertices[face];
                normals[k] = geometryNormals[face];
                texCoords[k] = geometryTexCoords[face];
                k++;
            }
        }

        var meshData = new Godot.Collections.Array();
        meshData.Resize((int)Mesh.ArrayType.Max);
        meshData[(int)Mesh.ArrayType.Vertex] = vertices;
        meshData[(int)Mesh.ArrayType.Normal] = normals;
        meshData[(int)Mesh.ArrayType.TexUV] = texCoords;

        var arrayMesh = new ArrayMesh();
        arrayMesh.ClearSurfaces();
        arrayMesh.AddSurfaceFromArrays(geometryPrimitiveType, meshData);
        arrayMesh.SurfaceSetMaterial(0, material);

        return arrayMesh;
    }

    private static PlaneMesh CreatePlaneMesh(ImageTexture imageTexture, Vector2 size)
    {
        return new PlaneMesh
        {
            Orientation = PlaneMesh.OrientationEnum.Z,
            Size = size * Mathz.PixelSize,
            Material = new StandardMaterial3D
            {
                AlbedoTexture = imageTexture, TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
            }
        };
    }

    private static Material CreateTrixelMaterial(ImageTexture imageTexture)
    {
        var material = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(TrixelMaterialShader) };
        material.SetShaderParameter("texture_albedo", imageTexture);
        return material;
    }
}