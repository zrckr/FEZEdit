using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Extensions;
using FEZEdit.Loaders;
using FEZRepacker.Core.Definitions.Game.Common;
using FEZRepacker.Core.Definitions.Game.MapTree;
using Godot;

namespace FEZEdit.Editors.Jenna;

public partial class JennaMaterializer : Node3D
{
    private static readonly Vector3 XzMask = Vector3.One - Vector3.Up;

    private readonly Dictionary<MapNode, JennaNode> _jennaNodes = new();

    private ILoader _loader;
    
    private MapTree _currentMapTree;

    public override void _Ready()
    {
        Name = nameof(JennaMaterializer);
    }

    public void Initialize(MapTree mapTree, ILoader loader)
    {
        _loader = loader;
        _currentMapTree = mapTree;
        _jennaNodes.Clear();
        RebuildVisualTree(mapTree.Root);
    }

    public void AddMapNode(MapNode parentNode, MapNode newNode, FaceOrientation connectionFace)
    {
        if (_jennaNodes.ContainsKey(parentNode))
        {
            parentNode.Connections.Add(new MapNodeConnection { Node = newNode, Face = connectionFace });
            RebuildVisualTree(parentNode);
        }
    }

    public void UpdateMapNode(MapNode nodeToUpdate)
    {
        if (_jennaNodes.ContainsKey(nodeToUpdate))
        {
            RebuildVisualTree(nodeToUpdate);
        }
    }

    public void RemoveMapNode(MapNode nodeToRemove)
    {
        if (_jennaNodes.ContainsKey(nodeToRemove))
        {
            (MapNode parent, _) = _currentMapTree.FindParentWithConnection(nodeToRemove);
            var connection = parent?.Connections.FirstOrDefault(c => c.Node == nodeToRemove);
            if (connection != null)
            {
                parent.Connections.Remove(connection);
                RebuildVisualTree(parent);
            }
        }
    }

    public void HighlightNode(MapNode nodeToHighlight, bool highlight)
    {
        if (_jennaNodes.TryGetValue(nodeToHighlight, out var jennaNode))
        {
            jennaNode.SetHighlight(highlight);
        }
    }

