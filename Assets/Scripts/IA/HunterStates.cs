using System.Collections.Generic;
using AI.FSM;
using UnityEngine;


public class HunterSearchState : State
{
    private HunterAgent _hunter;
    private List<Node> _path = new List<Node>();
    private int _pathIndex = 0;
    private bool _reachedCoto = false;
    private float _spawnTimer = 0f;
    private const float SPAWN_INTERVAL = 5f;

    public HunterSearchState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine) => _hunter = hunter;

    public override void Enter()
    {
        _reachedCoto = false;
        _spawnTimer = 0f;
        CalculatePathToHuntingGround();
    }

    public override void Update()
    {
        if (_hunter.DetectedWolf != null)
        {
            StateMachine.ChangeState(HunterStates.Flee);
            return;
        }

        if (_hunter.CurrentGathers >= _hunter.RequiredGathers)
        {
            StateMachine.ChangeState(HunterStates.Exit);
            return;
        }

        _hunter.DetectBoids();

        if (_hunter.TargetBoid != null && _hunter.TargetBoid.gameObject.activeInHierarchy)
        {
            if (_hunter.IsTargetBoidEliminated)
            {
                StateMachine.ChangeState(HunterStates.Gather);
                return;
            }
            else if (_hunter.TBATimer >= _hunter.TBA)
            {
                StateMachine.ChangeState(HunterStates.Attack);
                return;
            }
        }

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= SPAWN_INTERVAL)
        {
            _spawnTimer = 0f;
            SpawnInterestObject();
        }

        if (!_reachedCoto)
        {
            _hunter.MoveAlongPath(_path, ref _pathIndex);

            if (_pathIndex >= _path.Count || (ZoneManager.Instance != null && ZoneManager.Instance.HuntingGroundPoint != null && Vector3.Distance(_hunter.transform.position, ZoneManager.Instance.HuntingGroundPoint.position) <= 6f))
            {
                _reachedCoto = true;
                CalculateRandomCotoPath();
            }
        }
        else
        {
            _hunter.MoveAlongPath(_path, ref _pathIndex);
            if (_pathIndex >= _path.Count)
            {
                CalculateRandomCotoPath();
            }
        }
    }

    private void CalculatePathToHuntingGround()
    {
        if (ZoneManager.Instance != null && ZoneManager.Instance.HuntingGroundPoint != null && Graph.Instance != null)
        {
            Node startNode = Graph.Instance.GetClosestNode(_hunter.transform.position);
            IReadOnlyList<Node> allNodes = Graph.Instance.AllNodes;

            List<Node> cotoNodes = new List<Node>();
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] != null && !allNodes[i].IsSafeZoneNode && 
                    Vector3.Distance(allNodes[i].Position, ZoneManager.Instance.HuntingGroundPoint.position) <= 8f)
                {
                    cotoNodes.Add(allNodes[i]);
                }
            }

            if (cotoNodes.Count > 0)
            {
                Node randomTarget = cotoNodes[Random.Range(0, cotoNodes.Count)];
                _path = AStar.GetPath(startNode, randomTarget);
                _pathIndex = 0;
            }
        }
    }

    private void CalculateRandomCotoPath()
    {
        if (ZoneManager.Instance == null || ZoneManager.Instance.HuntingGroundPoint == null || Graph.Instance == null) return;

        Node startNode = Graph.Instance.GetClosestNode(_hunter.transform.position);
        IReadOnlyList<Node> allNodes = Graph.Instance.AllNodes;

        List<Node> cotoNodes = new List<Node>();
        for (int i = 0; i < allNodes.Count; i++)
        {
            if (allNodes[i] != null && !allNodes[i].IsSafeZoneNode && Vector3.Distance(allNodes[i].Position, ZoneManager.Instance.HuntingGroundPoint.position) <= 8f)
            {
                cotoNodes.Add(allNodes[i]);
            }
        }

        if (cotoNodes.Count > 0)
        {
            Node randomTarget = cotoNodes[Random.Range(0, cotoNodes.Count)];
            _path = AStar.GetPath(startNode, randomTarget);
            _pathIndex = 0;
        }
    }

    private void SpawnInterestObject()
    {
        if (_hunter == null) return;
        _hunter.TrySpawnInterestObject();
    }
}


public class HunterFleeState : State
{
    private HunterAgent _hunter;
    private List<Node> _path = new List<Node>();
    private int _pathIndex = 0;
    private float _waitTimer = 0f;
    private bool _reachedSafeZone = false;

