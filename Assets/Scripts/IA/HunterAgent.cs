using System.Collections.Generic;
using AI.FSM;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum HunterStates { Search, Attack, Gather, Flee, Exit, Dead }

public class HunterAgent : Agent
{
    [Header("Hunter Parameters")]
    [SerializeField] private float _moveSpeed = 4.5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private int _requiredGathers = 3;
    [SerializeField] private float _respawnTime = 5f;
    [SerializeField] private float _safeZoneWaitTime = 3f;

    [Header("Perception & Attack Ranges")]
    [SerializeField] private float _visionRadius = 8f;
    [SerializeField] private float _rangeAttackRadius = 6f;
    [SerializeField] private float _meleeAttackRadius = 2.5f;
    [SerializeField] private float _tba = 2.5f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _wolfLayerMask;
    [SerializeField] private LayerMask _boidLayerMask;

    [Header("References")]
    [SerializeField] private GameObject _interestObjectPrefab;
    [SerializeField] private Material _lineMaterial;
    private List<GameObject> _spawnedInterestObjects = new List<GameObject>();

    private StateMachine _stateMachine;
    private int _currentGathers = 0;
    private float _tbaTimer = 0f;
    private Transform _detectedWolf;
    private Transform _targetBoid;
    private bool _isTargetBoidEliminated = false;
    private bool _isInSafeZone = false;
    private bool _isDead = false;
    private float _gatherCountdown = 0f;

    private LineRenderer _lineRenderer;
    private float _shotVisualTimer = 0f;

    public float MoveSpeed => _moveSpeed;
    public int RequiredGathers => _requiredGathers;
    public int CurrentGathers => _currentGathers;
    public float RespawnTime => _respawnTime;
    public float SafeZoneWaitTime => _safeZoneWaitTime;
    public float VisionRadius => _visionRadius;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float TBA => _tba;
    public float TBATimer => _tbaTimer;
    public GameObject InterestObjectPrefab => _interestObjectPrefab;

    public Transform TargetBoid { get => _targetBoid; set => _targetBoid = value; }
    public bool IsTargetBoidEliminated { get => _isTargetBoidEliminated; set => _isTargetBoidEliminated = value; }
    public Transform DetectedWolf => _detectedWolf;
    public bool IsInSafeZone { get => _isInSafeZone; set => _isInSafeZone = value; }
    public bool IsDead => _isDead;
    public float GatherCountdown { get => _gatherCountdown; set => _gatherCountdown = value; }

    private void Awake()
    {
        InitializeFSM();
        SetupLineRenderer();
    }

    private void Start()
    {
        if (WolfManager.Instance != null)
        {
            WolfManager.Instance.RegisterHunter(this);
        }
    }

