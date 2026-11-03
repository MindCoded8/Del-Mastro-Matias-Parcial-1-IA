using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    public static List<Node> GetPath(Node startNode, Node targetNode, bool excludeSafeZones = false)
    {
        List<Node> path = new List<Node>();
        if (startNode == null || targetNode == null) return path;
        if (startNode == targetNode)
        {
            path.Add(startNode);
            return path;
        }

        List<Node> openSet = new List<Node> { startNode };
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.GCost = 0f;
        startNode.HCost = GetHeuristic(startNode, targetNode);
        startNode.Parent = null;

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || (Mathf.Approximately(openSet[i].FCost, currentNode.FCost) && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            IReadOnlyList<Node> neighbors = currentNode.Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                Node neighbor = neighbors[i];
                if (neighbor == null || !neighbor.gameObject.activeInHierarchy || closedSet.Contains(neighbor)) continue;
                if (excludeSafeZones && neighbor.IsSafeZoneNode) continue;

                float newMovementCostToNeighbor = currentNode.GCost + Vector3.Distance(currentNode.Position, neighbor.Position);
                if (newMovementCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newMovementCostToNeighbor;
                    neighbor.HCost = GetHeuristic(neighbor, targetNode);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return path;
    }

    private static List<Node> RetracePath(Node startNode, Node targetNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = targetNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        path.Add(startNode);
        path.Reverse();
        return path;
    }

    private static float GetHeuristic(Node nodeA, Node nodeB)
    {
        return (nodeA.Position - nodeB.Position).sqrMagnitude;
    }
}
