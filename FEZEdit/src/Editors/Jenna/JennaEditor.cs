using System;
using System.Linq;
using FEZEdit.Extensions;
using FEZEdit.Main;
using FEZEdit.Memento;
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

    public override object Value
    {
        get => _mapTree;
        set
        {
            _mapTree = (MapTree)value;
            Memento = new MementoManager(_mapTree);
        }
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

    public override void _Refresh()
    { 
        _materializer.Update(_mapTree, _mapTree.Root);
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
        
        _materializer.Update(_mapTree, _mapTree.Root);
        _camera.SetTarget(_materializer, false);
    }

    private void InitializeInspector()
    {
        _inspector = GetNode<Inspector>("%Inspector");
        _inspector.Memento = Memento;
        _inspector.TargetChanged += UpdateMaterializerState;
    }

    private void ShowPropertiesInInspector(object source)
    {
        if (_inspectedObject is MapNode oldNode)
        {
            _materializer.Highlight(oldNode, false);
        }
        if (source is MapNode newNode)
        {
            _materializer.Highlight(newNode, true);
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

    private void UpdateMaterializerState(object target)
    {
        if (target != null && _inspectedObject != null)
        {
            switch (target)
            {
                case MapNode node:
                    _materializer.Update(_mapTree, node);
                    break;
                
                case MapNodeConnection connection:
                    _materializer.Update(_mapTree, connection.Node);
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
            (MapNode parent, _) = _mapTree.FindParentWithConnection(_selectedMapNode);
            var connection = parent?.Connections.FirstOrDefault(c => c.Node == _selectedMapNode);
            if (connection != null)
            {
                using (Memento.BeginScope("Remove Map Node"))
                {
                    parent.Connections.Remove(connection);
                    _materializer.Update(_mapTree, parent);
                }
            }
        }
    }

    private void AddMapNode(FaceOrientation orientation)
    {
        if (_selectedMapNode != null)
        {
            using (Memento.BeginScope("Add Map Node"))
            {
                var newNode = new MapNode { LevelName = "UNTITLED" };
                _selectedMapNode.Connections.Add(new MapNodeConnection { Node = newNode, Face = orientation });
                _materializer.Update(_mapTree, _selectedMapNode);
            }
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