using System.Collections.Generic;
using UnityEngine;

public enum HunterStates
{
    Patrol,
    Attack,
    Gather
}

public class HunterAgent : Agent
{
    [Header("Hunter Mandatory Variables")]
    [SerializeField] private float _tba = 3f;
    [SerializeField] private float _rangeAttackRadius = 8f;
    [SerializeField] private float _meleeAttackRadius = 2f;
    [SerializeField] private float _perceptionRadius = 10f;

    [Header("Patrol & Interest Object Settings")]
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();
    [SerializeField] private InterestObject _interestObjectPrefab;
    [SerializeField] private float _spawnInterestObjectInterval = 6f;

    private StateMachine _stateMachine;
    private float _tbaTimer;
    private BoidAgent _targetBoid;

    public float TBA => _tba;
    public float TBATimer { get => _tbaTimer; set => _tbaTimer = value; }
    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float PerceptionRadius => _perceptionRadius;
    public List<Transform> Waypoints => _waypoints;
    public InterestObject InterestObjectPrefab => _interestObjectPrefab;
    public float SpawnInterestObjectInterval => _spawnInterestObjectInterval;
    public StateMachine FSM => _stateMachine;
    public BoidAgent TargetBoid { get => _targetBoid; set => _targetBoid = value; }

    protected virtual void Awake()
    {
        _stateMachine = new StateMachine();
        _tbaTimer = _tba;

        // Registrar estados en la FSM
        _stateMachine.RegisterState(HunterStates.Patrol, new PatrolState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Attack, new AttackState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Gather, new GatherState(this, _stateMachine));

        // Estado inicial obligatorio
        _stateMachine.ChangeState(HunterStates.Patrol);
    }

    protected override void Update()
    {
        if (_tbaTimer < _tba)
        {
            _tbaTimer += Time.deltaTime;
        }

        // Notificar presencia del cazador a Boids cercanos
        NotifyBoids();

        _stateMachine.Update();

        if (_velocity.sqrMagnitude > 0.001f)
        {
            _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
            transform.position += _velocity * Time.deltaTime;
            transform.forward = _velocity.normalized;
        }
    }

    private void NotifyBoids()
    {
        float sqrPerception = _perceptionRadius * _perceptionRadius;
        for (int i = 0; i < BoidAgent.AllBoids.Count; i++)
        {
            BoidAgent boid = BoidAgent.AllBoids[i];
            if (boid != null && !boid.IsDead)
            {
                if ((boid.transform.position - transform.position).sqrMagnitude <= sqrPerception)
                {
                    boid.RegisterHunter(this);
                }
            }
        }
    }

    public Vector3 ApplySteeringFormula(Vector3 desiredVelocity)
    {
        Vector3 steering = desiredVelocity - _velocity;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 desired = direction * _maxSpeed;
        return ApplySteeringFormula(desired);
    }

    public Vector3 Arrive(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        float sqrDistance = direction.sqrMagnitude;
        float stopRadius = 0.2f;

        if (sqrDistance < stopRadius * stopRadius)
        {
            _velocity = Vector3.zero;
            return Vector3.zero;
        }

        float distance = Mathf.Sqrt(sqrDistance);
        float slowingDistance = 3.0f;
        float targetSpeed = _maxSpeed * (distance / slowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        Vector3 desired = direction.normalized * desiredSpeed;
        return ApplySteeringFormula(desired);
    }

    public void StopMovement()
    {
        _velocity = Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        // Visualización de Rangos requeridos (Gizmos/Feedback)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);

        if (_stateMachine != null && _stateMachine.CurrentState != null)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.2f,
                $"State: {_stateMachine.CurrentState.GetType().Name}"
            );
#endif
        }
    }
}
