////using System.Collections.Generic;
////using UnityEngine;

////public class Graph : MonoBehaviour
////{
////    public static Graph Instance { get; private set; }

////    [Header("Nodes in Graph")]
////    [SerializeField] private List<Node> _allNodes = new List<Node>();

////    public IReadOnlyList<Node> AllNodes => _allNodes;

////    private void Awake()
////    {
////        if (Instance == null)
////        {
////            Instance = this;
////        }
////        else
////        {
////            Destroy(gameObject);
////            return;
////        }

////        AutoCollectNodes();
////    }

////    private void OnDestroy()
////    {
////        if (Instance == this)
////        {
////            Instance = null;
////        }
////    }

////    public void AutoCollectNodes()
////    {
////        _allNodes.Clear();
////        Node[] nodes = GetComponentsInChildren<Node>();
////        _allNodes.AddRange(nodes);
////    }

////    public Node GetClosestNode(Vector3 position, bool excludeSafeZones = false)
////    {
////        position.y = 0f;
////        Node closest = null;
////        float minSqrDist = float.MaxValue;

////        for (int i = 0; i < _allNodes.Count; i++)
////        {
////            Node node = _allNodes[i];
////            if (node == null || !node.gameObject.activeInHierarchy)
////            {
////                continue;
////            }

////            if (excludeSafeZones && node.IsSafeZoneNode)
////            {
////                continue;
////            }

////            float sqrDist = (node.Position - position).sqrMagnitude;
////            if (sqrDist < minSqrDist)
////            {
////                minSqrDist = sqrDist;
////                closest = node;
////            }
////        }

////        return closest;
////    }

////    private void OnDrawGizmosSelected()
////    {
////        Gizmos.color = Color.yellow;
////        for (int i = 0; i < _allNodes.Count; i++)
////        {
////            if (_allNodes[i] != null)
////            {
////                Gizmos.DrawWireSphere(_allNodes[i].transform.position, 0.4f);
////            }
////        }
////    }
////}



//using System.Collections.Generic;
//using UnityEngine;

//public class Graph : MonoBehaviour
//{
//    public static Graph Instance { get; private set; }

//    [Header("Settings")]
//    [SerializeField] private float _maxConnectionDistance = 7.0f;
//    [SerializeField] private LayerMask _obstacleMask;
//    [SerializeField] private Vector3 _raycastOffset = new Vector3(0f, 0.5f, 0f);

//    [Header("Nodes in Graph")]
//    [SerializeField] private List<Node> _allNodes = new List<Node>();

//    public IReadOnlyList<Node> AllNodes => _allNodes;

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Destroy(gameObject);
//            return;
//        }

//        AutoCollectNodes();
//    }

//    private void OnDestroy()
//    {
//        if (Instance == this)
//        {
//            Instance = null;
//        }
//    }

//    public void AutoCollectNodes()
//    {
//        _allNodes.Clear();
//        Node[] nodes = GetComponentsInChildren<Node>();
//        _allNodes.AddRange(nodes);
//    }

//    [ContextMenu("Auto Connect Neighbors")]
//    public void AutoConnectNeighbors()
//    {
//        AutoCollectNodes();
//        ClearAllNeighbors();

//        float maxDistSqr = _maxConnectionDistance * _maxConnectionDistance;
//        int nodeCount = _allNodes.Count;

//        for (int i = 0; i < nodeCount; i++)
//        {
//            Node nodeA = _allNodes[i];
//            if (nodeA == null) continue;

//            Vector3 startPos = nodeA.Position + _raycastOffset;

//            for (int j = i + 1; j < nodeCount; j++)
//            {
//                Node nodeB = _allNodes[j];
//                if (nodeB == null) continue;

//                Vector3 endPos = nodeB.Position + _raycastOffset;
//                float sqrDist = (startPos - endPos).sqrMagnitude;

//                if (sqrDist <= maxDistSqr)
//                {
//                    if (!Physics.Linecast(startPos, endPos, _obstacleMask))
//                    {
//                        nodeA.AddNeighbor(nodeB);
//                        nodeB.AddNeighbor(nodeA);
//                    }
//                }
//            }
//        }

//        Debug.Log($"¡Conexiones generadas automáticamente para {nodeCount} nodos!");
//    }

//    public Node GetClosestNode(Vector3 position, bool excludeSafeZones = false)
//    {
//        position.y = 0f;
//        Node closest = null;
//        float minSqrDist = float.MaxValue;

