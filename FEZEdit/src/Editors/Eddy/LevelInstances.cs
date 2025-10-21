using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace FEZEdit.Editors.Eddy;

using FEZRepacker.Core.Definitions.Game.Level;
using Script = FEZRepacker.Core.Definitions.Game.Level.Scripting.Script;

public partial class LevelInstances : Control
{
    private enum TableType
    {
        None,
        ArtObjects,
        BackgroundPlanes,
        Groups,
        Npcs,
        Paths,
        Volumes,
        Scripts
    }

    public event Action ValueChanged;

    public event Action<object> ValueInspected;

    public UndoRedo UndoRedo { private get; set; }

    public InstanceTable InstanceTable
    {
        private get => _instanceTable;
        set
        {
            _instanceTable = value;
            if (_instanceTable != null)
            {
                _instanceTable.Hide();
                _instanceTable.InstanceAdded += AddNewInstance;
                _instanceTable.InstanceCloned += CloneInstance;
                _instanceTable.InstanceDeleted += DeleteInstance;
                _instanceTable.InstanceSelected += SelectInstance;
                _instanceTable.Closed += CloseInstanceTable;
            }
        }
    }

    public Level Level { private get; set; }

    private TableType _currentTable;

    private InstanceTable _instanceTable;

    private IDictionary _instances;

    private Func<object> _instanceCreator;
    
    private Action _instancesRefresher;

    private readonly Dictionary<TableType, Button> _buttons = new();

    public override void _Ready()
    {
        InitializeButton(TableType.ArtObjects, "%ArtObjectsButton", ShowArtObjectsTable);
        InitializeButton(TableType.BackgroundPlanes, "%BackgroundPlanesButton", ShowBackgroundPlanes);
        InitializeButton(TableType.Groups, "%GroupsButton", ShowGroups);
        InitializeButton(TableType.Npcs, "%NpcsButton", ShowNpcs);
        InitializeButton(TableType.Paths, "%PathsButton", ShowPaths);
        InitializeButton(TableType.Volumes, "%VolumesButton", ShowVolumes);
        InitializeButton(TableType.Scripts, "%ScriptsButton", ShowScripts);
    }

    public void FindAndSelectRow(object obj)
    {
        var tableType = obj switch
        {
            ArtObjectInstance => TableType.ArtObjects,
            BackgroundPlane => TableType.BackgroundPlanes,
            TrileGroup => TableType.Groups,
            NpcInstance => TableType.Npcs,
            MovementPath => TableType.Paths,
            Volume => TableType.Volumes,
            Script => TableType.Scripts,
            _ => TableType.None
        };

        if (tableType == TableType.None)
        {
            return;
        }
        
        var button = _buttons[tableType];
        button.ButtonPressed = true;

        var objectId = -1;
        foreach (var key in _instances.Keys)
        {
            if (key is int id && _instances[id] == obj)
            {
                objectId = id;
                break;
            }
        }

        _instanceTable.SelectRow(objectId);
    }

    private void InitializeButton(TableType table, NodePath path, Action showTable)
    {
        var button = GetNode<Button>(path);
        _buttons.Add(table, button);
        
        button.Toggled += pressed =>
        {
            if (pressed)
            {
                ClearTable();
                showTable?.Invoke();
                InstanceTable.Show();
            }
            else if (_currentTable != TableType.None && _currentTable == table)
            {
                ClearTable();
                _currentTable = TableType.None;
            }
        };
    }

    private void ClearTable()
    {
        InstanceTable.Hide();
        InstanceTable.ClearRows();
        InstanceTable.ClearColumns();
    }

