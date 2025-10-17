using System;
using System.Collections.Generic;
using System.IO;
using FEZEdit.Extensions;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.Level;
using FEZRepacker.Core.Definitions.Game.MapTree;
using Godot;

namespace FEZEdit.Content;

public static class SaveDataProvider
{
    private const long Version = 6L;

    #region Reading

    public static SaveData Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var saveData = new SaveData();
        reader.ReadInt64();     // Reads PC save timestamp
        
        var version = reader.ReadInt64();
        if (version != Version)
        {
            throw new IOException($"Invalid version: {version} (expected {Version})");
        }

        saveData.CreationTime = reader.ReadInt64();
        saveData.Finished32 = reader.ReadBoolean();
        saveData.Finished64 = reader.ReadBoolean();
        saveData.HasFPView = reader.ReadBoolean();
        saveData.HasStereo3D = reader.ReadBoolean();
        saveData.CanNewGamePlus = reader.ReadBoolean();
        saveData.IsNewGamePlus = reader.ReadBoolean();
        saveData.OneTimeTutorials.Clear();

        var capacity = reader.ReadInt32();
        saveData.OneTimeTutorials = new Dictionary<string, bool>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            saveData.OneTimeTutorials.Add(reader.ReadNullableString(), reader.ReadBoolean());
        }

        saveData.Level = reader.ReadNullableString();
        saveData.View = (Viewpoint)reader.ReadInt32();
        saveData.Ground = reader.ReadVector3();
        saveData.TimeOfDay = reader.ReadTimeSpan();

        capacity = reader.ReadInt32();
        saveData.UnlockedWarpDestinations = new List<string>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            saveData.UnlockedWarpDestinations.Add(reader.ReadNullableString());
        }

        saveData.Keys = reader.ReadInt32();
        saveData.CubeShards = reader.ReadInt32();
        saveData.SecretCubes = reader.ReadInt32();
        saveData.CollectedParts = reader.ReadInt32();
        saveData.CollectedOwls = reader.ReadInt32();
        saveData.PiecesOfHeart = reader.ReadInt32();
        if (saveData.SecretCubes > 32 || saveData.CubeShards > 32 || saveData.PiecesOfHeart > 3)
        {
            saveData.ScoreDirty = true;
        }

        saveData.SecretCubes = Mathf.Min(saveData.SecretCubes, 32);
        saveData.CubeShards = Mathf.Min(saveData.CubeShards, 32);
        saveData.PiecesOfHeart = Mathf.Min(saveData.PiecesOfHeart, 3);

        capacity = reader.ReadInt32();
        saveData.Maps = new List<string>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            saveData.Maps.Add(reader.ReadNullableString());
        }

        capacity = reader.ReadInt32();
        saveData.Artifacts = new List<ActorType>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            saveData.Artifacts.Add((ActorType)reader.ReadInt32());
        }

        capacity = reader.ReadInt32();
        saveData.EarnedAchievements = new List<string>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            saveData.EarnedAchievements.Add(reader.ReadNullableString());
        }

        capacity = reader.ReadInt32();
        saveData.EarnedGamerPictures = new List<string>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            saveData.EarnedGamerPictures.Add(reader.ReadNullableString());
        }

        saveData.ScriptingState = reader.ReadNullableString();
        saveData.FezHidden = reader.ReadBoolean();
        saveData.GlobalWaterLevelModifier = reader.ReadNullableSingle();
        saveData.HasHadMapHelp = reader.ReadBoolean();
        saveData.CanOpenMap = reader.ReadBoolean();
        saveData.AchievementCheatCodeDone = reader.ReadBoolean();
        saveData.AnyCodeDeciphered = reader.ReadBoolean();
        saveData.MapCheatCodeDone = reader.ReadBoolean();

        capacity = reader.ReadInt32();
        saveData.World = new Dictionary<string, LevelSaveData>(capacity);
        for (int num3 = 0; num3 < capacity; num3++)
        {
            try
            {
                saveData.World.Add(reader.ReadNullableString(), ReadLevelSaveData(reader));
            }
            catch
            {
                break;
            }
        }

        reader.ReadBoolean();
        saveData.ScoreDirty = true;
        saveData.HasDoneHeartReboot = reader.ReadBoolean();
        saveData.PlayTime = reader.ReadInt64();
        saveData.IsNew = string.IsNullOrEmpty(saveData.Level) ||
                         saveData.CanNewGamePlus ||
                         saveData.World.Count == 0;
        saveData.HasFPView |= saveData.HasStereo3D;
        return saveData;
    }

    private static LevelSaveData ReadLevelSaveData(BinaryReader reader)
    {
        var levelSaveData = new LevelSaveData();

        var capacity = reader.ReadInt32();
        levelSaveData.DestroyedTriles = new List<TrileEmplacement>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.DestroyedTriles.Add(reader.ReadTrileEmplacement());
        }

        capacity = reader.ReadInt32();
        levelSaveData.InactiveTriles = new List<TrileEmplacement>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.InactiveTriles.Add(reader.ReadTrileEmplacement());
        }

        capacity = reader.ReadInt32();
        levelSaveData.InactiveArtObjects = new List<int>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.InactiveArtObjects.Add(reader.ReadInt32());
        }

        capacity = reader.ReadInt32();
        levelSaveData.InactiveEvents = new List<int>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.InactiveEvents.Add(reader.ReadInt32());
        }

        capacity = reader.ReadInt32();
        levelSaveData.InactiveGroups = new List<int>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.InactiveGroups.Add(reader.ReadInt32());
        }

        capacity = reader.ReadInt32();
        levelSaveData.InactiveVolumes = new List<int>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.InactiveVolumes.Add(reader.ReadInt32());
        }

        capacity = reader.ReadInt32();
        levelSaveData.InactiveNPCs = new List<int>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.InactiveNPCs.Add(reader.ReadInt32());
        }

        capacity = reader.ReadInt32();
        levelSaveData.PivotRotations = new Dictionary<int, int>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            levelSaveData.PivotRotations.Add(reader.ReadInt32(), reader.ReadInt32());
        }

        levelSaveData.LastStableLiquidHeight = reader.ReadNullableSingle();
        levelSaveData.ScriptingState = reader.ReadNullableString();
        levelSaveData.FirstVisit = reader.ReadBoolean();
        levelSaveData.FilledConditions = ReadWinConditions(reader);
        return levelSaveData;
    }

    private static WinConditions ReadWinConditions(BinaryReader reader)
    {
        var winConditions = new WinConditions
        {
            LockedDoorCount = reader.ReadInt32(),
            UnlockedDoorCount = reader.ReadInt32(),
            ChestCount = reader.ReadInt32(),
            CubeShardCount = reader.ReadInt32(),
            OtherCollectibleCount = reader.ReadInt32(),
            SplitUpCount = reader.ReadInt32()
        };

        var capacity = reader.ReadInt32();
        winConditions.ScriptIds = new List<int>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            winConditions.ScriptIds.Add(reader.ReadInt32());
        }

        winConditions.SecretCount = reader.ReadInt32();
        return winConditions;
    }

    #endregion

    #region Writing

    public static void Write(string path, SaveData saveData)
    {
        using var stream = new FileStream(path, FileMode.Create);
        using var w = new BinaryWriter(stream);

        w.Write(DateTime.Now.ToFileTimeUtc());
        w.Write(Version);
        w.Write(saveData.CreationTime);
        w.Write(saveData.Finished32);
        w.Write(saveData.Finished64);
        w.Write(saveData.HasFPView);
        w.Write(saveData.HasStereo3D);
        w.Write(saveData.CanNewGamePlus);
        w.Write(saveData.IsNewGamePlus);
        w.Write(saveData.OneTimeTutorials.Count);
        foreach (var oneTimeTutorial in saveData.OneTimeTutorials)
        {
            w.WriteNullable(oneTimeTutorial.Key);
            w.Write(oneTimeTutorial.Value);
        }

        w.WriteNullable(saveData.Level);
        w.Write((int)saveData.View);
        w.Write(saveData.Ground);
        w.Write(saveData.TimeOfDay);
        w.Write(saveData.UnlockedWarpDestinations.Count);
        foreach (string unlockedWarpDestination in saveData.UnlockedWarpDestinations)
        {
            w.WriteNullable(unlockedWarpDestination);
        }

        w.Write(saveData.Keys);
        w.Write(saveData.CubeShards);
        w.Write(saveData.SecretCubes);
        w.Write(saveData.CollectedParts);
        w.Write(saveData.CollectedOwls);
        w.Write(saveData.PiecesOfHeart);
        w.Write(saveData.Maps.Count);
        foreach (string map in saveData.Maps)
        {
            w.WriteNullable(map);
        }

        w.Write(saveData.Artifacts.Count);
        foreach (var artifact in saveData.Artifacts)
        {
            w.Write((int)artifact);
        }

        w.Write(saveData.EarnedAchievements.Count);
        foreach (var earnedAchievement in saveData.EarnedAchievements)
        {
            w.WriteNullable(earnedAchievement);
        }

        w.Write(saveData.EarnedGamerPictures.Count);
        foreach (var earnedGamerPicture in saveData.EarnedGamerPictures)
        {
            w.WriteNullable(earnedGamerPicture);
        }

        w.WriteNullable(saveData.ScriptingState);
        w.Write(saveData.FezHidden);
        w.WriteNullable(saveData.GlobalWaterLevelModifier);
        w.Write(saveData.HasHadMapHelp);
        w.Write(saveData.CanOpenMap);
        w.Write(saveData.AchievementCheatCodeDone);
        w.Write(saveData.AnyCodeDeciphered);
        w.Write(saveData.MapCheatCodeDone);
        w.Write(saveData.World.Count);
        foreach (var item in saveData.World)
        {
            w.WriteNullable(item.Key);
            WriteLevelSaveData(w, item.Value);
        }

        w.Write(saveData.ScoreDirty);
        w.Write(saveData.HasDoneHeartReboot);
        w.Write(saveData.PlayTime);
        w.Write(saveData.IsNew);
    }

    private static void WriteLevelSaveData(BinaryWriter write, LevelSaveData levelSaveData)
    {
        write.Write(levelSaveData.DestroyedTriles.Count);
        foreach (var destroyedTrile in levelSaveData.DestroyedTriles)
        {
            write.Write(destroyedTrile);
        }

        write.Write(levelSaveData.InactiveTriles.Count);
        foreach (var inactiveTrile in levelSaveData.InactiveTriles)
        {
            write.Write(inactiveTrile);
        }

        write.Write(levelSaveData.InactiveArtObjects.Count);
        foreach (int inactiveArtObject in levelSaveData.InactiveArtObjects)
        {
            write.Write(inactiveArtObject);
        }

        write.Write(levelSaveData.InactiveEvents.Count);
        foreach (int inactiveEvent in levelSaveData.InactiveEvents)
        {
            write.Write(inactiveEvent);
        }

        write.Write(levelSaveData.InactiveGroups.Count);
        foreach (int inactiveGroup in levelSaveData.InactiveGroups)
        {
            write.Write(inactiveGroup);
        }

        write.Write(levelSaveData.InactiveVolumes.Count);
        foreach (int inactiveVolume in levelSaveData.InactiveVolumes)
        {
            write.Write(inactiveVolume);
        }

        write.Write(levelSaveData.InactiveNPCs.Count);
        foreach (int inactiveNPC in levelSaveData.InactiveNPCs)
        {
            write.Write(inactiveNPC);
        }

        write.Write(levelSaveData.PivotRotations.Count);
        foreach (KeyValuePair<int, int> pivotRotation in levelSaveData.PivotRotations)
        {
            write.Write(pivotRotation.Key);
            write.Write(pivotRotation.Value);
        }

        write.WriteNullable(levelSaveData.LastStableLiquidHeight);
        write.WriteNullable(levelSaveData.ScriptingState);
        write.Write(levelSaveData.FirstVisit);
        WriteWinConditions(write, levelSaveData.FilledConditions);
    }

    private static void WriteWinConditions(BinaryWriter writer, WinConditions wc)
    {
        writer.Write(wc.LockedDoorCount);
        writer.Write(wc.UnlockedDoorCount);
        writer.Write(wc.ChestCount);
        writer.Write(wc.CubeShardCount);
        writer.Write(wc.OtherCollectibleCount);
        writer.Write(wc.SplitUpCount);
        writer.Write(wc.ScriptIds.Count);
        foreach (int scriptId in wc.ScriptIds)
        {
            writer.Write(scriptId);
        }

        writer.Write(wc.SecretCount);
    }

    #endregion
}