//        for (int i = 0; i < _allNodes.Count; i++)
//        {
//            Node node = _allNodes[i];
//            if (node == null || !node.gameObject.activeInHierarchy) continue;
//            if (excludeSafeZones && node.IsSafeZoneNode) continue;

//            float sqrDist = (node.Position - position).sqrMagnitude;
//            if (sqrDist < minSqrDist)
//            {
//                minSqrDist = sqrDist;
//                closest = node;
//            }
//        }

//        return closest;
//    }

//    private void ClearAllNeighbors()
//    {
//        for (int i = 0; i < _allNodes.Count; i++)
//        {
//            if (_allNodes[i] != null)
//            {
//                _allNodes[i].ClearNeighbors();
//            }
//        }
//    }
//}



using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public static Graph Instance { get; private set; }

    [Header("Graph Auto-Connect Settings")]
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _maxConnectionDistance = 4.5f; // Distancia máxima entre nodos contiguos
    [SerializeField] private float _raycastHeightOffset = 0.5f;   // Elevación para no chocar con el suelo
    [SerializeField] private float _agentRadius = 0.3f;           // Grosor del personaje para evitar esquinas estrechas

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

    /// <summary>
    /// Busca y recopila automáticamente todos los componentes Node que sean hijos de este GameObject.
    /// </summary>
    public void AutoCollectNodes()
    {
        _allNodes.Clear();
        Node[] nodes = GetComponentsInChildren<Node>();
        _allNodes.AddRange(nodes);
    }

    /// <summary>
    /// Limpia todas las listas de vecinos de los nodos en la jerarquía.
    /// </summary>
    [ContextMenu("Clear All Neighbors")]
    public void ClearAllNeighbors()
    {
        AutoCollectNodes();
        for (int i = 0; i < _allNodes.Count; i++)
        {
            if (_allNodes[i] != null)
            {
                _allNodes[i].ClearNeighbors();
            }
        }
        Debug.Log("Graph: Se han limpiado todas las conexiones del grafo.");
    }

    /// <summary>
    /// Conecta automáticamente los nodos mediante SphereCast respetando el radio del agente y los Colliders de la capa Obstacle.
    /// </summary>
    [ContextMenu("Auto Connect Neighbors")]
    public void AutoConnectNeighbors()
    {
        AutoCollectNodes();
        ClearAllNeighbors();

        int connectionCount = 0;

        for (int i = 0; i < _allNodes.Count; i++)
        {
            Node nodeA = _allNodes[i];
            if (nodeA == null)
            {
                continue;
            }

            for (int j = 0; j < _allNodes.Count; j++)
            {
                Node nodeB = _allNodes[j];
                if (nodeB == null || nodeA == nodeB)
                {
                    continue;
                }

                // Calculamos dirección y distancia entre los dos nodos
                Vector3 startPos =
                    nodeA.Position + Vector3.up * _raycastHeightOffset;
                Vector3 endPos =
                    nodeB.Position + Vector3.up * _raycastHeightOffset;
                Vector3 direction = endPos - startPos;
                float distance = direction.magnitude;

                // 1. Validar la distancia máxima de conexión
                if (
                    distance <= _maxConnectionDistance
                    && distance > 0.01f
                )
                {
                    // 2. Usar SphereCast con el radio del personaje para evitar pasajes estrechos o diagonales rozando muros
                    bool hitObstacle = Physics.SphereCast(
                        startPos,
                        _agentRadius,
                        direction.normalized,
                        out RaycastHit hit,
                        distance,
                        _obstacleMask
                    );

                    if (!hitObstacle)
                    {
                        nodeA.AddNeighbor(nodeB);
                        connectionCount++;
                    }
                }
            }
        }

        Debug.Log(
            $"Graph: Conexiones limpias generadas. " +
            $"Total de nodos: {_allNodes.Count}. " +
            $"Conexiones totales: {connectionCount}."
        );
    }

    /// <summary>
    /// Búsqueda optimizada del nodo más cercano a una posición dada.
    /// Cumple con la regla de la cátedra: usa sqrMagnitude para evitar el costo computacional de Mathf.Sqrt.
    /// </summary>
    public Node GetClosestNode(
        Vector3 position,
        bool excludeSafeZones = false
    )
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
                Gizmos.DrawWireSphere(
                    _allNodes[i].transform.position,
                    0.4f
                );
            }
        }
    }
}