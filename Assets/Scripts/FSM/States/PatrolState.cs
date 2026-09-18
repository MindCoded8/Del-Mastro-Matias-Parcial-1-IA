

using UnityEngine;

public class PatrolState : State
{
    private HunterAgent _hunter;
    private int _currentWaypointIndex = 0;
    private float _spawnTimer = 0f;

    public PatrolState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _spawnTimer = 0f;
        _hunter.UpdateHeadFeedback("PATROL", Color.orange);
    }

    public override void Update()
    {
        BoidAgent deadBoid = FindDeadBoid();
        if (deadBoid != null)
        {
            _hunter.TargetBoid = deadBoid;
            StateMachine.ChangeState(HunterStates.Gather);
            return;
        }

        if (_hunter.TBATimer >= _hunter.TBA)
        {
            BoidAgent targetBoid = FindTargetBoid();
            if (targetBoid != null)
            {
                _hunter.TargetBoid = targetBoid;
                StateMachine.ChangeState(HunterStates.Attack);
                return;
            }
        }

        PatrolWaypoints();
        HandleInterestObjectSpawning();
    }

    private void PatrolWaypoints()
    {
        if (_hunter.Waypoints == null || _hunter.Waypoints.Count == 0) return;

        Transform targetWaypoint = _hunter.Waypoints[_currentWaypointIndex];
        Vector3 targetPos = targetWaypoint.position;
        targetPos.y = 0f;

        Vector3 steering = _hunter.Arrive(targetPos);
        _hunter.Velocity += steering;

        Vector3 currentPos = _hunter.transform.position;
        currentPos.y = 0f;

        float sqrDistance = (targetPos - currentPos).sqrMagnitude;
        float stopRadius = 0.3f;

        if (sqrDistance <= stopRadius * stopRadius)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _hunter.Waypoints.Count;
        }
    }

    private void HandleInterestObjectSpawning()
    {
        if (InterestObject.AllInterestObjects.Count >= 5) return;

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _hunter.SpawnInterestObjectInterval)
        {
            _spawnTimer = 0f;

            if (_hunter.InterestObjectPrefab != null)
            {
                Vector3 spawnPos = _hunter.transform.position;
                spawnPos.y = 0f;

                if (Bounds.Instance != null)
                {
                    spawnPos = Bounds.Instance.OutOfBounds(spawnPos);
                }

                Object.Instantiate(_hunter.InterestObjectPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    private BoidAgent FindDeadBoid()
    {
        float sqrPerception = _hunter.PerceptionRadius * _hunter.PerceptionRadius;
        Vector3 hunterPos = _hunter.transform.position;
        hunterPos.y = 0f;

        for (int i = 0; i < BoidAgent.AllBoids.Count; i++)
        {
            BoidAgent boid = BoidAgent.AllBoids[i];
            if (boid != null && boid.IsDead && !boid.IsCollected && boid.gameObject.activeInHierarchy)
            {
                Vector3 boidPos = boid.transform.position;
                boidPos.y = 0f;

                if ((boidPos - hunterPos).sqrMagnitude <= sqrPerception)
                {
                    return boid;
                }
            }
        }

        return null;
    }

    private BoidAgent FindTargetBoid()
    {
        float sqrPerception = _hunter.PerceptionRadius * _hunter.PerceptionRadius;
        Vector3 hunterPos = _hunter.transform.position;
        hunterPos.y = 0f;

        BoidAgent closest = null;
        float minSqrDistance = float.MaxValue;

        for (int i = 0; i < BoidAgent.AllBoids.Count; i++)
        {
            BoidAgent boid = BoidAgent.AllBoids[i];
            if (boid != null && !boid.IsDead && boid.gameObject.activeInHierarchy)
            {
                Vector3 boidPos = boid.transform.position;
                boidPos.y = 0f;

                float sqrDist = (boidPos - hunterPos).sqrMagnitude;

                if (sqrDist <= sqrPerception && sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    closest = boid;
                }
            }
        }

        return closest;
    }
}