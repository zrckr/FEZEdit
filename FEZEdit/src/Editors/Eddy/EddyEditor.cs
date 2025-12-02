using System;
using System.Collections.Generic;
using System.IO;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Editors.Eddy.Materializers;
using FEZEdit.Extensions;
using FEZEdit.Gizmos;
using FEZEdit.Main;
using FEZEdit.Providers;
using Godot;

namespace FEZEdit.Editors.Eddy;

using FEZRepacker.Core.Definitions.Game.Level;
using FEZRepacker.Core.Definitions.Game.TrileSet;

public partial class EddyEditor : Editor
{
    private const int AssetBrowserTab = 0;

    private const int InspectorTab = 1;

    [Export] private float _cameraDistance;

    public override object Value
    {
        get => _level;
        set => _level = (Level)value;
    }

    public override bool Disabled
    {
        set
        {
            _instanceTable.Disabled = value;
            _assetBrowser.Disabled = value;
            _inspector.Disabled = value;
        }
    }

    private Level _level;

    private TrileSet _trileSet;

    private Dictionary<AssetType, IMaterializer> _materializers;

    private GomezMaterializer _gomez;
    
    private InstanceTable _instanceTable;

    private EddyTables _eddyTables;

    private TabContainer _tabContainer;

    private AssetBrowser _assetBrowser;

    private Inspector _inspector;

    private EddyToolbar _toolbar;

    private EddyCamera _camera;
    
    private Node3D _root;

    private TimeNode _time;

    private Cursor _cursor;

    private Gizmo3D _gizmo;

    private EddyToolbar.Tool _tool;

    private (string Path, AssetType Type) _instanceAtCursor;

    public override void _Ready()
    {
        ContentLoader.ContentProvider = new FolderProvider(new DirectoryInfo(@"D:\Projects\fez-assets\Assets"));
        _level = ContentLoader.Load<Level>(@"levels\villageville_3d");
        _trileSet = ContentLoader.LoadTrileSet(_level.TrileSetName);
        InitializeInstanceTable();
        InitializeTabContainer();
        InitializeToolbar();
        InitializeScene();
        InitializeMaterializers();
        InitializeGomez();
    }

    public override void _Refresh()
    {
        _eddyTables._Refresh();
    }

    #region  Initialization
    
    private void InitializeInstanceTable()
    {
        _instanceTable = GetNode<InstanceTable>("%InstanceTable");
        _eddyTables = GetNode<EddyTables>("%EddyTables");
        _eddyTables.ValueInspected += InspectValueFromInstanceTable;
        _eddyTables.InstanceTable = _instanceTable;
        _eddyTables.Memento = Memento;
        _eddyTables.Level = _level;
    }

    private void InitializeTabContainer()
    {
        _tabContainer = GetNode<TabContainer>("%TabContainer");
        _tabContainer.CurrentTab = AssetBrowserTab;
        
        _assetBrowser = GetNode<AssetBrowser>("%Asset Browser");
        _assetBrowser.AssetSelected += SetCursorMesh;
        _assetBrowser.CurrentTrileSetName = _level.TrileSetName;
        
        _inspector = GetNode<Inspector>("%Inspector");
        _inspector.Memento = Memento;
    }

    private void InitializeToolbar()
    {
        _toolbar = GetNode<EddyToolbar>("%EddyToolbar");
        _toolbar.ActionCalled += PerformActionOnSelection;
        _toolbar.ToolChanged += ChangeInstanceManipulationTool;
        _toolbar.ViewChanged += view => _camera.CurrentView = view;
        _toolbar.TimeChanged += time => _time.CurrentTime = time;
        _toolbar.TimeProcessChanged += value => _time.SetProcess(value);
        _toolbar.GridChanged += (level, axis) =>
        {
            _cursor.ShowGridAxis(axis);   
            _cursor.SetGridLevel(level);
        };
    }

    private void InitializeScene()
    {
        _gizmo = GetNode<Gizmo3D>("%Gizmo");
        
        _cursor = GetNode<Cursor>("%Cursor");
        _cursor.Pressed += UseToolOnInstance;
        _cursor.Canceled += ClearCursorMesh;
        _cursor.LevelChanged += _toolbar.SetGridLevel;
        
        _camera = GetNode<EddyCamera>("%Camera");
        _camera.PanningChanged += panning => _cursor.SetProcessUnhandledInput(!panning);
        _camera.ObjectPicked += InspectValueFromLevelScene;
        
        _time = GetNode<TimeNode>("%Time");
        _time.Tick += () => _toolbar.SetTime(_time.CurrentTime); 
    }
    
