using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public static Graph Instance { get; private set; }

    [Header("Graph Settings")]
    [SerializeField] private List<Node> _allNodes = new List<Node>();
    [SerializeField] private float _maxConnectionDistance = 4.5f;
    [SerializeField] private bool _allowDiagonals = false;
    [SerializeField] private float _agentRadius = 0.3f;
    [SerializeField] private float _raycastHeightOffset = 0.5f;
    [SerializeField] private LayerMask _obstacleMask;

    public IReadOnlyList<Node> AllNodes => _allNodes;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

#if UNITY_EDITOR
    [ContextMenu("Populate Nodes From Children")]
    public void PopulateNodesFromChildren()
    {
        _allNodes.Clear();
        Node[] childNodes = GetComponentsInChildren<Node>();
        _allNodes.AddRange(childNodes);
        Debug.Log($"[Graph] Se asignaron {_allNodes.Count} nodos hijos al Grafo.");
    }
#endif

    [ContextMenu("Auto Connect Neighbors")]
    public void AutoConnectNeighbors()
    {
        ClearAllConnections();

        int connectionCount = 0;
        for (int i = 0; i < _allNodes.Count; i++)
        {
            Node nodeA = _allNodes[i];
            if (nodeA == null) continue;

            for (int j = 0; j < _allNodes.Count; j++)
            {
                Node nodeB = _allNodes[j];
                if (nodeB == null || nodeA == nodeB) continue;

                Vector3 startPos = nodeA.Position + Vector3.up * _raycastHeightOffset;
                Vector3 endPos = nodeB.Position + Vector3.up * _raycastHeightOffset;
                Vector3 direction = endPos - startPos;
                float distance = direction.magnitude;

                if (distance <= _maxConnectionDistance && distance > 0.01f)
                {
                    Vector3 dirNormalized = direction.normalized;

                    if (!_allowDiagonals && Mathf.Abs(dirNormalized.x) > 0.3f && Mathf.Abs(dirNormalized.z) > 0.3f)
                    {
                        continue;
                    }

                    if (!Physics.SphereCast(startPos, _agentRadius, dirNormalized, out RaycastHit hit, distance, _obstacleMask))
                    {
                        nodeA.AddNeighbor(nodeB);
                        connectionCount++;
                    }
                }
            }
        }
        Debug.Log($"[Graph] Conexiones completadas. Nodos en lista: {_allNodes.Count}. Conexiones totales creadas: {connectionCount}.");
    }

    [ContextMenu("Clear All Connections")]
    public void ClearAllConnections()
    {
        for (int i = 0; i < _allNodes.Count; i++)
        {
            if (_allNodes[i] != null)
            {
                _allNodes[i].ClearNeighbors();
            }
        }
        Debug.Log("[Graph] Se limpiaron todas las conexiones de los nodos.");
    }

    public Node GetClosestNode(Vector3 position, bool excludeSafeZones = false)
    {
        position.y = 0f;
        Node closest = null;
        float minSqrDist = float.MaxValue;

        for (int i = 0; i < _allNodes.Count; i++)
        {
            Node node = _allNodes[i];
            if (node == null || !node.gameObject.activeInHierarchy) continue;
            if (excludeSafeZones && node.IsSafeZoneNode) continue;

            float sqrDist = (node.Position - position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                closest = node;
            }
        }

        return closest;
    }
}
