using System;
using FEZEdit.Main;
using Godot;

namespace FEZEdit.Editors.Eddy;

using FEZRepacker.Core.Definitions.Game.Level;

public partial class EddyEditor : Editor
{
    private const int AssetBrowserTab = 0;

    private const int InspectorTab = 1;

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

    private Level _level = new();

    private LevelScene _levelScene;

    private InstanceTable _instanceTable;

    private LevelInstances _levelInstances;

    private TabContainer _tabContainer;

    private AssetBrowser _assetBrowser;

    private Inspector _inspector;

    public override void _Ready()
    {
        InitializeLevelScene();
        InitializeInstanceTable();
        InitializeTabContainer();
        InitializeAssetBrowser();
        InitializeInspector();
    }

    public override void _Refresh()
    {
        _levelInstances._Refresh();
    }

    private void InitializeLevelScene()
    {
        _levelScene = GetNode<LevelScene>("%LevelScene");
        _levelScene.ValueInspected += InspectValueFromLevelScene;
        _levelScene.Level = _level;
        _levelScene.Materialize();
    }
    
    private void InitializeInstanceTable()
    {
        _instanceTable = GetNode<InstanceTable>("%InstanceTable");
        _levelInstances = GetNode<LevelInstances>("%LevelInstances");
        _levelInstances.ValueInspected += InspectValueFromLevelInstance;
        _levelInstances.InstanceTable = _instanceTable;
        _levelInstances.Memento = Memento;
        _levelInstances.Level = _level;
    }

    private void InitializeTabContainer()
    {
        _tabContainer = GetNode<TabContainer>("%TabContainer");
        _tabContainer.CurrentTab = AssetBrowserTab;
    }
    
    private void InitializeAssetBrowser()
    {
        _assetBrowser = GetNode<AssetBrowser>("%Asset Browser");
        _assetBrowser.AssetSelected += PlaceInstanceOnLevel;
        _assetBrowser.CurrentTrileSet = _level.TrileSetName;
    }
    
    private void InitializeInspector()
    {
        _inspector = GetNode<Inspector>("%Inspector");
        _inspector.Memento = Memento;
    }
    
    private void InspectValueFromLevelScene(object obj)
    {
        _tabContainer.CurrentTab = InspectorTab;
        _inspector.InspectObject(obj);
        _levelInstances.FindAndSelectRow(obj);
    }

    private void InspectValueFromLevelInstance(object obj)
    {
        _tabContainer.CurrentTab = InspectorTab;
        _inspector.InspectObject(obj);
        _levelScene.FindAndSelectNode(obj);
    }
    
    private void PlaceInstanceOnLevel(string path)
    {
        throw new NotImplementedException();
    }
}