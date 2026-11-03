using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] private List<Node> _neighbors = new List<Node>();
    [SerializeField] private bool _isSafeZoneNode = false;

    public float GCost { get; set; }
    public float HCost { get; set; }
    public float FCost => GCost + HCost;
    public Node Parent { get; set; }

    public IReadOnlyList<Node> Neighbors => _neighbors;
    public bool IsSafeZoneNode => _isSafeZoneNode;

    public Vector3 Position
    {
        get
        {
            Vector3 pos = transform.position;
            pos.y = 0f;
            return pos;
        }
    }

    public void ClearNeighbors() => _neighbors.Clear();

    public void AddNeighbor(Node neighbor)
    {
        if (neighbor != null && !_neighbors.Contains(neighbor))
        {
            _neighbors.Add(neighbor);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isSafeZoneNode ? Color.green : Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.35f);

        if (_neighbors != null)
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < _neighbors.Count; i++)
            {
                if (_neighbors[i] != null)
                {
                    Gizmos.DrawLine(transform.position, _neighbors[i].transform.position);
                }
            }
        }
    }
}
