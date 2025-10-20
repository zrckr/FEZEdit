using System;
using FEZEdit.Extensions;
using FEZEdit.Main;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.MapTree;
using Godot;

namespace FEZEdit.Editors.Jenna;

public partial class JennaEditor : Editor
{
    public enum Options
    {
        AddChildNode,
        RemoveNode
    }

    public override event Action ValueChanged;

    public override object Value
    {
        get => _mapTree;
        set => _mapTree = (MapTree)value;
    }

    public override bool Disabled
    {
        set
        {
            _inspector.Disabled = value;
        }
    }

    [Export] private IconsResource _icons;

    private MapTree _mapTree;

    private PopupMenu _contextMenu;

    private PopupMenu _addChildNodeMenu;

    private JennaCamera _camera;

    private JennaMaterializer _materializer;

    private Inspector _inspector;

    private object _inspectedObject;
    
    private MapNode _selectedMapNode;

    public override void _Ready()
    {
        InitializeSubViewport();
        InitializeContextMenu();
        InitializeMaterializer();
        InitializeInspector();
    }

    private void InitializeSubViewport()
    {
        _camera = GetNode<JennaCamera>("%Camera");
        _camera.ResultClickedLeft += ShowPropertiesInInspector;
        _camera.ResultClickedRight += ShowContextMenu;
    }

    private void InitializeContextMenu()
    {
        _contextMenu = GetNode<PopupMenu>("%ContextMenu");
        _contextMenu.IdPressed += id => RemoveNode((Options)id);
        
        _addChildNodeMenu = _contextMenu.GetNode<PopupMenu>("AddChildNode");
        _addChildNodeMenu.IndexPressed += face => AddMapNode((FaceOrientation)face);
        foreach (var face in Enum.GetNames<FaceOrientation>())
        {
            _addChildNodeMenu.AddItem(Tr(face));
        }

        _contextMenu.AddSubmenuNodeItem(Tr("Add Child Node..."), _addChildNodeMenu, (int)Options.AddChildNode);
        _contextMenu.AddItem(Tr("Remove node"), (int)Options.RemoveNode);
        _contextMenu.SetItemIcon((int)Options.AddChildNode, _icons.ActionAdd);
        _contextMenu.SetItemIcon((int)Options.RemoveNode, _icons.ActionRemove);
    }

    private void InitializeMaterializer()
    {
        if (_mapTree == null)
        {
            return;
        }
        
        GetNode("%WorldMap").QueueFree();
        _materializer = new JennaMaterializer();
        _camera.AddSibling(_materializer, true);
        
        _materializer.Initialize(_mapTree);
        _camera.SetTarget(_materializer, false);
    }

    private void InitializeInspector()
    {
        _inspector = GetNode<Inspector>("%Inspector");
        _inspector.UndoRedo = UndoRedo;
        _inspector.TargetChanged += UpdateObjectFromProperties;
    }

    private void ShowPropertiesInInspector(object source)
    {
        if (_inspectedObject is MapNode oldNode)
        {
            _materializer.HighlightNode(oldNode, false);
        }
        if (source is MapNode newNode)
        {
            _materializer.HighlightNode(newNode, true);
        }
        
        if (_inspectedObject != source)
        {
            _inspectedObject = source;
            switch (source)
            {
                case MapNode mapNode:
                    InspectMapNode(mapNode);
                    break;
                case MapNodeConnection connection:
                    InspectMapConnection(connection);
                    break;
            }
        }
    }

    private void UpdateObjectFromProperties(object target)
    {
        if (target != null && _inspectedObject != null)
        {
            switch (target)
            {
                case MapNode node:
                    _materializer.UpdateMapNode(node);
                    ValueChanged?.Invoke();
                    break;
                
                case MapNodeConnection connection:
                    _materializer.UpdateMapNode(connection.Node);
                    ValueChanged?.Invoke();
                    break;
            }
        }
    }

    private void ShowContextMenu(object @object)
    {
        if (@object is MapNode node)
        {
            _selectedMapNode = node;
            
            (_, MapNodeConnection parentConnection) = _mapTree.FindParentWithConnection(node);
            var parentFace = parentConnection?.Face.GetOpposite();
            var faces = Enum.GetValues<FaceOrientation>();
            
            for (int i = 0; i < faces.Length; i++)
            {
                _addChildNodeMenu.SetItemDisabled(i, faces[i] == parentFace);
            }
            
            _contextMenu.Position = (Vector2I)GetGlobalMousePosition();
            _contextMenu.Popup();
        }
    }

    private void RemoveNode(Options options)
    {
        if (options == Options.RemoveNode && _selectedMapNode != null)
        {
            (MapNode parent, MapNodeConnection parentConnection) = _mapTree.FindParentWithConnection(_selectedMapNode);

            UndoRedo.CreateAction("Remove Map Node", _materializer);
            UndoRedo.AddDoMethod(() =>
            {
                _materializer.RemoveMapNode(_selectedMapNode);
                ValueChanged?.Invoke();
            });
            UndoRedo.AddUndoMethod(() =>
            {
                _materializer.AddMapNode(parent, _selectedMapNode, parentConnection.Face);
                ValueChanged?.Invoke();
            });
            UndoRedo.CommitAction();
        }
    }

    private void AddMapNode(FaceOrientation orientation)
    {
        if (_selectedMapNode != null)
        {
            var newNode = new MapNode { LevelName = "UNTITLED" };
            
            UndoRedo.CreateAction("Add Map Node", _materializer);
            UndoRedo.AddDoMethod(() =>
            {
                _materializer.AddMapNode(_selectedMapNode, newNode, orientation);
                ValueChanged?.Invoke();
            });
            UndoRedo.AddUndoMethod(() =>
            {
                _materializer.RemoveMapNode(newNode);
                ValueChanged?.Invoke();
            });
            UndoRedo.CommitAction();
        }
    }

    private void InspectMapNode(MapNode mapNode)
    {
        _inspector.ClearProperties();
        _inspector.InspectProperty(mapNode, nameof(MapNode.LevelName)); 
        _inspector.InspectProperty(mapNode, nameof(MapNode.NodeType)); 
        _inspector.InspectProperty(mapNode, nameof(MapNode.HasLesserGate)); 
        _inspector.InspectProperty(mapNode, nameof(MapNode.HasWarpGate)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.ChestCount)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.LockedDoorCount)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.UnlockedDoorCount)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.ScriptIds)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.CubeShardCount)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.OtherCollectibleCount)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.SplitUpCount)); 
        _inspector.InspectProperty(mapNode.Conditions, nameof(WinConditions.SecretCount));
    }

    private void InspectMapConnection(MapNodeConnection connection)
    {
        _inspector.ClearProperties();
        _inspector.InspectProperty(connection, nameof(MapNodeConnection.BranchOversize));
    }
}