
////using System.Collections.Generic;
////using UnityEngine;

////public enum HunterStates { Patrol, Attack, Gather }

////public class HunterAgent : Agent
////{
////    public static HunterAgent Instance { get; private set; }

////    [Header("Hunter FSM Parameters")]
////    [SerializeField] private float _perceptionRadius = 10f;
////    [SerializeField] private float _rangeAttackRadius = 5f;
////    [SerializeField] private float _meleeAttackRadius = 2f;
////    [SerializeField] private float _tba = 3f;
////    [SerializeField] private float _spawnInterestObjectInterval = 4f;

////    [Header("References")]
////    [SerializeField] private AgentFeedback _feedback;
////    [SerializeField] private GameObject _interestObjectPrefab;
////    [SerializeField] private List<Transform> _waypoints = new List<Transform>();

////    private StateMachine _stateMachine;
////    private BoidAgent _targetBoid;
////    private float _tbaTimer = 0f;

////    public float PerceptionRadius => _perceptionRadius;
////    public float RangeAttackRadius => _rangeAttackRadius;
////    public float MeleeAttackRadius => _meleeAttackRadius;
////    public float TBA => _tba;
////    public float TBATimer => _tbaTimer;
////    public float SpawnInterestObjectInterval => _spawnInterestObjectInterval;
////    public GameObject InterestObjectPrefab => _interestObjectPrefab;
////    public List<Transform> Waypoints => _waypoints;

////    public BoidAgent TargetBoid
////    {
////        get => _targetBoid;
////        set => _targetBoid = value;
////    }

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

////        if (_feedback == null)
////        {
////            _feedback = GetComponentInChildren<AgentFeedback>();
////        }

////        _tbaTimer = _tba;

////        _stateMachine = new StateMachine();
////        PatrolState patrolState = new PatrolState(this, _stateMachine);
////        AttackState attackState = new AttackState(this, _stateMachine);
////        GatherState gatherState = new GatherState(this, _stateMachine);

////        _stateMachine.RegisterState(HunterStates.Patrol, patrolState);
////        _stateMachine.RegisterState(HunterStates.Attack, attackState);
////        _stateMachine.RegisterState(HunterStates.Gather, gatherState);

////        _stateMachine.ChangeState(HunterStates.Patrol);
////    }

////    private void OnDestroy()
////    {
////        if (Instance == this)
////        {
////            Instance = null;
////        }
////    }

////    protected override void Update()
////    {
////        _tbaTimer += Time.deltaTime;
////        _stateMachine.Update();

////        _velocity.y = 0f;
////        transform.position += _velocity * Time.deltaTime;
////        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);

////        if (_velocity.sqrMagnitude > 0.001f)
////        {
////            transform.forward = _velocity.normalized;
////        }

////        if (Bounds.Instance != null)
////        {
////            transform.position = Bounds.Instance.OutOfBounds(transform.position);
////        }
////    }

////    public void UpdateHeadFeedback(string stateLabel, Color labelColor)
////    {
////        if (_feedback != null)
////        {
////            _feedback.SetStateText(stateLabel, labelColor);
////            _feedback.SetColor(labelColor);
////        }
////    }

////    public void ResetTBATimer()
////    {
////        _tbaTimer = 0f;
////    }

////    public void StopMovement()
////    {
////        _velocity = Vector3.zero;
////    }

////    public Vector3 Seek(Vector3 targetPosition)
////    {
////        targetPosition.y = 0f;
////        Vector3 direction = (targetPosition - transform.position).normalized;
////        Vector3 desired = direction * _maxSpeed;
////        return ApplySteeringFormula(desired);
////    }

////    public Vector3 Arrive(Vector3 targetPosition)
////    {
////        targetPosition.y = 0f;
////        Vector3 direction = targetPosition - transform.position;
////        float sqrDistance = direction.sqrMagnitude;
////        float stopRadius = 0.2f;

////        if (sqrDistance < stopRadius * stopRadius)
////        {
////            _velocity = Vector3.zero;
////            return Vector3.zero;
////        }

////        float distance = Mathf.Sqrt(sqrDistance);
////        float slowingDistance = 3.0f;
////        float targetSpeed = _maxSpeed * (distance / slowingDistance);
////        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

////        Vector3 desired = direction.normalized * desiredSpeed;
////        return ApplySteeringFormula(desired);
////    }

////    private Vector3 ApplySteeringFormula(Vector3 desiredVelocity)
////    {
////        desiredVelocity.y = 0f;
////        Vector3 steering = desiredVelocity - _velocity;
////        steering.y = 0f;
////        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
////    }

////    private void OnDrawGizmos()
////    {
////        Gizmos.color = Color.yellow;
////        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);

