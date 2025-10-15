using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Extensions;
using FEZEdit.Loaders;
using FEZEdit.Materializers;
using FEZRepacker.Core.Definitions.Game.MapTree;
using Godot;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

namespace FEZEdit.Interface.Editors.Jenna;

public partial class JennaNode : MeshInstance3D
{
    private const string MapNodeShader = "res://src/Shaders/MapNode.gdshader";

    private const string OutlineShader = "res://src/Shaders/Outline.gdshader";

    private const string MapIconsTexture = "res://assets/MapIcons.png";

    private const string MissingTexture = "res://assets/Empty.png";

    private const float OutlineMultiplier = 20f;

    public const float LinkThickness = 0.05375f;

    private static readonly string[] IconNames =
    [
        "HasWarpGate",
        "HasLesserGate",
        "ChestCount",
        "LockedDoorCount",
        "CubeShardCount",
        "SplitUpCount",
        "SecretCount"
    ];

    public float OutlineSize
    {
        get => _outlineSize;
        set
        {
            _outlineSize = value;
            _outlineMaterial.SetShaderParameter("size", LinkThickness * OutlineMultiplier * _outlineSize);
        }
    }

    public IEnumerable<CollisionObject3D> Links => _linksNode.GetChildren().OfType<CollisionObject3D>();

    public IEnumerable<Sprite3D> Icons => _iconsNode.GetChildren().OfType<Sprite3D>();

    public IEnumerable<JennaNode> Nodes => _nodesNode.GetChildren().OfType<JennaNode>();

    public JennaNode Parent { get; private set; }

    private Node3D _linksNode;

    private Node3D _iconsNode;

    private Node3D _nodesNode;

    private ShaderMaterial _mapNodeMaterial;

    private ShaderMaterial _outlineMaterial;

    private Tween _tween;

    private float _outlineSize = 1.0f;

    public JennaNode()
    {
        _linksNode = new Node3D { Name = "Links" };
        AddChild(_linksNode);

        _iconsNode = new Node3D { Name = "Icons" };
        AddChild(_iconsNode);
        for (int i = 0; i < IconNames.Length; i++)
        {
            var iconName = IconNames[i];
            var iconSprite = new Sprite3D
            {
                Name = iconName,
                PixelSize = 0.02f,
                Hframes = IconNames.Length,
                Frame = i,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                DoubleSided = false,
                Texture = ResourceLoader.Load<Godot.Texture2D>(MapIconsTexture),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Visible = false
            };
            _iconsNode.AddChild(iconSprite);
        }

        _nodesNode = new Node3D { Name = "Nodes" };
        AddChild(_nodesNode);
    }

    public static JennaNode Create(ILoader loader, MapNode node)
    {
        var jennaNode = new JennaNode { Name = node.LevelName };
        var jennaSize = node.NodeType.GetSizeFactor();

        var mapNodeShader = ResourceLoader.Load<Shader>(MapNodeShader);
        jennaNode._mapNodeMaterial = new ShaderMaterial { Shader = mapNodeShader };
        try
        {
            var mapNodeTexturePath = Path.Combine("other textures", "map_screens", node.LevelName.ToLower());
            var mapNodeTexture = (Texture2D)loader.LoadAsset(mapNodeTexturePath);
            jennaNode._mapNodeMaterial.SetShaderParameter("texture_albedo", mapNodeTexture.ToImageTexture());
        }
        catch
        {
            var missingTexture = ResourceLoader.Load<Godot.Texture2D>(MissingTexture);
            jennaNode._mapNodeMaterial.SetShaderParameter("texture_albedo", missingTexture);
        }
        jennaNode._mapNodeMaterial.SetShaderParameter("texture_scale", 1f / jennaSize);

        var outlineShader = ResourceLoader.Load<Shader>(OutlineShader);
        jennaNode._outlineMaterial = new ShaderMaterial { Shader = outlineShader };
        jennaNode._outlineMaterial.SetShaderParameter("size", LinkThickness * OutlineMultiplier);
        jennaNode._mapNodeMaterial.NextPass = jennaNode._outlineMaterial;

        var mesh = new BoxMesh { Size = Vector3.One * jennaSize, Material = jennaNode._mapNodeMaterial };
        jennaNode.Mesh = mesh;
        jennaNode.AddChild(MaterializerProxy.CreateFromBox(node, mesh.Size));

        return jennaNode;
    }

    public void AddJennaChild(JennaNode node, Vector3 position)
    {
        _nodesNode.AddChild(node, true);
        node.Parent = this;
        node.GlobalPosition = position;
    }

    public void CreateLink(MapNodeConnection connection)
    {
        var linkNode = MaterializerProxy.CreateEmpty(connection);
        linkNode.Name = $"{connection.Face} ^ {connection.Node.LevelName}";
        _linksNode.AddChild(linkNode, true);
    }

    public void AddLinkBranch(Vector3 position, Vector3 scale)
    {
        var linkMesh = new BoxMesh { Size = Vector3.One * scale };
        var linkInstance = new MeshInstance3D { Mesh = linkMesh };
        var linkShape = new CollisionShape3D { Shape = linkMesh.CreateTrimeshShape() };
        Links.Last().AddChild(linkShape, true);
        linkShape.AddChild(linkInstance, true);
        linkShape.GlobalPosition = position;
    }

    public void UpdateMapIcons(MapNode node)
    {
        bool[] conditions =
        [
            node.HasWarpGate,
            node.HasLesserGate,
            node.Conditions.ChestCount > 0,
            node.Conditions.LockedDoorCount > 0,
            node.Conditions.CubeShardCount > 0,
            node.Conditions.SplitUpCount > 0,
            node.Conditions.SecretCount > 0
        ];

        var i = 0;
        var y = 0;
        foreach (var icon in Icons)
        {
            icon.Visible = false;
            icon.Position = Vector3.Zero;
            if (conditions[i])
            {
                icon.Visible = true;
                icon.Position = Vector3.Down * y / 2f;
                y++;
            }

            i++;
        }

        var size = node.NodeType.GetSizeFactor();
        _iconsNode.Position += Vector3.One * size / 1.5f;
    }

    public void SetHighlight(bool highlight)
    {
        if (_tween != null && _tween.IsValid())
        {
            _tween.Kill();
        }

        var value = highlight ? 1.25f : 1.0f;
        var easing = highlight ? Tween.EaseType.In : Tween.EaseType.Out;

        _tween = CreateTween();
        _tween.TweenProperty(this, nameof(OutlineSize), value, 0.25f)
            .SetEase(easing)
            .SetTrans(Tween.TransitionType.Cubic);
    }
}