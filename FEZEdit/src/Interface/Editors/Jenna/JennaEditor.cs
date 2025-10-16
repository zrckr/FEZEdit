using System;
using FEZEdit.Extensions;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.MapTree;
using Godot;

namespace FEZEdit.Interface.Editors.Jenna;

public partial class JennaEditor : TypedEditor<MapTree>
{
    public enum Options
    {
        AddChildNode,
        RemoveNode
    }

    public override MapTree TypedValue { get; set; }

    public override bool Disabled
    {
        set
        {
            _inspector.ShowDisabled = value;
        }
    }

    [Export] private IconsResource _icons;

    private PopupMenu _contextMenu;

    private PopupMenu _addChildNodeMenu;

    private JennaCamera _camera;

    private JennaMaterializer _materializer;

    private Inspector _inspector;

    private object _inspectedObject;
    
    private object _selectedObject;

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
        if (TypedValue == null)
        {
            return;
        }
        
        GetNode("%WorldMap").QueueFree();
        _materializer = new JennaMaterializer();
        _camera.AddSibling(_materializer, true);
        
        _materializer.Initialize(TypedValue, Loader);
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
            object properties = source switch
            {
                MapNode mapNode => MapNodeProperties.CopyFrom(mapNode),
                MapNodeConnection mapNodeConnection => ConnectionProperties.CopyFrom(mapNodeConnection),
                _ => null
            };
            _inspector.Inspect(properties);
            _inspectedObject = source;
        }
    }

    private void UpdateObjectFromProperties(object target)
    {
        if (target != null && _inspectedObject != null)
        {
            switch (target)
            {
                case MapNodeProperties properties when _inspectedObject is MapNode node:
                    properties.CopyTo(node);
                    _materializer.UpdateMapNode(node);
                    break;
                
                case ConnectionProperties properties when _inspectedObject is MapNodeConnection connection:
                    properties.CopyTo(connection);
                    _materializer.UpdateMapNode(connection.Node);
                    break;
            }
        }
    }

    private void ShowContextMenu(object @object)
    {
        if (@object != null)
        {
            _selectedObject = @object;
            _contextMenu.Position = (Vector2I)GetGlobalMousePosition();
            _contextMenu.Popup();
        }
    }

    private void RemoveNode(Options options)
    {
        if (options == Options.RemoveNode && _selectedObject is MapNode node)
        {
            (MapNode parent, MapNodeConnection parentConnection) = TypedValue.FindParentWithConnection(node);

            UndoRedo.CreateAction("Remove Map Node", _materializer);
            UndoRedo.AddDoMethod(() =>
            {
                _materializer.RemoveMapNode(node);
            });
            UndoRedo.AddUndoMethod(() =>
            {
                _materializer.AddMapNode(parent, node, parentConnection.Face);
            });
            UndoRedo.CommitAction();
        }
    }

    private void AddMapNode(FaceOrientation orientation)
    {
        if (_selectedObject is MapNode parentNode)
        {
            var newNode = new MapNode { LevelName = "Untitled" };
            
            UndoRedo.CreateAction("Add Map Node", _materializer);
            UndoRedo.AddDoMethod(() =>
            {
                _materializer.AddMapNode(parentNode, newNode, orientation);
            });
            UndoRedo.AddUndoMethod(() =>
            {
                _materializer.RemoveMapNode(newNode);
            });
            UndoRedo.CommitAction();
        }
    }
}