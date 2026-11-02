using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    /// <summary>
    /// Calcula el camino A* más corto entre un nodo origen y un nodo destino.
    /// </summary>
    public static List<Node> GetPath(Node startNode, Node targetNode, bool excludeSafeZones = false)
    {
        if (startNode == null || targetNode == null)
            return new List<Node>();

        if (startNode == targetNode)
            return new List<Node> { startNode };

        List<Node> openSet = new List<Node> { startNode };
        HashSet<Node> closedSet = new HashSet<Node>();

        IReadOnlyList<Node> allNodes = Graph.Instance != null
            ? Graph.Instance.AllNodes
            : new List<Node>();

        for (int i = 0; i < allNodes.Count; i++)
        {
            allNodes[i].GCost = float.MaxValue;
            allNodes[i].HCost = 0f;
            allNodes[i].Parent = null;
        }

        startNode.GCost = 0f;
        startNode.HCost = Vector3.Distance(startNode.Position, targetNode.Position);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                    (Mathf.Approximately(openSet[i].FCost, currentNode.FCost) &&
                     openSet[i].HCost < currentNode.HCost))
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

            foreach (Node neighbor in currentNode.Neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor))
                {
                    continue;
                }

                if (excludeSafeZones && neighbor.IsSafeZoneNode)
                {
                    continue;
                }

                float newMovementCostToNeighbor = currentNode.GCost + Vector3.Distance(currentNode.Position, neighbor.Position);

                if (newMovementCostToNeighbor < neighbor.GCost)
                {
                    neighbor.GCost = newMovementCostToNeighbor;
                    neighbor.HCost = Vector3.Distance(neighbor.Position, targetNode.Position);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return new List<Node>();
    }

    private static List<Node> RetracePath(Node startNode, Node targetNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = targetNode;

        while (currentNode != startNode && currentNode != null)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        path.Add(startNode);
        path.Reverse();
        return path;
    }
}