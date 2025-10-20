using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Content;
using FEZEdit.Core;
using FEZEdit.Main;
using Godot;
using Serilog;
using WinConditions = FEZRepacker.Core.Definitions.Game.MapTree.WinConditions;

namespace FEZEdit.Editors.Sally;

public partial class SallyEditor : Editor
{
    private static readonly ILogger Logger = LoggerFactory.Create<SallyEditor>();
    
    private static readonly string[] SaveDataProperties = [
        nameof(SaveData.IsNew),
        nameof(SaveData.CreationTime),
        nameof(SaveData.PlayTime),
        nameof(SaveData.Finished32),
        nameof(SaveData.Finished64),
        nameof(SaveData.HasFPView),
        nameof(SaveData.HasStereo3D),
        nameof(SaveData.CanNewGamePlus),
        nameof(SaveData.IsNewGamePlus),
        nameof(SaveData.OneTimeTutorials),
        nameof(SaveData.Level),
        nameof(SaveData.View),
        nameof(SaveData.Ground),
        nameof(SaveData.TimeOfDay),
        nameof(SaveData.Keys),
        nameof(SaveData.CubeShards),
        nameof(SaveData.SecretCubes),
        nameof(SaveData.CollectedParts),
        nameof(SaveData.CollectedOwls),
        nameof(SaveData.PiecesOfHeart),
        nameof(SaveData.Maps),
        nameof(SaveData.Artifacts),
        nameof(SaveData.EarnedAchievements),
        nameof(SaveData.EarnedGamerPictures),
        nameof(SaveData.ScriptingState),
        nameof(SaveData.FezHidden),
        nameof(SaveData.GlobalWaterLevelModifier),
        nameof(SaveData.HasHadMapHelp),
        nameof(SaveData.CanOpenMap),
        nameof(SaveData.AchievementCheatCodeDone),
        nameof(SaveData.AnyCodeDeciphered),
        nameof(SaveData.MapCheatCodeDone),
        nameof(SaveData.ScoreDirty),
        nameof(SaveData.HasDoneHeartReboot)
    ];

    private static readonly string[] LevelSaveDataProperties =
    [
        nameof(LevelSaveData.DestroyedTriles),
        nameof(LevelSaveData.InactiveTriles),
        nameof(LevelSaveData.InactiveArtObjects),
        nameof(LevelSaveData.InactiveEvents),
        nameof(LevelSaveData.InactiveGroups),
        nameof(LevelSaveData.InactiveGroups),
        nameof(LevelSaveData.InactiveVolumes),
        nameof(LevelSaveData.InactiveNPCs),
        nameof(LevelSaveData.PivotRotations),
        nameof(LevelSaveData.LastStableLiquidHeight),
        nameof(LevelSaveData.ScriptingState),
        nameof(LevelSaveData.FirstVisit)
    ];

    private static readonly string[] WinConditionsProperties =
    [
        nameof(WinConditions.LockedDoorCount),
        nameof(WinConditions.UnlockedDoorCount),
        nameof(WinConditions.ChestCount),
        nameof(WinConditions.CubeShardCount),
        nameof(WinConditions.OtherCollectibleCount),
        nameof(WinConditions.SplitUpCount),
        nameof(WinConditions.ScriptIds),
        nameof(WinConditions.SecretCount)
    ];

    private const string SaveDataPath = "SaveDataPath";
    
    public override event Action ValueChanged;

    public override object Value
    {
        get => _saveData;
        set => _saveData = (SaveData)value;
    }

    public override bool Disabled
    {
        set
        {
            _saveDataInspector.Disabled = value;
            _levelSaveDataList.Disabled = value;
            _levelSaveDataInspector.Disabled = value;
            _filledConditionsInspector.Disabled = value;
        }
    }
    
    private List<string> LevelKeys => _saveData.World.Keys.OrderBy(k => k).ToList();

    [Export] private Godot.Collections.Dictionary<string, string> _saveDataTooltips = new();
    
    [Export] private Godot.Collections.Dictionary<string, string> _levelSaveDataTooltips = new();
    
    [Export] private Godot.Collections.Dictionary<string, string> _filledConditionsTooltips = new();

    [Export(PropertyHint.File)] private string _oneHundredSaveFile;
   