    private void ShowArtObjectsTable()
    {
        _currentTable = TableType.ArtObjects;
        InstanceTable.AddColumn("Name");
        InstanceTable.AddColumn("Position");
        InstanceTable.AddColumn("Rotation");
        InstanceTable.AddColumn("Scale");

        _instances = Level.ArtObjects;
        _instanceCreator = () => new ArtObjectInstance();
        _instancesRefresher = () =>
        {
            if (_instances is Dictionary<int, ArtObjectInstance> dict)
            {
                var sorted = new SortedDictionary<int, ArtObjectInstance>(dict);
                foreach ((int id, ArtObjectInstance ao) in sorted)
                {
                    InstanceTable.AddRow(id, ao.Name, ao.Position, ao.Rotation, ao.Scale);
                }
            }
        };
        
        _instancesRefresher?.Invoke();
    }

    private void ShowBackgroundPlanes()
    {
        _currentTable = TableType.BackgroundPlanes;
        InstanceTable.AddColumn("TextureName");
        InstanceTable.AddColumn("Position");
        InstanceTable.AddColumn("Rotation");
        InstanceTable.AddColumn("Scale");

        _instances = Level.BackgroundPlanes;
        _instanceCreator = () => new BackgroundPlane();
        _instancesRefresher = () =>
        {
            if (_instances is Dictionary<int, BackgroundPlane> dict)
            {
                var sorted = new SortedDictionary<int, BackgroundPlane>(dict);
                foreach ((int id, BackgroundPlane bp) in sorted)
                {
                    InstanceTable.AddRow(id, bp.TextureName, bp.Position, bp.Rotation, bp.Scale);
                }
            }
        };
        
        _instancesRefresher?.Invoke();
    }

    private void ShowGroups()
    {
        _currentTable = TableType.Groups;
        InstanceTable.AddColumn("Triles");
        InstanceTable.AddColumn("Actor Type");

        _instances = Level.Groups;
        _instanceCreator = () => new TrileGroup();
        _instancesRefresher = () =>
        {
            if (_instances is Dictionary<int, TrileGroup> dict)
            {
                var sorted = new SortedDictionary<int, TrileGroup>(dict);
                foreach ((int id, TrileGroup group) in sorted)
                {
                    InstanceTable.AddRow(id, group.Triles.Count, group.ActorType);
                }
            }
        };
        
        _instancesRefresher?.Invoke();
    }

    private void ShowNpcs()
    {
        _currentTable = TableType.Npcs;
        InstanceTable.AddColumn("Name");
        InstanceTable.AddColumn("Position");
        InstanceTable.AddColumn("Destination");
        InstanceTable.AddColumn("Actor Type");

        _instances = Level.NonPlayerCharacters;
        _instanceCreator = () => new NpcInstance();
        _instancesRefresher = () =>
        {
            if (_instances is Dictionary<int, NpcInstance> dict)
            {
                var sorted = new SortedDictionary<int, NpcInstance>(dict);
                foreach ((int id, NpcInstance npc) in sorted)
                {
                    InstanceTable.AddRow(id, npc.Name, npc.Position, npc.DestinationOffset, npc.ActorType);
                }
            }
        };

        _instancesRefresher?.Invoke();
    }

    private void ShowPaths()
    {
        _currentTable = TableType.Paths;
        InstanceTable.AddColumn("Segments");
        InstanceTable.AddColumn("Needs Trigger");
        InstanceTable.AddColumn("End Behaviour");

        _instances = Level.Paths;
        _instanceCreator = () => new MovementPath();
        _instancesRefresher = () =>
        {
            if (_instances is Dictionary<int, MovementPath> dict)
            {
                var sorted = new SortedDictionary<int, MovementPath>(dict);
                foreach ((int id, MovementPath path) in sorted)
                {
                    InstanceTable.AddRow(id, path.Segments.Count, path.NeedsTrigger, path.EndBehavior);
                }
            }
        };

        _instancesRefresher?.Invoke();
    }

