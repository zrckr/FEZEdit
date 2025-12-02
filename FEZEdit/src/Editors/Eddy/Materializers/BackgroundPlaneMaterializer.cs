using System.Collections.Generic;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Memento;
using FEZRepacker.Core.Definitions.Game.Level;
using Godot;

namespace FEZEdit.Editors.Eddy.Materializers;

using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;
using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;

public partial class BackgroundPlaneMaterializer : Node, IMaterializer
{
    private const string BackgroundPlaneShader = "res://src/Shaders/BackgroundPlane.gdshader";

    private const string DefaultAnimation = "default";

    private readonly Dictionary<int, SpriteBase3D> _spriteInstances = new();

    private readonly Dictionary<int, MaterializerProxy> _proxies = new();

    private readonly Dictionary<string, SpriteBase3D> _uniqueInstances = new();

    private Dictionary<int, BackgroundPlane> _backgroundPlanes;

    public MementoManager Memento { private get; set; }

    public static IMaterializer Create(Dictionary<int, BackgroundPlane> backgroundPlanes)
    {
        var materializer = new BackgroundPlaneMaterializer();
        materializer._backgroundPlanes = backgroundPlanes;
        return materializer;
    }

    public override void _Ready()
    {
        Name = nameof(BackgroundPlaneMaterializer);
        ClearInstances();
        UpdateInstances();
    }

    public GeometryInstance3D GetCursorInstance(string path)
    {
        if (!_uniqueInstances.TryGetValue(path, out var instance))
        {
            var backgroundPlane = ContentLoader.LoadBackgroundPlane(path);
            instance = backgroundPlane.Match<SpriteBase3D>(
                texture2D =>
                {
                    var texture = ContentConversion.ConvertToTexture(texture2D);
                    var material = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(BackgroundPlaneShader) };
                    material.SetShaderParameter("texture_albedo", texture);
                    return new Sprite3D { Texture = texture, MaterialOverlay = material };
                },
                animatedTexture =>
                {
                    var frames = ContentConversion.ConvertToSpriteFrames(animatedTexture);
                    return new AnimatedSprite3D
                    {
                        Animation = DefaultAnimation, Autoplay = DefaultAnimation, SpriteFrames = frames
                    };
                }
            );
        }

        return instance;
    }

    public void CreateInstance(string path, Transform3D emplacement)
    {
        using (Memento.BeginScope("Create Background Plane"))
        {
            var instance = new BackgroundPlane
            {
                TextureName = path,
                Position = emplacement.Origin.ToXna(),
                Rotation = emplacement.Basis.GetRotationQuaternion().ToXna(),
                Scale = Vector3.One.ToXna()
            };

            var nextId = _backgroundPlanes.Keys.Max() + 1;
            _backgroundPlanes[nextId] = instance;
            UpdateInstances();
        }
    }

    public void RemoveInstance(Transform3D emplacement)
    {
        var position = emplacement.Origin.Floor();
        var id = _backgroundPlanes
            .FirstOrDefault(ao => ao.Value.Position.ToGodot().IsEqualApprox(position))
            .Key;

        using (Memento.BeginScope("Remove Background Plane"))
        {
            _backgroundPlanes.Remove(id);
            UpdateInstances();
        }
    }

    public string PickInstance(Transform3D emplacement)
    {
        var position = emplacement.Origin.Floor();
        var id = _backgroundPlanes
            .FirstOrDefault(ao => ao.Value.Position.ToGodot().IsEqualApprox(position))
            .Key;

        return _backgroundPlanes[id].TextureName;
    }

    public void SetTransform(int id, Transform3D transform)
    {
        if (_backgroundPlanes.TryGetValue(id, out var backgroundPlane))
        {
            backgroundPlane.Position = transform.Origin.ToXna();
            backgroundPlane.Rotation = transform.Basis.GetRotationQuaternion().ToXna();
            backgroundPlane.Scale = transform.Basis.Scale.ToXna();
        }

        if (_spriteInstances.TryGetValue(id, out var spriteInstance))
        {
            spriteInstance.Transform = transform;
        }
    }

    private void UpdateInstances()
    {
        var uniqueBackgroundPlanes = _backgroundPlanes
            .Select(kv => kv.Value.TextureName)
            .Distinct();

        var spriteFrames = new Dictionary<string, SpriteFrames>();
        var imageTextures = new Dictionary<string, ImageTexture>();

        foreach (var name in uniqueBackgroundPlanes)
        {
            var backgroundPlane = ContentLoader.LoadBackgroundPlane(name);
            backgroundPlane.Match(
                texture2D =>
                {
                    var imageTexture = ContentConversion.ConvertToTexture(texture2D);
                    imageTextures.Add(name, imageTexture);
                },
                animatedTexture =>
                {
                    var frames = ContentConversion.ConvertToSpriteFrames(animatedTexture);
                    spriteFrames.Add(name, frames);
                }
            );
        }

        _uniqueInstances.Clear();
        foreach ((int key, var plane) in _backgroundPlanes)
        {
            SpriteBase3D sprite = null;
            if (spriteFrames.TryGetValue(plane.TextureName, out var frames))
            {
                sprite = new AnimatedSprite3D
                {
                    Animation = DefaultAnimation, Autoplay = DefaultAnimation, SpriteFrames = frames
                };
            }
            else if (imageTextures.TryGetValue(plane.TextureName, out var texture))
            {
                // Fixes z-fighting with Art Objects
                var material = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(BackgroundPlaneShader) };
                material.SetShaderParameter("texture_albedo", texture);
                sprite = new Sprite3D { Texture = texture, MaterialOverlay = material };
            }

            if (sprite == null)
            {
                continue;
            }

            _spriteInstances.Add(key, sprite);
            _uniqueInstances.TryAdd(plane.TextureName, sprite.Duplicate() as SpriteBase3D);
            AddChild(sprite, true);

            sprite.PixelSize = Mathz.PixelSize;
            sprite.Billboard = plane.Billboard
                ? BaseMaterial3D.BillboardModeEnum.FixedY
                : BaseMaterial3D.BillboardModeEnum.Disabled;
            sprite.Shaded = true;
            sprite.DoubleSided = plane.Doublesided;
            sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Discard;
            sprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
            sprite.Modulate = new Color(plane.Filter.ToGodot(), plane.Opacity);
            sprite.Position = plane.Position.ToGodot();
            sprite.Quaternion = plane.Rotation.ToGodot();
            sprite.Scale = plane.Scale.ToGodot();

            var proxy = MaterializerProxy.CreateFromBox(plane, plane.Size.ToGodot());
            _proxies.Add(key, proxy);
            sprite.AddChild(proxy, true);
        }
    }

    private void ClearInstances()
    {
        foreach (var proxy in _proxies.Values)
        {
            proxy.QueueFree();
        }

        foreach (var instance in _spriteInstances.Values)
        {
            instance.QueueFree();
        }

        _spriteInstances.Clear();
        _proxies.Clear();
    }
}