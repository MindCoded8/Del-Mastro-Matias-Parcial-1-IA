using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public static Graph Instance { get; private set; }

    [Header("Nodes in Graph")]
    [SerializeField] private List<Node> _allNodes = new List<Node>();

    public IReadOnlyList<Node> AllNodes => _allNodes;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AutoCollectNodes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AutoCollectNodes()
    {
        _allNodes.Clear();
        Node[] nodes = GetComponentsInChildren<Node>();
        _allNodes.AddRange(nodes);
    }

    public Node GetClosestNode(Vector3 position, bool excludeSafeZones = false)
    {
        position.y = 0f;
        Node closest = null;
        float minSqrDist = float.MaxValue;

        for (int i = 0; i < _allNodes.Count; i++)
        {
            Node node = _allNodes[i];
            if (node == null || !node.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (excludeSafeZones && node.IsSafeZoneNode)
            {
                continue;
            }

            float sqrDist = (node.Position - position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                closest = node;
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _allNodes.Count; i++)
        {
            if (_allNodes[i] != null)
            {
                Gizmos.DrawWireSphere(_allNodes[i].transform.position, 0.4f);
            }
        }
    }
}