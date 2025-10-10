using System;
using FEZEdit.Extensions;
using FEZEdit.Materializers;
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
            if (value)
            {
                _camera.ResultClickedLeft += ShowInspector;
                _camera.ResultClickedRight += ShowContextMenu;
            }
            else
            {
                _camera.ResultClickedLeft -= ShowInspector;
                _camera.ResultClickedRight -= ShowContextMenu;
            }
        }
    }

    [Export] private IconsResource _icons;

    private PopupMenu _contextMenu;

    private PopupMenu _addChildNodeMenu;

    private JennaCamera _camera;

    private MapTreeMaterializer _materializer;

    private Inspector _inspector;
    
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
        _camera.ResultClickedLeft += ShowInspector;
        _camera.ResultClickedRight += ShowContextMenu;
    }

    private void InitializeContextMenu()
    {
        _contextMenu = GetNode<PopupMenu>("%ContextMenu");
        _contextMenu.IdPressed += id => SelectOption((Options)id);
        _contextMenu.PopupHide += () => _selectedObject = null;
        
        _addChildNodeMenu = _contextMenu.GetNode<PopupMenu>("AddChildNode");
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
        _materializer = new MapTreeMaterializer();
        _camera.AddSibling(_materializer, true);
        
        _materializer.Loader = Loader;
        _materializer.CreateNodesFrom(TypedValue);
        _camera.SetTarget(_materializer, false);
    }

    private void InitializeInspector()
    {
        _inspector = GetNode<Inspector>("%Inspector");
    }

    private void ShowInspector(object @object)
    {
        object properties = @object switch
        {
            MapNode mapNode => MapNodeProperties.CopyFrom(mapNode),
            MapNodeConnection mapNodeConnection => ConnectionProperties.CopyFrom(mapNodeConnection),
            _ => null
        };
        _inspector.Inspect(properties);
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

    private void SelectOption(Options options)
    {
        switch (options)
        {
            case Options.AddChildNode:
                throw new NotImplementedException();

            case Options.RemoveNode:
                throw new NotImplementedException();
        }
    }
}