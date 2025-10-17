using System;
using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.Level;
using FEZRepacker.Core.Definitions.Game.MapTree;
using FEZRepacker.Core.Definitions.Game.XNA;

namespace FEZEdit.Content;

public sealed class SaveData
{
    public bool IsNew = true;

    public long CreationTime = DateTime.Now.ToFileTimeUtc();

    public long PlayTime;

    public long? SinceLastSaved;

    public bool CanNewGamePlus;

    public bool IsNewGamePlus;

    public bool Finished32;

    public bool Finished64;

    public bool HasFPView;

    public bool HasStereo3D;

    public bool HasDoneHeartReboot;

    public string Level;

    public Viewpoint View;

    public Vector3 Ground;

    public TimeSpan TimeOfDay = TimeSpan.FromHours(12.0);

    public List<string> UnlockedWarpDestinations = ["NATURE_HUB"];

    public int Keys;

    public int CubeShards;

    public int SecretCubes;

    public int CollectedParts;

    public int CollectedOwls;

    public int PiecesOfHeart;

    public List<string> Maps = [];

    public List<ActorType> Artifacts = [];

    public List<string> EarnedAchievements = [];

    public List<string> EarnedGamerPictures = [];

    public bool ScoreDirty;

    public string ScriptingState;

    public bool FezHidden;

    public float? GlobalWaterLevelModifier;

    public bool HasHadMapHelp;

    public bool CanOpenMap;

    public bool AchievementCheatCodeDone;

    public bool MapCheatCodeDone;

    public bool AnyCodeDeciphered;

    public Dictionary<string, LevelSaveData> World = new();

    public Dictionary<string, bool> OneTimeTutorials = new()
    {
        { "DOT_LOCKED_DOOR_A", false },
        { "DOT_NUT_N_BOLT_A", false },
        { "DOT_PIVOT_A", false },
        { "DOT_TIME_SWITCH_A", false },
        { "DOT_TOMBSTONE_A", false },
        { "DOT_TREASURE", false },
        { "DOT_VALVE_A", false },
        { "DOT_WEIGHT_SWITCH_A", false },
        { "DOT_LESSER_A", false },
        { "DOT_WARP_A", false },
        { "DOT_BOMB_A", false },
        { "DOT_CLOCK_A", false },
        { "DOT_CRATE_A", false },
        { "DOT_TELESCOPE_A", false },
        { "DOT_WELL_A", false },
        { "DOT_WORKING", false }
    };
}

public sealed class LevelSaveData
{
    public List<TrileEmplacement> DestroyedTriles = [];

    public List<TrileEmplacement> InactiveTriles = [];

    public List<int> InactiveArtObjects = [];

    public List<int> InactiveEvents = [];

    public List<int> InactiveGroups = [];

    public List<int> InactiveVolumes = [];

    public List<int> InactiveNPCs = [];

    public Dictionary<int, int> PivotRotations = new();

    public float? LastStableLiquidHeight;

    public string ScriptingState;

    public WinConditions FilledConditions = new();

    public bool FirstVisit;
}