using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    [Header("Base Agent Stats")]
    [SerializeField] protected float _maxSpeed = 5f;
    [SerializeField] protected float _maxSteering = 5f;

    protected Vector3 _velocity;

    public Vector3 Velocity
    {
        get => _velocity;
        set => _velocity = value;
    }

    public float MaxSpeed => _maxSpeed;
    public float MaxSteering => _maxSteering;

    protected abstract void Update();
}