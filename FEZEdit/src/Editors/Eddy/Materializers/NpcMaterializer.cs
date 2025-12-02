using System.Collections.Generic;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Memento;
using FEZRepacker.Core.Definitions.Game.Level;
using Godot;

namespace FEZEdit.Editors.Eddy.Materializers;

public partial class NpcMaterializer : Node, IMaterializer
{
    private static readonly Vector3 Offset = Vector3.Up / 2f;

    private readonly Dictionary<int, AnimatedSprite3D> _animatedSpriteInstances = new();

    private readonly Dictionary<int, MaterializerProxy> _proxies = new();

    private readonly Dictionary<string, AnimatedSprite3D> _uniqueInstances = new();

    private Dictionary<int, NpcInstance> _npcInstances;

    public MementoManager Memento { private get; set; }

    public static IMaterializer Create(Dictionary<int, NpcInstance> npcInstances)
    {
        var materializer = new NpcMaterializer();
        materializer._npcInstances = npcInstances;
        return materializer;
    }

    public override void _Ready()
    {
        Name = nameof(NpcMaterializer);
        ClearInstances();
        UpdateInstances();
    }

    public GeometryInstance3D GetCursorInstance(string path)
    {
        if (!_uniqueInstances.TryGetValue(path, out var instance))
        {
            var characterAnimations = ContentLoader.LoadCharacterAnimations(path);
            var frames = ContentConversion.ConvertToSpriteFrames(characterAnimations.Animations);
            instance = new AnimatedSprite3D
            {
                SpriteFrames = frames,
                PixelSize = Mathz.PixelSize,
                Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
            };
        }

        return instance;
    }

    public void CreateInstance(string path, Transform3D emplacement)
    {
        using (Memento.BeginScope("Create NPC"))
        {
            var instance = new NpcInstance { Name = path, Position = emplacement.Origin.ToXna() };

            var nextId = _npcInstances.Keys.Max() + 1;
            _npcInstances[nextId] = instance;
            UpdateInstances();
        }
    }

    public void RemoveInstance(Transform3D emplacement)
    {
        var position = emplacement.Origin.Floor();
        var id = _npcInstances
            .FirstOrDefault(ao => ao.Value.Position.ToGodot().IsEqualApprox(position))
            .Key;

        using (Memento.BeginScope("Remove NPC"))
        {
            _npcInstances.Remove(id);
            UpdateInstances();
        }
    }

    public string PickInstance(Transform3D emplacement)
    {
        var position = emplacement.Origin.Floor();
        var id = _npcInstances
            .FirstOrDefault(ao => ao.Value.Position.ToGodot().IsEqualApprox(position))
            .Key;

        return _npcInstances[id].Name;
    }

    public void SetTransform(int id, Transform3D transform)
    {
        if (_npcInstances.TryGetValue(id, out var npc))
        {
            npc.Position = transform.Origin.ToXna();
        }

        if (_animatedSpriteInstances.TryGetValue(id, out var animatedSprite))
        {
            animatedSprite.Transform = transform;
        }
    }

    private void UpdateInstances()
    {
        var uniqueCharacters = _npcInstances
            .Select(kv => kv.Value.Name)
            .Distinct();

        var spriteFrames = new Dictionary<string, SpriteFrames>();
        foreach (var name in uniqueCharacters)
        {
            var characterAnimations = ContentLoader.LoadCharacterAnimations(name);
            var frames = ContentConversion.ConvertToSpriteFrames(characterAnimations.Animations);
            spriteFrames.Add(name, frames);
        }

        _uniqueInstances.Clear();
        foreach ((int key, var instance) in _npcInstances)
        {
            var frames = spriteFrames[instance.Name];
            var animatedSprite = new AnimatedSprite3D
            {
                SpriteFrames = frames,
                PixelSize = Mathz.PixelSize,
                Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Position = instance.Position.ToGodot() + Offset
            };
            _animatedSpriteInstances.Add(key, animatedSprite);
            _uniqueInstances.TryAdd(instance.Name, animatedSprite.Duplicate() as AnimatedSprite3D);
            AddChild(animatedSprite, true);

            var proxy = MaterializerProxy.CreateFromBox(instance, animatedSprite.GetAabb().Size);
            _proxies.Add(key, proxy);
            animatedSprite.AddChild(proxy, true);

            if (frames.HasAnimation("idle"))
            {
                animatedSprite.Play("idle");
            }
            else if (frames.HasAnimation("walk"))
            {
                animatedSprite.Play("walk");
            }
            else
            {
                animatedSprite.Play();
            }
        }
    }

    private void ClearInstances()
    {
        foreach (var proxy in _proxies.Values)
        {
            proxy.QueueFree();
        }

        foreach (var instance in _animatedSpriteInstances.Values)
        {
            instance.QueueFree();
        }

        _animatedSpriteInstances.Clear();
        _proxies.Clear();
    }
}