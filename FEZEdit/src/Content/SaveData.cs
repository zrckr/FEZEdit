using System;
using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.Level;
using FEZRepacker.Core.Definitions.Game.MapTree;
using FEZRepacker.Core.Definitions.Game.XNA;

namespace FEZEdit.Content;

public sealed class SaveData
{
    public bool IsNew { get; set; } = true;

    public DateTime CreationTime { get; set; } = DateTime.Now.ToUniversalTime();

    public TimeSpan PlayTime { get; set; }

    public bool CanNewGamePlus { get; set; }

    public bool IsNewGamePlus { get; set; }

    public bool Finished32 { get; set; }

    public bool Finished64 { get; set; }

    public bool HasFPView { get; set; }

    public bool HasStereo3D { get; set; }

    public bool HasDoneHeartReboot { get; set; }

    public string Level { get; set; }

    public Viewpoint View { get; set; }

    public Vector3 Ground { get; set; }

    public TimeSpan TimeOfDay { get; set; } = TimeSpan.FromHours(12.0);

    public List<string> UnlockedWarpDestinations { get; set; } = ["NATURE_HUB"];

    public int Keys { get; set; }

    public int CubeShards { get; set; }

    public int SecretCubes { get; set; }

    public int CollectedParts { get; set; }

    public int CollectedOwls { get; set; }

    public int PiecesOfHeart { get; set; }

    public List<string> Maps { get; set; } = [];

    public List<ActorType> Artifacts { get; set; } = [];

    public List<string> EarnedAchievements { get; set; } = [];

    public List<string> EarnedGamerPictures { get; set; } = [];

    public bool ScoreDirty { get; set; }

    public string ScriptingState { get; set; }

    public bool FezHidden { get; set; }

    public float? GlobalWaterLevelModifier { get; set; }

    public bool HasHadMapHelp { get; set; }

    public bool CanOpenMap { get; set; }

    public bool AchievementCheatCodeDone { get; set; }

    public bool MapCheatCodeDone { get; set; }

    public bool AnyCodeDeciphered { get; set; }

    public Dictionary<string, LevelSaveData> World { get; set; } = new();

    public Dictionary<string, bool> OneTimeTutorials { get; set; } = new()
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
    public List<TrileEmplacement> DestroyedTriles { get; set; } = [];

    public List<TrileEmplacement> InactiveTriles { get; set; } = [];

    public List<int> InactiveArtObjects { get; set; } = [];

    public List<int> InactiveEvents { get; set; } = [];

    public List<int> InactiveGroups { get; set; } = [];

    public List<int> InactiveVolumes { get; set; } = [];

    public List<int> InactiveNPCs { get; set; } = [];

    public Dictionary<int, int> PivotRotations { get; set; } = new();

    public float? LastStableLiquidHeight { get; set; }

    public string ScriptingState { get; set; }

    public WinConditions FilledConditions { get; set; } = new();

    public bool FirstVisit { get; set; }
}