    public HunterFleeState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine) => _hunter = hunter;

    public override void Enter()
    {
        _reachedSafeZone = false;
        _waitTimer = 0f;
        _hunter.IsInSafeZone = false;
        CalculatePathToClosestSafeZone();
    }

    public override void Update()
    {
        if (!_reachedSafeZone)
        {
            _hunter.MoveAlongPath(_path, ref _pathIndex);

            Node currentNode = Graph.Instance != null ? Graph.Instance.GetClosestNode(_hunter.transform.position) : null;
            if ((currentNode != null && currentNode.IsSafeZoneNode) || _pathIndex >= _path.Count)
            {
                _reachedSafeZone = true;
                _hunter.IsInSafeZone = true;
            }
        }
        else
        {
            _waitTimer += Time.deltaTime;
            if (_waitTimer >= _hunter.SafeZoneWaitTime)
            {
                _hunter.IsInSafeZone = false;

                if (_hunter.CurrentGathers >= _hunter.RequiredGathers)
                    StateMachine.ChangeState(HunterStates.Exit);
                else
                    StateMachine.ChangeState(HunterStates.Search);
            }
        }
    }

    public override void Exit()
    {
        _hunter.IsInSafeZone = false;
    }

    private void CalculatePathToClosestSafeZone()
    {
        if (ZoneManager.Instance == null || ZoneManager.Instance.SafeZonePoints.Count == 0 || Graph.Instance == null) return;

        Transform closestZone = null;
        float minScore = float.MaxValue;
        Node startNode = Graph.Instance.GetClosestNode(_hunter.transform.position);

        for (int i = 0; i < ZoneManager.Instance.SafeZonePoints.Count; i++)
        {
            Transform zonePoint = ZoneManager.Instance.SafeZonePoints[i];
            if (zonePoint == null) continue;

            Node targetNode = Graph.Instance.GetClosestNode(zonePoint.position);
            List<Node> tempPath = AStar.GetPath(startNode, targetNode);

            if (tempPath != null && tempPath.Count > 0)
            {
                float pathLength = 0f;
                for (int p = 0; p < tempPath.Count - 1; p++)
                {
                    pathLength += Vector3.Distance(tempPath[p].Position, tempPath[p + 1].Position);
                }

                float penalty = 0f;
                if (_hunter.DetectedWolf != null)
                {
                    Vector3 wolfPos = _hunter.DetectedWolf.position;
                    wolfPos.y = 0f;

                    
                    float minWolfDist = float.MaxValue;
                    for (int p = 0; p < tempPath.Count; p++)
                    {
                        Vector3 nodePos = tempPath[p].Position;
                        nodePos.y = 0f;
                        float d = Vector3.Distance(nodePos, wolfPos);
                        if (d < minWolfDist) minWolfDist = d;
                    }

                    
                    if (minWolfDist < 4.5f)
                    {
                        penalty += (4.5f - minWolfDist) * 100f + 200f;
                    }

                    
                    if (tempPath.Count > 1)
                    {
                        Vector3 startPos = _hunter.transform.position;
                        startPos.y = 0f;
                        Vector3 moveDir = (tempPath[1].Position - startPos).normalized;
                        Vector3 wolfDir = (wolfPos - startPos).normalized;
                        if (Vector3.Dot(moveDir, wolfDir) > 0.2f)
                        {
                            penalty += 300f;
                        }
                    }
                }

                float score = pathLength + penalty;

                if (score < minScore)
                {
                    minScore = score;
                    closestZone = zonePoint;
                    _path = tempPath;
                }
            }
        }

        _pathIndex = 0;
    }
}


public class HunterAttackState : State
{
    private HunterAgent _hunter;

    public HunterAttackState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine) => _hunter = hunter;

    public override void Update()
    {
        if (_hunter.DetectedWolf != null)
        {
            StateMachine.ChangeState(HunterStates.Flee);
            return;
        }

       
        if (_hunter.TargetBoid == null || !_hunter.TargetBoid.gameObject.activeInHierarchy || !_hunter.IsBoidVisible(_hunter.TargetBoid))
        {
            _hunter.TargetBoid = null;
            StateMachine.ChangeState(HunterStates.Search);
            return;
        }

        float dist = Vector3.Distance(_hunter.transform.position, _hunter.TargetBoid.position);

        
        if (dist <= _hunter.MeleeAttackRadius)
        {
            _hunter.ResetTBATimer();
            _hunter.AttackAndEliminateBoid(_hunter.TargetBoid, true); 
            StateMachine.ChangeState(HunterStates.Gather);
        }
        else if (dist <= _hunter.RangeAttackRadius)
        {
            _hunter.ResetTBATimer();
            _hunter.AttackAndEliminateBoid(_hunter.TargetBoid, false); 
            StateMachine.ChangeState(HunterStates.Gather);
        }
        else
        {
            Vector3 steering = _hunter.Pursuit(_hunter.TargetBoid);
            if (steering.sqrMagnitude > 0.001f)
            {
                Vector3 moveDir = steering.normalized;
                moveDir.y = 0f;
                float step = _hunter.MoveSpeed * Time.deltaTime;

                Vector3 rayOrigin = _hunter.transform.position + Vector3.up * 0.5f;
                if (Physics.Raycast(rayOrigin, moveDir, out RaycastHit hit, step + 0.4f, _hunter.ObstacleMask))
                {
                    Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;
                    slideDir.y = 0f;
                    if (slideDir.sqrMagnitude > 0.01f) moveDir = slideDir;
                }

                _hunter.transform.position += moveDir * step;
                _hunter.transform.forward = moveDir;
            }
        }
    }
}