    private void InitializeMaterializers()
    {
        _root = GetNode<Node3D>("%RootNode");
        
        _materializers = new Dictionary<AssetType, IMaterializer>()
        {
            [AssetType.TrileSet] = TrileMaterializer.Create(_trileSet, _level.Triles, _level.Groups),
            [AssetType.ArtObject] = ArtObjectMaterializer.Create(_level.ArtObjects),
            [AssetType.BackgroundPlane] = BackgroundPlaneMaterializer.Create(_level.BackgroundPlanes),
            [AssetType.NonPlayableCharacter] = NpcMaterializer.Create(_level.NonPlayerCharacters)
        };

        foreach (var materializer in _materializers.Values)
        {
            materializer.Memento = Memento;
            _root.AddChild((Node)materializer, true);
        }
    }

    private void InitializeGomez()
    {
        _gomez = GomezMaterializer.Create(_level.StartingFace);
        _root.AddChild(_gomez, true);
     
        var position = _level.StartingFace.Id.ToGodot();
        _cursor.SetGridLevel(position.Y);
        
        _camera.Basis = _level.StartingFace.Face.AsBasis();
        _camera.Position = position + _camera.Basis.Z * _cameraDistance + _camera.Basis.Y;
        _camera.Size = _cameraDistance;
    }
    
    #endregion
    
    private void InspectValueFromLevelScene(object obj)
    {
        _tabContainer.CurrentTab = InspectorTab;
        _inspector.InspectObject(obj);
        _eddyTables.FindAndSelectRow(obj);
    }

    private void InspectValueFromInstanceTable(object obj)
    {
        _tabContainer.CurrentTab = InspectorTab;
        _inspector.InspectObject(obj);
        SelectInstance(obj);
    }

    private void SelectInstance(object obj)
    {
        throw new NotImplementedException();
    }

    private void SetCursorMesh(string path, AssetType option)
    {
        if (_materializers.TryGetValue(option, out var materializer))
        {
            var instance = materializer.GetCursorInstance(path);
            _cursor.SetCursorInstance(instance);
            _toolbar.SetToolButton(EddyToolbar.Tool.Paint);
            _instanceAtCursor = (Path: path, Type: option);
        }
        else
        {
            _cursor.SetCursorInstance(null);
            _toolbar.SetToolButton(EddyToolbar.Tool.Select);
            _instanceAtCursor = (Path: string.Empty, Type: AssetType.Unknown);
        }
    }

    private void ClearCursorMesh()
    {
        _cursor.SetCursorInstance(null);
        _toolbar.SetToolButton(EddyToolbar.Tool.Select);
    }

    private void ChangeInstanceManipulationTool(EddyToolbar.Tool tool)
    {
        _tool = tool;
        if (_tool == EddyToolbar.Tool.Select)
        {
            _cursor.SetCursorInstance(null);
        }
        
        if (_gizmo.Editing)
        {
            _gizmo.Mode = _tool switch
            {
                EddyToolbar.Tool.Select => Gizmo3D.ToolMode.All,
                EddyToolbar.Tool.Translate => Gizmo3D.ToolMode.Move,
                EddyToolbar.Tool.Rotate => Gizmo3D.ToolMode.Rotate,
                EddyToolbar.Tool.Scale => Gizmo3D.ToolMode.Scale,
                _ => _gizmo.Mode
            };
        }
    }

    private void UseToolOnInstance()
    {
        var path = _instanceAtCursor.Path;
        var type = _instanceAtCursor.Type;
        var emplacement = _cursor.GetCursorEmplacement();
        var materializer = _materializers[type];
       
        switch (_tool)
        {
            case EddyToolbar.Tool.Paint:
                materializer.CreateInstance(path, emplacement);
                break;
            
            case EddyToolbar.Tool.Pick:
                path = materializer.PickInstance(emplacement);
                SetCursorMesh(path, type);
                break;
            
            case EddyToolbar.Tool.Erase:
                materializer.RemoveInstance(emplacement);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void PerformActionOnSelection(EddyToolbar.Action action)
    {
        throw new NotImplementedException();
    }
}