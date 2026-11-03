using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum WolfState { Patrol, Alerted, Chase, SearchingArea }

[RequireComponent(typeof(FieldOfView))]
public class WolfAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 3.2f;
    [SerializeField] private float _rotationSpeed = 8f;
    [SerializeField] private float _captureRadius = 1.5f;

    [Header("Patrol Settings")]
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();
    [SerializeField] private bool _isPingPong = false;
    [SerializeField] private LayerMask _obstacleMask;
    private int _currentWaypointIndex = 0;
    private int _patrolDirection = 1;

    [Header("Target")]
    [SerializeField] private Transform _hunterTarget;

    private FieldOfView _fov;
    private WolfState _currentState = WolfState.Patrol;
    private Vector3 _lastKnownHunterPosition;
    private List<Node> _currentPath = new List<Node>();
    private int _pathIndex = 0;
    private float _howlEffectTimer = 0f;

    public WolfState CurrentState => _currentState;

    private void Awake()
    {
        _fov = GetComponent<FieldOfView>();
    }

    private void Start()
    {
        SubscribeToHowl();
    }

    private void OnEnable()
    {
        SubscribeToHowl();
    }

    private void OnDisable()
    {
        if (WolfManager.Instance != null)
        {
            WolfManager.Instance.OnWolfHowlAlert -= ReceiveHowlAlert;
        }
    }

    private void SubscribeToHowl()
    {
        if (WolfManager.Instance != null)
        {
            WolfManager.Instance.OnWolfHowlAlert -= ReceiveHowlAlert;
            WolfManager.Instance.OnWolfHowlAlert += ReceiveHowlAlert;
        }
    }

    private Transform GetEffectiveHunterTarget()
    {
        if (_hunterTarget != null) return _hunterTarget;
        if (WolfManager.Instance != null && WolfManager.Instance.TargetHunter != null)
        {
            return WolfManager.Instance.TargetHunter.transform;
        }
        return null;
    }

    private void Update()
    {
        if (_howlEffectTimer > 0) _howlEffectTimer -= Time.deltaTime;

        CheckPerception();

        switch (_currentState)
        {
            case WolfState.Patrol:
                ExecutePatrol();
                break;
            case WolfState.Alerted:
            case WolfState.SearchingArea:
                ExecuteFollowPath();
                break;
            case WolfState.Chase:
                ExecuteDirectChase();
                break;
        }

        CheckCapture();
    }

    private void CheckPerception()
    {
        Transform target = GetEffectiveHunterTarget();
        if (target == null || !target.gameObject.activeInHierarchy) return;

        HunterAgent hunter = target.GetComponent<HunterAgent>();
        if (hunter != null && (hunter.IsInSafeZone || hunter.IsDead))
        {
            if (_currentState != WolfState.Patrol)
            {
                _currentState = WolfState.Patrol;
                RecalculatePatrolPath();
            }
            return;
        }

        bool canSee = _fov.CanSeeTarget(target);

        if (canSee)
        {
            _lastKnownHunterPosition = target.position;

            if (_currentState != WolfState.Chase)
            {
                _currentState = WolfState.Chase;
                _howlEffectTimer = 2.0f;

                if (WolfManager.Instance != null)
                {
                    WolfManager.Instance.BroadcastHowlAlert(_lastKnownHunterPosition);
                }
            }
        }
        else if (_currentState == WolfState.Chase)
        {
            _currentState = WolfState.SearchingArea;
            CalculateAStarPathTo(_lastKnownHunterPosition);
        }
    }

    private void ReceiveHowlAlert(Vector3 hunterPos)
    {
        Transform target = GetEffectiveHunterTarget();
        if (target != null)
        {
            HunterAgent hunter = target.GetComponent<HunterAgent>();
            if (hunter != null && (hunter.IsInSafeZone || hunter.IsDead)) return;
        }

        if (_currentState != WolfState.Chase)
        {
            _lastKnownHunterPosition = hunterPos;
            _currentState = WolfState.Alerted;
            _howlEffectTimer = 1.5f;
            CalculateAStarPathTo(_lastKnownHunterPosition);
        }
    }

    private void ExecutePatrol()
    {
        if (_waypoints == null || _waypoints.Count == 0) return;

        Transform targetWaypoint = _waypoints[_currentWaypointIndex];
        if (targetWaypoint == null) return;

        if (_currentPath != null && _currentPath.Count > 0 && _pathIndex < _currentPath.Count)
        {
            ExecuteFollowPath();
            return;
        }

        float sqrDist = (targetWaypoint.position - transform.position).sqrMagnitude;

        if (sqrDist <= 0.8f * 0.8f)
        {
            AdvanceWaypointIndex();
            targetWaypoint = _waypoints[_currentWaypointIndex];
            RecalculatePatrolPath();
        }
        else
        {
            Vector3 dir = (targetWaypoint.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, targetWaypoint.position);

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, dist, _obstacleMask))
            {
                RecalculatePatrolPath();
            }
            else
            {
                MoveTowards(targetWaypoint.position);
            }
        }
    }

    private void RecalculatePatrolPath()
    {
        if (_waypoints == null || _waypoints.Count == 0) return;
        Transform targetWaypoint = _waypoints[_currentWaypointIndex];
        if (targetWaypoint != null)
        {
            CalculateAStarPathTo(targetWaypoint.position);
        }
    }

    private void AdvanceWaypointIndex()
    {
        if (!_isPingPong)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
        }
        else
        {
            _currentWaypointIndex += _patrolDirection;
            if (_currentWaypointIndex >= _waypoints.Count)
            {
                _currentWaypointIndex = Mathf.Max(0, _waypoints.Count - 2);
                _patrolDirection = -1;
            }
            else if (_currentWaypointIndex < 0)
            {
                _currentWaypointIndex = Mathf.Min(1, _waypoints.Count - 1);
                _patrolDirection = 1;
            }
        }
    }

    private void ExecuteDirectChase()
    {
        Transform target = GetEffectiveHunterTarget();
        if (target != null)
        {
            MoveTowards(target.position);
        }
    }

    private void ExecuteFollowPath()
    {
        if (_currentPath == null || _pathIndex >= _currentPath.Count)
        {
            _currentPath.Clear();
            _pathIndex = 0;
            return;
        }

        Vector3 targetPos = _currentPath[_pathIndex].Position;
        float sqrDist = (targetPos - transform.position).sqrMagnitude;

        if (sqrDist <= 0.6f * 0.6f)
        {
            _pathIndex++;
            if (_pathIndex >= _currentPath.Count)
            {
                _currentPath.Clear();
                _pathIndex = 0;
                return;
            }
        }

        MoveTowards(_currentPath[_pathIndex].Position);
    }

    private void CalculateAStarPathTo(Vector3 destination)
    {
        Node startNode = Graph.Instance != null ? Graph.Instance.GetClosestNode(transform.position, excludeSafeZones: true) : null;
        Node targetNode = Graph.Instance != null ? Graph.Instance.GetClosestNode(destination, excludeSafeZones: true) : null;

        if (startNode != null && targetNode != null)
        {
            _currentPath = AStar.GetPath(startNode, targetNode, excludeSafeZones: true);
            _pathIndex = 0;
        }
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;
        Vector3 dir = (targetPosition - transform.position).normalized;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            transform.position += transform.forward * _moveSpeed * Time.deltaTime;
        }
    }

    private void CheckCapture()
    {
        Transform target = GetEffectiveHunterTarget();
        if (target == null || !target.gameObject.activeInHierarchy) return;

        HunterAgent hunter = target.GetComponent<HunterAgent>();
        if (hunter != null && hunter.IsDead) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist <= _captureRadius * _captureRadius)
        {
            if (hunter != null)
            {
                hunter.DieAndRespawn();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Color stateColor = Color.gray;
        string stateLabel = "PATROL";

        switch (_currentState)
        {
            case WolfState.Patrol:
                stateColor = Color.white;
                stateLabel = "PATROL";
                break;
            case WolfState.Alerted:
                stateColor = Color.magenta;
                stateLabel = "ALERTED";
                break;
            case WolfState.SearchingArea:
                stateColor = Color.yellow;
                stateLabel = "SEARCHING";
                break;
            case WolfState.Chase:
                stateColor = Color.red;
                stateLabel = "CHASE";
                break;
        }

        Vector3 headPos = transform.position + Vector3.up * 1.8f;
        Gizmos.color = stateColor;
        Gizmos.DrawSphere(headPos, 0.45f);

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = stateColor;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        Handles.Label(headPos + Vector3.up * 0.5f, stateLabel, style);
#endif

        if (_howlEffectTimer > 0)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, (2.0f - _howlEffectTimer) * 5f);
        }

        if (_currentState == WolfState.Alerted || _currentState == WolfState.SearchingArea || _currentState == WolfState.Chase)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastKnownHunterPosition + Vector3.up * 0.2f, 0.5f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, _lastKnownHunterPosition + Vector3.up * 0.2f);
        }

        if (_currentPath != null && _currentPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                if (_currentPath[i] != null && _currentPath[i + 1] != null)
                {
                    Gizmos.DrawLine(_currentPath[i].Position + Vector3.up * 0.3f, _currentPath[i + 1].Position + Vector3.up * 0.3f);
                }
            }
        }
    }
}
