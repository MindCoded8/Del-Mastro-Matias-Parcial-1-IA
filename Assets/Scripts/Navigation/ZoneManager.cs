using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [Header("Key Locations")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _huntingGroundPoint;
    [SerializeField] private List<Transform> _safeZonePoints = new List<Transform>();
    [SerializeField] private Transform _exitPoint;

    public Transform SpawnPoint => _spawnPoint;
    public Transform HuntingGroundPoint => _huntingGroundPoint;
    public IReadOnlyList<Transform> SafeZonePoints => _safeZonePoints;
    public Transform ExitPoint => _exitPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Node GetClosestSafeZoneNode(Vector3 currentPosition)
    {
        if (_safeZonePoints == null || _safeZonePoints.Count == 0)
            return null;

        Node closestNode = null;
        float shortestPathCost = float.MaxValue;
        Node startNode = Graph.Instance.GetClosestNode(currentPosition);

        for (int i = 0; i < _safeZonePoints.Count; i++)
        {
            Node targetSafeNode = Graph.Instance.GetClosestNode(_safeZonePoints[i].position);
            if (targetSafeNode == null)
                continue;

            List<Node> path = AStar.GetPath(startNode, targetSafeNode);
            if (path != null && path.Count > 0)
            {
                float pathCost = targetSafeNode.GCost;
                if (pathCost < shortestPathCost)
                {
                    shortestPathCost = pathCost;
                    closestNode = targetSafeNode;
                }
            }
        }

        return closestNode;
    }
}