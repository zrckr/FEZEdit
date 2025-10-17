using FEZRepacker.Core.Definitions.Game.MapTree;

namespace FEZEdit.Editors.Jenna;

internal sealed class ConnectionProperties
{
    public float BranchOversize { get; set; }

    public static ConnectionProperties CopyFrom(MapNodeConnection connection)
    {
        return new ConnectionProperties { BranchOversize = connection.BranchOversize, };
    }

    public void CopyTo(MapNodeConnection connection)
    {
        connection.BranchOversize = BranchOversize;
    }
}