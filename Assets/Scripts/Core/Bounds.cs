using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private float height = 30f;
    [SerializeField] private float width = 60f;
    [SerializeField] private bool drawGizmos = true;

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

    public Vector3 OutOfBounds(Vector3 position)
    {
        Vector3 newPosition = position;

        if (position.x > width / 2f) newPosition.x = -width / 2f;
        if (position.x < -width / 2f) newPosition.x = width / 2f;
        if (position.z > height / 2f) newPosition.z = -height / 2f;
        if (position.z < -height / 2f) newPosition.z = height / 2f;

        return newPosition;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0f, height));
    }
}