////        Gizmos.color = Color.blue;
////        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

////        Gizmos.color = Color.red;
////        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);
////    }
////}

//using System.Collections.Generic;
//using UnityEngine;

//public enum HunterStates
//{
//    Patrol,
//    Attack,
//    Gather
//}

//public class HunterAgent : Agent
//{
//    [Header("Hunter FSM Parameters")]
//    [SerializeField] private float _perceptionRadius = 10f;
//    [SerializeField] private float _rangeAttackRadius = 5f;
//    [SerializeField] private float _meleeAttackRadius = 2f;
//    [SerializeField] private float _tba = 3f;
//    [SerializeField] private float _spawnInterestObjectInterval = 4f;

//    [Header("Layers & References")]
//    [SerializeField] private LayerMask _boidLayerMask;
//    [SerializeField] private AgentFeedback _feedback;
//    [SerializeField] private GameObject _interestObjectPrefab;
//    [SerializeField] private List<Transform> _waypoints = new List<Transform>();

//    private StateMachine _stateMachine;
//    private BoidAgent _targetBoid;
//    private float _tbaTimer = 0f;

//    public float PerceptionRadius => _perceptionRadius;
//    public float RangeAttackRadius => _rangeAttackRadius;
//    public float MeleeAttackRadius => _meleeAttackRadius;
//    public float TBA => _tba;
//    public float TBATimer => _tbaTimer;
//    public float SpawnInterestObjectInterval => _spawnInterestObjectInterval;
//    public LayerMask BoidLayerMask => _boidLayerMask;
//    public GameObject InterestObjectPrefab => _interestObjectPrefab;
//    public List<Transform> Waypoints => _waypoints;

//    public BoidAgent TargetBoid
//    {
//        get => _targetBoid;
//        set => _targetBoid = value;
//    }

//    private void Awake()
//    {
//        InitializeFeedback();
//        InitializeFSM();
//    }

//    private void InitializeFeedback()
//    {
//        if (_feedback == null)
//        {
//            _feedback = GetComponentInChildren<AgentFeedback>();
//        }
//        _tbaTimer = _tba;
//    }

//    private void InitializeFSM()
//    {
//        _stateMachine = new StateMachine();

//        PatrolState patrolState = new PatrolState(this, _stateMachine);
//        AttackState attackState = new AttackState(this, _stateMachine);
//        GatherState gatherState = new GatherState(this, _stateMachine);

//        _stateMachine.RegisterState(HunterStates.Patrol, patrolState);
//        _stateMachine.RegisterState(HunterStates.Attack, attackState);
//        _stateMachine.RegisterState(HunterStates.Gather, gatherState);

//        _stateMachine.ChangeState(HunterStates.Patrol);
//    }

//    protected override void Update()
//    {
//        _tbaTimer += Time.deltaTime;
//        _stateMachine.Update();

//        _velocity.y = 0f;
//        transform.position += _velocity * Time.deltaTime;
//        transform.position = new Vector3(
//            transform.position.x,
//            0f,
//            transform.position.z
//        );

//        if (_velocity.sqrMagnitude > 0.001f)
//        {
//            transform.forward = _velocity.normalized;
//        }

//        if (Bounds.Instance != null)
//        {
//            transform.position = Bounds.Instance.OutOfBounds(transform.position);
//        }
//    }

//    public void UpdateHeadFeedback(string stateLabel, Color labelColor)
//    {
//        if (_feedback != null)
//        {
//            _feedback.SetStateText(stateLabel, labelColor);
//            _feedback.SetColor(labelColor);
//        }
//    }

//    public void ResetTBATimer()
//    {
//        _tbaTimer = 0f;
//    }

//    public void StopMovement()
//    {
//        _velocity = Vector3.zero;
//    }

//    public Vector3 Seek(Vector3 targetPosition)
//    {
//        targetPosition.y = 0f;
//        Vector3 direction = (targetPosition - transform.position).normalized;
//        Vector3 desired = direction * _maxSpeed;
//        return ApplySteeringFormula(desired);
//    }

//    public Vector3 Arrive(Vector3 targetPosition)
//    {
//        targetPosition.y = 0f;
//        Vector3 direction = targetPosition - transform.position;
//        float sqrDistance = direction.sqrMagnitude;

//        if (sqrDistance < 0.04f)
//        {
//            _velocity = Vector3.zero;
//            return Vector3.zero;
//        }

//        float distance = Mathf.Sqrt(sqrDistance);
//        float slowingDistance = 3.0f;
//        float targetSpeed = _maxSpeed * (distance / slowingDistance);
//        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

