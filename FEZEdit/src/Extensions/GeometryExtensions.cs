using System.Linq;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.Graphics;
using Godot;

namespace FEZEdit.Extensions;

internal static class GeometryExtensions
{
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
}