public class HunterGatherState : State
{
    private HunterAgent _hunter;
    private float _gatherTimer = 0f;
    private const float GATHER_DURATION = 2.0f;

    public HunterGatherState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine) => _hunter = hunter;

    public override void Enter()
    {
        _gatherTimer = 0f;
        _hunter.GatherCountdown = GATHER_DURATION;
    }

    public override void Update()
    {
        if (_hunter.DetectedWolf != null)
        {
            StateMachine.ChangeState(HunterStates.Flee);
            return;
        }

        if (_hunter.TargetBoid != null && _hunter.TargetBoid.gameObject.activeInHierarchy)
        {
            Vector3 arriveSteering = _hunter.Arrive(_hunter.TargetBoid.position, 2.0f);
            if (arriveSteering.sqrMagnitude > 0.001f)
            {
                Vector3 moveDir = arriveSteering.normalized;
                moveDir.y = 0f;
                float step = _hunter.MoveSpeed * Time.deltaTime;

                Vector3 rayOrigin = _hunter.transform.position + Vector3.up * 0.5f;
                if (Physics.Raycast(rayOrigin, moveDir, out RaycastHit hit, step + 0.4f, _hunter.ObstacleMask))
                {
                    Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;
                    slideDir.y = 0f;
                    if (slideDir.sqrMagnitude > 0.01f) moveDir = slideDir;
                }

                _hunter.transform.position += moveDir * step;
            }
        }

        _gatherTimer += Time.deltaTime;
        _hunter.GatherCountdown = Mathf.Max(0f, GATHER_DURATION - _gatherTimer);

        if (_gatherTimer >= GATHER_DURATION)
        {
            if (_hunter.TargetBoid != null)
            {
                _hunter.CollectBoid(_hunter.TargetBoid);
            }

            _hunter.AddGatherCount();
            _hunter.TargetBoid = null;
            _hunter.IsTargetBoidEliminated = false;

            if (_hunter.CurrentGathers >= _hunter.RequiredGathers)
                StateMachine.ChangeState(HunterStates.Exit);
            else
                StateMachine.ChangeState(HunterStates.Search);
        }
    }
}


public class HunterExitState : State
{
    private HunterAgent _hunter;
    private List<Node> _path = new List<Node>();
    private int _pathIndex = 0;

    public HunterExitState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine) => _hunter = hunter;

    public override void Enter() => CalculatePathToExit();

    public override void Update()
    {
        if (_hunter.DetectedWolf != null)
        {
            StateMachine.ChangeState(HunterStates.Flee);
            return;
        }

        _hunter.MoveAlongPath(_path, ref _pathIndex);

        if (_pathIndex >= _path.Count)
        {
            _hunter.SnapToSpawnPoint();
            _hunter.ResetGathers();
            StateMachine.ChangeState(HunterStates.Search);
        }
    }

    private void CalculatePathToExit()
    {
        if (ZoneManager.Instance != null && ZoneManager.Instance.ExitPoint != null && Graph.Instance != null)
        {
            Node startNode = Graph.Instance.GetClosestNode(_hunter.transform.position);
            Node targetNode = Graph.Instance.GetClosestNode(ZoneManager.Instance.ExitPoint.position);
            if (startNode != null && targetNode != null)
            {
                _path = AStar.GetPath(startNode, targetNode);
                _pathIndex = 0;
            }
        }
    }
}


public class HunterDeadState : State
{
    private HunterAgent _hunter;
    private float _timer = 0f;

    public HunterDeadState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine) => _hunter = hunter;

    public override void Enter()
    {
        _timer = 0f;
        _hunter.SetHunterVisible(false);
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _hunter.RespawnTime)
        {
            _hunter.SnapToSpawnPoint();
            _hunter.ResetGathers();
            _hunter.SetHunterVisible(true);
            StateMachine.ChangeState(HunterStates.Search);
        }
    }
}
