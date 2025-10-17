using System.Collections.Generic;
using System.Linq;
using FEZEdit.Core;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.Graphics;
using FEZRepacker.Core.Definitions.Game.XNA;
using Godot;

namespace FEZEdit.Extensions;

using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

internal static class ConversionExtensions
{
    private const string DefaultAnimation = "default";

    private const string ShaderPath = "res://src/Shaders/TrixelMaterial.gdshader";
    
    public static Mesh ToGodotMesh<T>(this IndexedPrimitives<VertexInstance, T> geometry, Material material)
    {
        if (geometry.Vertices.Length < 1)
        {
            return new Mesh();
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
    
    

    public static Material ToGodotMaterial(this FEZRepacker.Core.Definitions.Game.XNA.Texture2D texture2D)
    {
        var material = new ShaderMaterial();
        material.Shader = GD.Load<Shader>(ShaderPath);
        material.SetShaderParameter("TEXTURE", texture2D.ToImageTexture());
        return material;
    }

    public static FEZRepacker.Core.Definitions.Game.XNA.Texture2D ToXna(this ImageTexture texture)
    {
        return new FEZRepacker.Core.Definitions.Game.XNA.Texture2D
        {
            Format = SurfaceFormat.Color,
            Width = texture.GetWidth(),
            Height = texture.GetHeight(),
            MipmapLevels = 1,
            TextureData = texture.GetImage().GetData()
        };
    }

    public static ImageTexture ToImageTexture(this FEZRepacker.Core.Definitions.Game.XNA.Texture2D texture2D)
    {
        return ImageTexture.CreateFromImage(
            Image.CreateFromData(
                texture2D.Width,
                texture2D.Height,
                false,
                Image.Format.Rgba8,
                texture2D.TextureData)
        );
    }
    
    private static ImageTexture ToImageTexture(this FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture animatedTexture)
    {
        return ImageTexture.CreateFromImage(
            Image.CreateFromData(
                animatedTexture.AtlasWidth,
                animatedTexture.AtlasHeight,
                false,
                Image.Format.Rgba8,
                animatedTexture.TextureData)
        );
    }

    public static SpriteFrames ToSpriteFrames(
        this FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture animatedTexture)
    {
        var spriteFrames = new SpriteFrames();
        spriteFrames.SetAnimationSpeed(DefaultAnimation, animatedTexture.Frames.Count);
        spriteFrames.SetAnimationLoop(DefaultAnimation, true);

        foreach (var frame in animatedTexture.Frames)
        {
            var atlasTexture = new AtlasTexture();
            atlasTexture.Atlas = animatedTexture.ToImageTexture();
            atlasTexture.Region = frame.Rectangle.ToGodot();
            var duration = (float)frame.Duration.TotalSeconds * animatedTexture.Frames.Count;
            spriteFrames.AddFrame(DefaultAnimation, atlasTexture, duration);
        }

        return spriteFrames;
    }

    public static SpriteFrames ToSpriteFrames(
        this IDictionary<string, FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture> animatedTextures)
    {
        var spriteFrames = new SpriteFrames();
        spriteFrames.RemoveAnimation(DefaultAnimation);

        foreach ((string name, var animatedTexture) in animatedTextures)
        {
            spriteFrames.AddAnimation(name);
            spriteFrames.SetAnimationSpeed(name, animatedTexture.Frames.Count);
            
            foreach (var frame in animatedTexture.Frames)
            {
                var atlasTexture = new AtlasTexture();
                atlasTexture.Atlas = animatedTexture.ToImageTexture();
                atlasTexture.Region = frame.Rectangle.ToGodot();
                var duration = (float)frame.Duration.TotalSeconds * animatedTexture.Frames.Count;
                spriteFrames.AddFrame(name, atlasTexture, duration);
            }
        }
        
        return spriteFrames;
    }
}