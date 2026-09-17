using System.Collections.Generic;
using UnityEngine;

public class PatrolState : State
{
    private HunterAgent _hunter;
    private int _currentWaypointIndex = 0;
    private float _spawnTimer = 0f;
    private List<InterestObject> _spawnedObjects = new List<InterestObject>();

    public PatrolState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _spawnTimer = 0f;
    }

    public override void Update()
    {
        // 1. Prioridad: Detección de Boids caidos para recolectar (Gather).
        BoidAgent deadBoid = FindDeadBoid();
        if (deadBoid != null)
        {
            _hunter.TargetBoid = deadBoid;
            stateMachine.ChangeState(HunterStates.Gather);
            return;
        }

        // 2. Detección de Boids vivos para atacar (Attack) si el TBA Finalizó
        if (_hunter.TBATimer >= _hunter.TBA)
        {
            BoidAgent targetBoid = FindTargetBoid();
            if (targetBoid != null)
            {
                _hunter.TargetBoid = targetBoid;
                stateMachine.ChangeState(HunterStates.Attack);
                return;
            }
        }

        // 3. Recorrido de Waypoints
        PatrolWaypoints();

        // 4. Generación periódica de Objetos de Interés (Máximo 5 activos)
        HandleInterestObjectSpawning();
    }

    private void PatrolWaypoints()
    {
        if (_hunter.Waypoints == null || _hunter.Waypoints.Count == 0) return;

        Transform targetWaypoint = _hunter.Waypoints[_currentWaypointIndex];
        Vector3 steering = _hunter.Arrive(targetWaypoint.position);
        _hunter.Velocity += steering;

        float sqrDistance = (targetWaypoint.position - _hunter.transform.position).sqrMagnitude;
        float stopRadius = 0.3f;

        if (sqrDistance < stopRadius * stopRadius)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _hunter.Waypoints.Count;
        }
    }

    private void HandleInterestObjectSpawning()
    {
        _spawnedObjects.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

        if (_spawnedObjects.Count >= 5) return;

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _hunter.SpawnInterestObjectInterval)
        {
            _spawnTimer = 0f;

            if (_hunter.InterestObjectPrefab != null)
            {
                InterestObject newObj = Object.Instantiate(_hunter.InterestObjectPrefab, _hunter.transform.position, Quaternion.identity);
                _spawnedObjects.Add(newObj);

                // Notificar a los Boids mediante autoregistro estático
                for (int i = 0; i < BoidAgent.AllBoids.Count; i++)
                {
                    if (BoidAgent.AllBoids[i] != null && !BoidAgent.AllBoids[i].IsDead)
                    {
                        BoidAgent.AllBoids[i].RegisterInterestObject(newObj);
                    }
                }
            }
        }
    }

    private BoidAgent FindDeadBoid()
    {
        float sqrPerception = _hunter.PerceptionRadius * _hunter.PerceptionRadius;

        for (int i = 0; i < BoidAgent.AllBoids.Count; i++)
        {
            BoidAgent boid = BoidAgent.AllBoids[i];
            if (boid != null && boid.IsDead && !boid.IsCollected && boid.gameObject.activeInHierarchy)
            {
                if ((boid.transform.position - _hunter.transform.position).sqrMagnitude < sqrPerception)
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
        BoidAgent closest = null;
        float minSqrDistance = float.MaxValue;

        for (int i = 0; i < BoidAgent.AllBoids.Count; i++)
        {
            BoidAgent boid = BoidAgent.AllBoids[i];
            if (boid != null && !boid.IsDead && boid.gameObject.activeInHierarchy)
            {
                float sqrDist = (boid.transform.position - _hunter.transform.position).sqrMagnitude;
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