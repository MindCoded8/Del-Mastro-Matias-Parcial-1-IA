using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    [Header("Base Movement Stats")]
    [SerializeField] protected float _maxSpeed = 5f;
    [SerializeField] protected float _maxSteering = 5f;

    protected Vector3 _velocity;

    // Propiedad con lectura (get) y escritura (set) pública para permitir modificaciones desde la FSM
    public Vector3 Velocity { get => _velocity; set => _velocity = value; }
    public float MaxSpeed => _maxSpeed;

    protected virtual void Update()
    {
        // Método virtual extensible por clases derivadas
    }
}