//        return ApplySteeringFormula(direction.normalized * desiredSpeed);
//    }

//    private Vector3 ApplySteeringFormula(Vector3 desiredVelocity)
//    {
//        desiredVelocity.y = 0f;
//        Vector3 steering = desiredVelocity - _velocity;
//        steering.y = 0f;
//        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
//    }

//    private void /*OnDrawGizmos*/()
//    {
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);

//        Gizmos.color = Color.blue;
//        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

//        Gizmos.color = Color.red;
//        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);
//    }
//}


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
    [Header("Hunter FSM Parameters")]
    [SerializeField] private float _perceptionRadius = 10f;
    [SerializeField] private float _rangeAttackRadius = 5f;
    [SerializeField] private float _meleeAttackRadius = 2f;
    [SerializeField] private float _tba = 3f;
    [SerializeField] private float _spawnInterestObjectInterval = 4f;

    [Header("Layers & References")]
    [SerializeField] private LayerMask _boidLayerMask;
    [SerializeField] private AgentFeedback _feedback;
    [SerializeField] private GameObject _interestObjectPrefab;
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();

    private StateMachine _stateMachine;
    private BoidAgent _targetBoid;
    private float _tbaTimer = 0f;

    public float PerceptionRadius => _perceptionRadius;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float TBA => _tba;
    public float TBATimer => _tbaTimer;
    public float SpawnInterestObjectInterval => _spawnInterestObjectInterval;
    public LayerMask BoidLayerMask => _boidLayerMask;
    public GameObject InterestObjectPrefab => _interestObjectPrefab;
    public List<Transform> Waypoints => _waypoints;

    public BoidAgent TargetBoid
    {
        get => _targetBoid;
        set => _targetBoid = value;
    }

    private void Awake()
    {
        InitializeFeedback();
        InitializeFSM();
    }

    private void InitializeFeedback()
    {
        if (_feedback == null)
        {
            _feedback = GetComponentInChildren<AgentFeedback>();
        }
        _tbaTimer = _tba;
    }

    private void InitializeFSM()
    {
        _stateMachine = new StateMachine();

        PatrolState patrolState = new PatrolState(this, _stateMachine);
        AttackState attackState = new AttackState(this, _stateMachine);
        GatherState gatherState = new GatherState(this, _stateMachine);

        _stateMachine.RegisterState(HunterStates.Patrol, patrolState);
        _stateMachine.RegisterState(HunterStates.Attack, attackState);
        _stateMachine.RegisterState(HunterStates.Gather, gatherState);

        _stateMachine.ChangeState(HunterStates.Patrol);
    }

    protected override void Update()
    {
        _tbaTimer += Time.deltaTime;
        _stateMachine.Update();

        _velocity.y = 0f;
        transform.position += _velocity * Time.deltaTime;
        transform.position = new Vector3(
            transform.position.x,
            0f,
            transform.position.z
        );

        if (_velocity.sqrMagnitude > 0.001f)
        {
            transform.forward = _velocity.normalized;
        }

        if (Bounds.Instance != null)
        {
            transform.position = Bounds.Instance.OutOfBounds(transform.position);
        }
    }

    public void UpdateHeadFeedback(string stateLabel, Color labelColor)
    {
        if (_feedback != null)
        {
            _feedback.SetStateText(stateLabel, labelColor);
            _feedback.SetColor(labelColor);
        }
    }

    public void TriggerShotVisual(Vector3 targetPosition)
    {
        if (_feedback != null)
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            targetPosition.y = origin.y;
            _feedback.ShowShotBeam(origin, targetPosition);
        }
    }

    public void ResetTBATimer()
    {
        _tbaTimer = 0f;
    }

    public void StopMovement()
    {
        _velocity = Vector3.zero;
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        targetPosition.y = 0f;
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 desired = direction * _maxSpeed;
        return ApplySteeringFormula(desired);
    }

    public Vector3 Arrive(Vector3 targetPosition)
    {
        targetPosition.y = 0f;
        Vector3 direction = targetPosition - transform.position;
        float sqrDistance = direction.sqrMagnitude;

        if (sqrDistance < 0.04f)
        {
            _velocity = Vector3.zero;
            return Vector3.zero;
        }

        float distance = Mathf.Sqrt(sqrDistance);
        float slowingDistance = 3.0f;
        float targetSpeed = _maxSpeed * (distance / slowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        return ApplySteeringFormula(direction.normalized * desiredSpeed);
    }

    private Vector3 ApplySteeringFormula(Vector3 desiredVelocity)
    {
        desiredVelocity.y = 0f;
        Vector3 steering = desiredVelocity - _velocity;
        steering.y = 0f;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);
    }
}