    private void RebuildVisualTree(MapNode node)
    {
        (_, MapNodeConnection parentConnection) = _currentMapTree.FindParentWithConnection(node);
        var jennaNode = _jennaNodes.GetValueOrDefault(node);
        var parentJennaNode = jennaNode?.Parent;
        var offset = jennaNode?.GlobalPosition ?? Vector3.Zero;

        var multiBranchIds = new Dictionary<MapNodeConnection, int>();
        var multiBranchCounts = new Dictionary<MapNodeConnection, int>();

        var stack = new Stack<NodeProcessingState>();
        stack.Push(new NodeProcessingState(node, parentConnection, parentJennaNode, offset));

        _jennaNodes.Remove(node);
        jennaNode?.QueueFree();
        
        while (stack.Count > 0)
        {
            (node, parentConnection, parentJennaNode, offset) = stack.Pop();

            jennaNode = JennaNode.Create(_loader, node);
            if (parentJennaNode == null)
            {
                AddChild(jennaNode, true);
            }
            else
            {
                parentJennaNode.AddJennaChild(jennaNode, offset);
            }
            jennaNode.UpdateMapIcons(node);
            _jennaNodes[node] = jennaNode;

            foreach (var c in node.Connections)
            {
                if (c.Node.NodeType == LevelNodeType.Lesser &&
                    node.Connections.Any(x => x.Face == c.Face && c.Node.NodeType != LevelNodeType.Lesser))
                {
                    if (node.Connections.All(x => x.Face != FaceOrientation.Top))
                    {
                        c.Face = FaceOrientation.Top;
                    }
                    else if (node.Connections.All(x => x.Face != FaceOrientation.Down))
                    {
                        c.Face = FaceOrientation.Down;
                    }
                }
            }

            foreach (var c in node.Connections)
            {
                multiBranchIds.TryAdd(c, 0);
            }

            foreach (var c in node.Connections)
            {
                multiBranchIds[c] = node.Connections
                    .Where(x => x.Face == c.Face)
                    .Max(x => multiBranchIds[x]) + 1;
                multiBranchCounts[c] = node.Connections.Count(x => x.Face == c.Face);
            }

            var num = 0f;
            var orderedConnections = node.Connections.OrderByDescending(x => x.Node.NodeType.GetSizeFactor());
            foreach (var item in orderedConnections)
            {
                if (parentConnection != null && item.Face == parentConnection.Face.GetOpposite())
                {
                    item.Face = item.Face.GetOpposite();
                }

                // Calculate size factor for this connection
                var sizeFactor = 3f + ((node.NodeType.GetSizeFactor() + item.Node.NodeType.GetSizeFactor()) / 2f);
                if ((node.NodeType == LevelNodeType.Hub || item.Node.NodeType == LevelNodeType.Hub) &&
                    node.NodeType != LevelNodeType.Lesser && item.Node.NodeType != LevelNodeType.Lesser)
                {
                    sizeFactor += 1f;
                }

                // Adjust for lesser nodes
                if ((node.NodeType == LevelNodeType.Lesser || item.Node.NodeType == LevelNodeType.Lesser) &&
                    multiBranchCounts[item] == 1)
                {
                    sizeFactor -= item.Face.IsSide() ? 1 : 2;
                }

                // Apply branch oversize
                sizeFactor *= 1.25f + item.BranchOversize;
                var num4 = sizeFactor * 0.375f;
                if (item.Node.NodeType == LevelNodeType.Node && node.NodeType == LevelNodeType.Node)
                {
                    num4 *= 1.5f;
                }

                // Calculate branch offset for multi-branch connections
                var faceVector = item.Face.AsVector();
                var vector2 = Vector3.Zero;
                if (multiBranchCounts[item] > 1)
                {
                    vector2 = ((multiBranchIds[item] - 1) - ((multiBranchCounts[item] - 1) / 2f)) *
                              (XzMask - item.Face.AsVector().Abs()) * num4;
                }

                var childOffset = offset + (faceVector * sizeFactor) + vector2;
                stack.Push(new NodeProcessingState(item.Node, item, jennaNode, childOffset));

                jennaNode.CreateLink(item);
                if (multiBranchCounts[item] > 1)
                {
                    // Create multi-branch link segments
                    num = Math.Max(num, sizeFactor / 2f);
                    var scale = (faceVector * num) + Vector3.One * JennaNode.LinkThickness;
                    var position = (faceVector * num / 2f) + offset;
                    jennaNode.AddLinkBranch(position, scale);

                    scale = vector2 + Vector3.One * JennaNode.LinkThickness;
                    position = (vector2 / 2f) + offset + (faceVector * num);
                    jennaNode.AddLinkBranch(position, scale);

                    var num5 = sizeFactor - num;
                    scale = (faceVector * num5) + Vector3.One * JennaNode.LinkThickness;
                    position = (faceVector * num5 / 2f) + offset + (faceVector * num) + vector2;
                    jennaNode.AddLinkBranch(position, scale);
                }
                else
                {
                    // Create single branch link
                    var scale = (faceVector * sizeFactor) + Vector3.One * JennaNode.LinkThickness;
                    var position = (faceVector * sizeFactor / 2f) + offset;
                    jennaNode.AddLinkBranch(position, scale);
                }

                // Handle special cases
                switch (item.Node.LevelName)
                {
                    case "LIGHTHOUSE_SPIN":
                        {
                            const float num6 = 3.425f;
                            var scale = (Vector3.Back * num6) + Vector3.One * JennaNode.LinkThickness;
                            var position = (Vector3.Back * num6 / 2f) + offset + (faceVector * sizeFactor);
                            jennaNode.AddLinkBranch(position, scale);
                            break;
                        }

                    case "LIGHTHOUSE_HOUSE_A":
                        {
                            const float num7 = 5f;
                            var scale = (Vector3.Right * num7) + Vector3.One * JennaNode.LinkThickness;
                            var position = (Vector3.Right * num7 / 2f) + offset + (faceVector * sizeFactor);
                            jennaNode.AddLinkBranch(position, scale);
                            break;
                        }
                }
            }
        }
    }

    private record struct NodeProcessingState(
        MapNode Node,
        MapNodeConnection ParentConnection,
        JennaNode ParentJennaNode,
        Vector3 Offset
    );
}