    [Export(PropertyHint.File)] private string _twoHundredSaveFile;
    
    private SaveData _saveData = new();

    private OptionButton _formatOption;
    
    private Button _newSaveButton;

    private Button _twoHundredButton;
    
    private Button _oneHundredButton;

    private Inspector _saveDataInspector;
    
    private LevelSaveDataList _levelSaveDataList;

    private Inspector _levelSaveDataInspector;

    private Inspector _filledConditionsInspector;

    private ConfirmationDialog _confirmDialog;

    public override void _Ready()
    {
        InitializeFormatOption();
        InitializeButtons();
        InitializeSaveData();
        InitializeList();
        InitializeLevelSaveData();
        RepopulateSaveData();
    }

    private void InitializeFormatOption()
    {
        _formatOption = GetNode<OptionButton>("%FormatOption");
        foreach (var value in Enum.GetValues<SaveDataProvider.SaveFormat>())
        {
            _formatOption.AddItem(value.ToString().ToUpper());
        }
        
        _formatOption.Selected = 0;
        _formatOption.ItemSelected += format =>
        {
            var oldFormat = SaveDataProvider.Format;
            UndoRedo.CreateAction("Change Save Format");
            UndoRedo.AddDoMethod(() =>
            {
                SaveDataProvider.Format = (SaveDataProvider.SaveFormat)format;
                ValueChanged?.Invoke();
            });
            UndoRedo.AddUndoMethod(() =>
            {
                SaveDataProvider.Format = oldFormat;
                ValueChanged?.Invoke();
            });
            UndoRedo.CommitAction();
        };
    }

    private void InitializeButtons()
    {
        _newSaveButton = GetNode<Button>("%NewSaveButton");
        _newSaveButton.Pressed += () => ConfirmLoadingSaveData(string.Empty);
        
        _twoHundredButton = GetNode<Button>("%TwoHundredButton");
        _twoHundredButton.Pressed += () => ConfirmLoadingSaveData(_twoHundredSaveFile);
        
        _oneHundredButton = GetNode<Button>("%OneHundredButton");
        _oneHundredButton.Pressed += () => ConfirmLoadingSaveData(_oneHundredSaveFile);
        
        _confirmDialog = GetNode<ConfirmationDialog>("%ConfirmDialog");
        _confirmDialog.Confirmed += LoadSaveData;
    }

    private void InitializeSaveData()
    {
        _saveDataInspector = GetNode<Inspector>("%SaveDataInspector");
        _saveDataInspector.UndoRedo = UndoRedo;
        _saveDataInspector.TargetChanged += _ => ValueChanged?.Invoke();
    }

    private void InitializeList()
    {
        _levelSaveDataList = GetNode<LevelSaveDataList>("%LevelSaveDataList");
        _levelSaveDataList.LevelAdded += AddWorldLevel;
        _levelSaveDataList.LevelRemoved += RemoveWorldLevel;
        _levelSaveDataList.LevelSelected += RepopulateLevelSaveData;
        _levelSaveDataList.LevelRenamed += RenameWorldLevel;
    }

    private void InitializeLevelSaveData()
    {
        _levelSaveDataInspector = GetNode<Inspector>("%LevelSaveDataInspector");
        _levelSaveDataInspector.UndoRedo = UndoRedo;
        _levelSaveDataInspector.TargetChanged += _ => ValueChanged?.Invoke();
        _levelSaveDataInspector.Visible = false;
        
        _filledConditionsInspector = GetNode<Inspector>("%FilledConditionsInspector");
        _filledConditionsInspector.UndoRedo = UndoRedo;
        _filledConditionsInspector.TargetChanged += _ => ValueChanged?.Invoke();
        _filledConditionsInspector.Visible = false;
    }

    private void RepopulateSaveData()
    {
        _saveDataInspector.ClearProperties();
        foreach (string propertyName in SaveDataProperties)
        {
            _saveDataInspector.InspectProperty(_saveData, propertyName);
            _saveDataTooltips.TryGetValue(propertyName, out var tooltip);
            _saveDataInspector.AddPropertyTooltip(propertyName, tooltip ?? string.Empty);
        }
        
        RepopulateLevelList();
    }

