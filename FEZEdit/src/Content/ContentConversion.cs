using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Core;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.Graphics;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;

namespace FEZEdit.Content;

using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

public static class ContentConversion
{
    private const string DefaultAnimationName = "default";

    private const string TrixelMaterialShader = "res://src/Shaders/TrixelMaterial.gdshader";

    private const string MissingTexture = "res://assets/textures/Missing.png";
    
    private static readonly Dictionary<CollisionType, string> CollisionTextures = new()
    {
        [CollisionType.AllSides] = "res://assets/textures/AllSides.png",
        [CollisionType.TopOnly] = "res://assets/textures/TopOnly.png",
        [CollisionType.None] =  "res://assets/textures/None.png",
        [CollisionType.Immaterial] =  "res://assets/textures/Immaterial.png",
        [CollisionType.TopNoStraightLedge] = "res://assets/textures/TopNoStraightLedge.png"
    };

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
        if (mesh != null) mesh.ResourceName = artObject.Name;
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

    public static Mesh CreateCollisionMesh(Dictionary<FaceOrientation, CollisionType> faces, Vector3 size, float alpha)
    {
        var arrayMesh = new ArrayMesh();
        
        foreach ((FaceOrientation face, var collision) in faces)
        {
            var vertices = new Vector3[4];
            var normals = new Vector3[4];

            switch (face)
            {
                case FaceOrientation.Front:
                    vertices[0] = new Vector3(-1, -1, 1);
                    vertices[1] = new Vector3(1, -1, 1);
                    vertices[2] = new Vector3(1, 1, 1);
                    vertices[3] = new Vector3(-1, 1, 1);
                    normals[0] = normals[1] = normals[2] = normals[3] = new Vector3(0, 0, 1);
                    break;
                case FaceOrientation.Back:
                    vertices[0] = new Vector3(1, -1, -1);
                    vertices[1] = new Vector3(-1, -1, -1);
                    vertices[2] = new Vector3(-1, 1, -1);
                    vertices[3] = new Vector3(1, 1, -1);
                    normals[0] = normals[1] = normals[2] = normals[3] = new Vector3(0, 0, -1);
                    break;
                case FaceOrientation.Left:
                    vertices[0] = new Vector3(-1, -1, -1);
                    vertices[1] = new Vector3(-1, -1, 1);
                    vertices[2] = new Vector3(-1, 1, 1);
                    vertices[3] = new Vector3(-1, 1, -1);
                    normals[0] = normals[1] = normals[2] = normals[3] = new Vector3(-1, 0, 0);
                    break;
                case FaceOrientation.Right:
                    vertices[0] = new Vector3(1, -1, 1);
                    vertices[1] = new Vector3(1, -1, -1);
                    vertices[2] = new Vector3(1, 1, -1);
                    vertices[3] = new Vector3(1, 1, 1);
                    normals[0] = normals[1] = normals[2] = normals[3] = new Vector3(1, 0, 0);
                    break;
            }
            
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= 0.5f;    // Scale down to unit size
                vertices[i] *= size;
            }

            var uvs = new Vector2[]
            {
                new(0, 1), // bottom-left
                new(1, 1), // bottom-right
                new(1, 0), // top-right
                new(0, 0)  // top-left
            };

            var indices = new[] { 2, 1, 0, 3, 2, 0 };
            
            var meshData = new Godot.Collections.Array();
            meshData.Resize((int)Mesh.ArrayType.Max);
            meshData[(int)Mesh.ArrayType.Vertex] = vertices;
            meshData[(int)Mesh.ArrayType.Normal] = normals;
            meshData[(int)Mesh.ArrayType.TexUV] = uvs;
            meshData[(int)Mesh.ArrayType.Index] = indices;
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshData);

            var texture = CollisionTextures.GetValueOrDefault(collision, MissingTexture);

            var material = new StandardMaterial3D();
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
            material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            material.AlbedoTexture = ResourceLoader.Load<Godot.Texture2D>(texture);
            material.AlbedoColor = new Color(Colors.White, Mathf.Clamp(alpha, 0f, 1f));
            arrayMesh.SurfaceSetMaterial(arrayMesh.GetSurfaceCount() - 1, material);
        }
        
        return arrayMesh;
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