    private void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        _lineRenderer.startWidth = 0.1f;
        _lineRenderer.endWidth = 0.1f;
        if (_lineMaterial != null)
        {
            _lineRenderer.material = _lineMaterial;
        }
        _lineRenderer.startColor = Color.red;
        _lineRenderer.endColor = Color.yellow;
        _lineRenderer.enabled = false;
    }

    private void InitializeFSM()
    {
        _stateMachine = new StateMachine();

        _stateMachine.RegisterState(HunterStates.Search, new HunterSearchState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Attack, new HunterAttackState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Gather, new HunterGatherState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Flee, new HunterFleeState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Exit, new HunterExitState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Dead, new HunterDeadState(this, _stateMachine));

        _stateMachine.ChangeState(HunterStates.Search);
    }

    protected override void Update()
    {
        if (_shotVisualTimer > 0)
        {
            _shotVisualTimer -= Time.deltaTime;
            if (_shotVisualTimer <= 0 && _lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

        _tbaTimer += Time.deltaTime;

        if (!_isDead)
        {
            DetectWolves();
            DetectBoids();
        }

        _stateMachine.Update();
    }

    public void TriggerShotVisual(Vector3 targetPos)
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.SetPosition(0, transform.position + Vector3.up * 0.5f);
            _lineRenderer.SetPosition(1, targetPos + Vector3.up * 0.5f);
            _lineRenderer.enabled = true;
            _shotVisualTimer = 0.25f;
        }
    }

    public void TrySpawnInterestObject()
    {
        for (int i = _spawnedInterestObjects.Count - 1; i >= 0; i--)
        {
            if (_spawnedInterestObjects[i] == null || !_spawnedInterestObjects[i].activeInHierarchy)
            {
                _spawnedInterestObjects.RemoveAt(i);
            }
        }

        if (_spawnedInterestObjects.Count >= 5) return;

        Vector3 spawnPos = transform.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        GameObject newObj = null;

        if (_interestObjectPrefab != null)
        {
            newObj = Instantiate(_interestObjectPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newObj.transform.position = spawnPos + Vector3.up * 0.4f;
            newObj.transform.localScale = Vector3.one * 0.6f;
            Renderer r = newObj.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.yellow;
            if (newObj.GetComponent<InterestObject>() == null)
            {
                newObj.AddComponent<InterestObject>();
            }
        }

        if (newObj != null)
        {
            _spawnedInterestObjects.Add(newObj);
        }
    }

    private void DetectWolves()
    {
        _detectedWolf = null;
        if (_isInSafeZone) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, _visionRadius, _wolfLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Vector3 dir = (hits[i].transform.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, hits[i].transform.position);

            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, dist, _obstacleMask))
            {
                _detectedWolf = hits[i].transform;
                break;
            }
        }
    }

    public void DetectBoids()
    {
        if (_targetBoid != null && _targetBoid.gameObject.activeInHierarchy && !_isTargetBoidEliminated) return;

        _targetBoid = null;

        int mask = _boidLayerMask.value != 0 ? _boidLayerMask.value : ~0;
        Collider[] hits = Physics.OverlapSphere(transform.position, _visionRadius, mask);
        float closestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].gameObject.activeInHierarchy) continue;

            BoidAgent boid = hits[i].GetComponentInParent<BoidAgent>();
            if (boid == null || boid.IsCollected) continue;

            Vector3 dir = (hits[i].transform.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, hits[i].transform.position);

            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, dist, _obstacleMask))
            {
                if (dist < closestDist)
                {
                    closestDist = dist;
                    _targetBoid = boid.transform;
                }
            }
        }
    }

    public void AttackAndEliminateBoid(Transform boidTransform)
    {
        if (boidTransform == null) return;

        TriggerShotVisual(boidTransform.position);

        BoidAgent boid = boidTransform.GetComponentInParent<BoidAgent>();
        if (boid != null)
        {
            boid.TakeDamage(100f);
        }
        else
        {
            MonoBehaviour[] scripts = boidTransform.GetComponents<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] != this) scripts[i].enabled = false;
            }
            Renderer r = boidTransform.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.red;
        }

        _isTargetBoidEliminated = true;
    }

    public void CollectBoid(Transform boidTransform)
    {
        if (boidTransform == null) return;

        BoidAgent boid = boidTransform.GetComponentInParent<BoidAgent>();
        if (boid != null)
        {
            boid.CollectByHunter();
        }
        else
        {
            boidTransform.gameObject.SetActive(false);
        }
    }

    public void SetHunterVisible(bool visible)
    {
        _isDead = !visible;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = visible;
        }
    }

    public void MoveAlongPath(List<Node> path, ref int pathIndex)
    {
        if (path == null || pathIndex >= path.Count) return;

        Vector3 targetPos = path[pathIndex].Position;
        targetPos.y = transform.position.y;

        Vector3 dir = (targetPos - transform.position).normalized;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            transform.position += transform.forward * _moveSpeed * Time.deltaTime;
        }

        if ((targetPos - transform.position).sqrMagnitude <= 0.4f * 0.4f)
        {
            pathIndex++;
        }
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;
        Vector3 desired = (targetPosition - transform.position).normalized * _moveSpeed;
        Vector3 steering = desired - _velocity;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }

    public Vector3 Arrive(Vector3 targetPosition, float slowingDistance = 3f)
    {
        targetPosition.y = transform.position.y;
        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;

        if (distance < 0.1f)
        {
            _velocity = Vector3.zero;
            return Vector3.zero;
        }

        float targetSpeed = _moveSpeed * (distance / slowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, _moveSpeed);
        Vector3 desired = direction.normalized * desiredSpeed;
        Vector3 steering = desired - _velocity;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }

    public void AddGatherCount()
    {
        _currentGathers++;
    }

    public void ResetGathers() => _currentGathers = 0;
    public void ResetTBATimer() => _tbaTimer = 0f;

    public void DieAndRespawn()
    {
        _stateMachine.ChangeState(HunterStates.Dead);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _visionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);

        Color labelColor = Color.green;
        string stateText = "HUNTER: SEARCH";
        if (_stateMachine != null && _stateMachine.CurrentState != null)
        {
            string sName = _stateMachine.CurrentState.GetType().Name.Replace("Hunter", "").Replace("State", "").ToUpper();
            stateText = $"HUNTER: {sName} ({_currentGathers}/{_requiredGathers})";
            if (sName == "GATHER") stateText += $" ({_gatherCountdown:F1}s)";
        }

        Vector3 headPos = transform.position + Vector3.up * 2.0f;
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = labelColor;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        Handles.Label(headPos, stateText, style);
#endif
    }
}
