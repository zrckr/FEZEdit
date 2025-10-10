using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.MapTree;

namespace FEZEdit.Interface.Editors.Jenna;

internal sealed class MapNodeProperties
{
    public string LevelName { get; set; }

    public LevelNodeType NodeType { get; set; }

    public bool HasLesserGate { get; set; }

    public bool HasWarpGate { get; set; }

    public int ChestCount { get; set; }

    public int LockedDoorCount { get; set; }

    public int UnlockedDoorCount { get; set; }

    public List<int> ScriptIds { get; set; }

    public int CubeShardCount { get; set; }

    public int OtherCollectibleCount { get; set; }

    public int SplitUpCount { get; set; }

    public int SecretCount { get; set; }

    public static MapNodeProperties CopyFrom(MapNode mapNode)
    {
        return new MapNodeProperties
        {
            LevelName = mapNode.LevelName,
            NodeType = mapNode.NodeType,
            HasLesserGate = mapNode.HasLesserGate,
            HasWarpGate = mapNode.HasWarpGate,
            ChestCount = mapNode.Conditions.ChestCount,
            LockedDoorCount = mapNode.Conditions.LockedDoorCount,
            UnlockedDoorCount = mapNode.Conditions.UnlockedDoorCount,
            ScriptIds = mapNode.Conditions.ScriptIds,
            CubeShardCount = mapNode.Conditions.CubeShardCount,
            OtherCollectibleCount = mapNode.Conditions.OtherCollectibleCount,
            SplitUpCount = mapNode.Conditions.SplitUpCount,
            SecretCount = mapNode.Conditions.SecretCount
        };
    }

    public void CopyTo(MapNode mapNode)
    {
        mapNode.LevelName = LevelName; 
        mapNode.NodeType = NodeType; 
        mapNode.HasLesserGate = HasLesserGate; 
        mapNode.HasWarpGate = HasWarpGate; 
        mapNode.Conditions.ChestCount = ChestCount; 
        mapNode.Conditions.LockedDoorCount = LockedDoorCount; 
        mapNode.Conditions.UnlockedDoorCount = UnlockedDoorCount; 
        mapNode.Conditions.ScriptIds = ScriptIds; 
        mapNode.Conditions.CubeShardCount = CubeShardCount; 
        mapNode.Conditions.OtherCollectibleCount = OtherCollectibleCount; 
        mapNode.Conditions.SplitUpCount = SplitUpCount; 
        mapNode.Conditions.SecretCount = SecretCount;
    }
}