    private void RepopulateLevelList()
    {
        _levelSaveDataList.ClearLevels();
        foreach (var level in LevelKeys)
        {
            _levelSaveDataList.AddLevel(level);
        }
        
        RepopulateLevelSaveData(string.Empty);
    }
    
    private void RemoveWorldLevel(string level)
    {
        var levelSaveData = _saveData.World[level];
        UndoRedo.CreateAction($"Remove Level {level}");
        UndoRedo.AddDoMethod(() =>
        {
            _saveData.World.Remove(level);
            RepopulateLevelList();
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _saveData.World[level] = levelSaveData;
            RepopulateLevelList();
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }

    private void AddWorldLevel(string level)
    {
        UndoRedo.CreateAction($"Add Level {level}");
        UndoRedo.AddDoMethod(() =>
        {
            _saveData.World[level] = new LevelSaveData();
            RepopulateLevelList();
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _saveData.World.Remove(level);
            RepopulateLevelList();
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }

    private void RenameWorldLevel(int index, string level)
    {
        var oldLevel = LevelKeys[index];
        var saveData = _saveData.World[oldLevel];
        
        UndoRedo.CreateAction($"Rename Level {oldLevel} to {level}");
        UndoRedo.AddDoMethod(() =>
        {
            _saveData.World.Remove(oldLevel);
            _saveData.World[level] = saveData;
            RepopulateLevelList();
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _saveData.World[oldLevel] = saveData;
            _saveData.World.Remove(level);
            RepopulateLevelList();
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }
    
    private void RepopulateLevelSaveData(string level)
    {
        _levelSaveDataInspector.ClearProperties();
        _filledConditionsInspector.ClearProperties();
       
        if (string.IsNullOrEmpty(level))
        {
            _levelSaveDataInspector.Visible = false;
            _filledConditionsInspector.Visible = false;
            return;
        }
        
        var levelSaveData = _saveData.World[level];
        foreach (var propertyName in LevelSaveDataProperties)
        {
            _levelSaveDataInspector.InspectProperty(levelSaveData, propertyName);
            _levelSaveDataTooltips.TryGetValue(propertyName, out var tooltip);
            _levelSaveDataInspector.AddPropertyTooltip(propertyName, tooltip ?? string.Empty);
        }

        var conditions = levelSaveData.FilledConditions;
        foreach (var propertyName in WinConditionsProperties)
        {
            _filledConditionsInspector.InspectProperty(conditions, propertyName);
            _filledConditionsTooltips.TryGetValue(propertyName, out var tooltip);
            _filledConditionsInspector.AddPropertyTooltip(propertyName, tooltip ?? string.Empty);
        }

        _levelSaveDataInspector.Visible = true;
        _filledConditionsInspector.Visible = true;
    }

    private void ConfirmLoadingSaveData(string path)
    {
        _confirmDialog.SetMeta(SaveDataPath, path);
        _confirmDialog.PopupCentered();
    }
    
    private void LoadSaveData()
    {
        var path = _confirmDialog.GetMeta(SaveDataPath).AsString();
        _confirmDialog.RemoveMeta(SaveDataPath);

        var oldSaveData = _saveData;
        SaveData newSaveData;
        
        if (string.IsNullOrEmpty(path))
        {
            newSaveData = new SaveData();
        }
        else
        {
            var fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (Godot.FileAccess.GetOpenError() != Error.Ok)
            {
                EventBus.Error("Failed to open save data: {0}", path);
                Logger.Error("Failed to open save data: '{0}'", path);
                return;
            }
        
            var fileLength = (long)fileAccess.GetLength();
            using var stream = new MemoryStream(fileAccess.GetBuffer(fileLength));
            newSaveData = SaveDataProvider.Read(stream);
        }
        
        if (newSaveData == null)
        {
            EventBus.Error("Failed to create new save data: {0}", path);
            Logger.Error("Failed to create new save data: '{0}'", path);
            return;
        }
        
        UndoRedo.CreateAction("New Save Data");
        UndoRedo.AddDoMethod(() =>
        {
            _saveData = newSaveData;
            RepopulateSaveData();
            ValueChanged?.Invoke();
        });
        UndoRedo.AddUndoMethod(() =>
        {
            _saveData = oldSaveData;
            RepopulateSaveData();
            ValueChanged?.Invoke();
        });
        UndoRedo.CommitAction();
    }
}