    private void ShowScripts()
    {
        _currentTable = TableType.Scripts;
        InstanceTable.AddColumn("Name");
        InstanceTable.AddColumn("Trigger");
        InstanceTable.AddColumn("Condition");
        InstanceTable.AddColumn("Action");

        _instances = Level.Scripts;
        _instanceCreator = () => new Script();
        _instancesRefresher = () =>
        {
            if (_instances is Dictionary<int, Script> dict)
            {
                var sorted = new SortedDictionary<int, Script>(dict);
                foreach ((int id, Script s) in sorted)
                {
                    InstanceTable.AddRow(id, s.Name, s.Triggers, s.Conditions, s.Actions);
                }
            }
        };
        
        _instancesRefresher?.Invoke();
    }

    private void ShowVolumes()
    {
        _currentTable = TableType.Volumes;
        InstanceTable.AddColumn("Orientations");
        InstanceTable.AddColumn("From");
        InstanceTable.AddColumn("To");

        _instances = Level.Volumes;
        _instanceCreator = () => new Volume();
        _instancesRefresher = () =>
        {
            if (_instances is Dictionary<int, Volume> dict)
            {
                var sorted = new SortedDictionary<int, Volume>(dict);
                foreach ((int id, Volume v) in sorted)
                {
                    InstanceTable.AddRow(id, v.Orientations, v.From, v.To);
                }
            }
        };
        
        _instancesRefresher?.Invoke();
    }

    private void AddNewInstance(int id)
    {
        if (_instances.Contains(id))
        {
            return;
        }

        var newInstance = _instanceCreator?.Invoke();
        if (newInstance == null)
        {
            return;
        }

        UndoRedo.CreateAction($"Add new {newInstance.GetType().Name}");
        UndoRedo.AddDoMethod(() =>
        {
            _instances[id] = newInstance;
            ValueChanged?.Invoke();
            RefreshCurrentTable();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _instances.Remove(id);
            ValueChanged?.Invoke();
            RefreshCurrentTable();
        });
        UndoRedo.CommitAction();
    }

    private void SelectInstance(int id)
    {
        if (_instances.Contains(id))
        {
            ValueInspected?.Invoke(_instances[id]);
        }
    }

    private void DeleteInstance(int id)
    {
        if (!_instances.Contains(id))
        {
            return;
        }

        var existingInstance = _instances[id];
        if (existingInstance == null)
        {
            return;
        }

        UndoRedo.CreateAction($"Delete the {existingInstance.GetType().Name}");
        UndoRedo.AddDoMethod(() =>
        {
            _instances.Remove(id);
            ValueChanged?.Invoke();
            RefreshCurrentTable();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _instances[id] = existingInstance;
            ValueChanged?.Invoke();
            RefreshCurrentTable();
        });
        UndoRedo.CommitAction();
    }

    private void CloneInstance(int id)
    {
        if (!_instances.Contains(id))
        {
            return;
        }

        var existingInstance = _instances[id];
        if (existingInstance == null)
        {
            return;
        }

        var nextId = ((IEnumerable<int>)_instances.Keys).Max() + 1;
        var type = existingInstance.GetType();
        var clonedInstance = CloneObject(existingInstance, type);

        UndoRedo.CreateAction($"Clone the {type} ({nextId})");
        UndoRedo.AddDoMethod(() =>
        {
            _instances[nextId] = clonedInstance;
            ValueChanged?.Invoke();
            RefreshCurrentTable();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _instances.Remove(nextId);
            ValueChanged?.Invoke();
            RefreshCurrentTable();
        });
        UndoRedo.CommitAction();
    }

    private void CloseInstanceTable()
    {
        if (_currentTable != TableType.None)
        {
            _buttons[_currentTable].ButtonPressed = false;
        }
    }

    private void RefreshCurrentTable()
    {
        _instanceTable.ClearRows();
        _instancesRefresher?.Invoke();
    }

    private static object CloneObject(object obj, Type type)
    {
        // Not a very efficient solution, but it is called once on request and it is simple
        var json = JsonSerializer.Serialize(obj, type);
        return JsonSerializer.Deserialize(json, type);
    }
}