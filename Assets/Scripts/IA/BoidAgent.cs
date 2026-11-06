using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidAgent : Agent
{
    [Header("Life & Health Stats")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _respawnDelay = 3f;

    [Header("Perception Radii (Ajustados para conejos)")]
    [SerializeField] private float _separationRadius = 1.2f;
    [SerializeField] private float _flockingRadius = 4.0f;
    [SerializeField] private float _hunterDetectionRadius = 8.0f;
    [SerializeField] private float _interestObjectDetectionRadius = 10.0f;
    [SerializeField] private float _interactDistance = 0.8f;
    [SerializeField] private float _slowingDistance = 3.0f;

    [Header("Flocking Weights")]
    [SerializeField, Range(0f, 5f)] private float _separationWeight = 3.0f;
    [SerializeField, Range(0f, 5f)] private float _alignmentWeight = 1.0f;
    [SerializeField, Range(0f, 5f)] private float _cohesionWeight = 1.0f;

    [Header("Detection Layers")]
    [SerializeField] private LayerMask _boidLayerMask;
    [SerializeField] private LayerMask _hunterLayerMask;
    [SerializeField] private LayerMask _interestObjectLayerMask;

    private AgentFeedback _feedback;
    private bool _isDead = false;
    private bool _isCollected = false;

    private static readonly Collider[] _overlapBuffer = new Collider[64];

    public bool IsDead => _isDead;
    public bool IsCollected => _isCollected;

    protected virtual void Awake()
    {
        _currentHealth = _maxHealth;
        _feedback = GetComponentInChildren<AgentFeedback>();
        InitializeRandomVelocity();
    }

    protected override void Update()
    {
        if (_isDead || _isCollected)
        {
            _velocity = Vector3.zero;
            return;
        }

        ExecuteMovement();
    }

    private void ExecuteMovement()
    {
        Vector3 steering = CalculateDecisionSteering();

        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
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

    private Vector3 CalculateDecisionSteering()
    {
        HunterAgent threatHunter = DetectLocalHunter();
        if (threatHunter != null)
        {
            UpdateVisualFeedback(isEvading: true, isFeeding: false);
            return Evade(threatHunter);
        }

        InterestObject closestObject = DetectLocalInterestObject();
        if (closestObject != null)
        {
            Vector3 targetPos = closestObject.transform.position;
            targetPos.y = 0f;

            if ((targetPos - GetFlattenedPosition()).sqrMagnitude <= _interactDistance * _interactDistance)
            {
                closestObject.TakeDamage(20f * Time.deltaTime);
            }

            UpdateVisualFeedback(isEvading: false, isFeeding: true);
            return Arrive(targetPos);
        }

        UpdateVisualFeedback(isEvading: false, isFeeding: false);
        return CalculateFlocking();
    }

    private HunterAgent DetectLocalHunter()
    {
        int mask = _hunterLayerMask.value != 0 ? _hunterLayerMask.value : ~0;
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            _hunterDetectionRadius,
            _overlapBuffer,
            mask
        );

        HunterAgent closestHunter = null;
        float minSqrDist = float.MaxValue;
        Vector3 myPos = GetFlattenedPosition();

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            HunterAgent hunter = col.GetComponentInParent<HunterAgent>();
            if (hunter != null && hunter.gameObject.activeInHierarchy && !hunter.IsDead && !hunter.IsInSafeZone)
            {
                Vector3 hunterPos = hunter.transform.position;
                hunterPos.y = 0f;

                float sqrDist = (hunterPos - myPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closestHunter = hunter;
                }
            }
        }

        return closestHunter;
    }

    private InterestObject DetectLocalInterestObject()
    {
        int mask = _interestObjectLayerMask.value != 0 ? _interestObjectLayerMask.value : ~0;
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            _interestObjectDetectionRadius,
            _overlapBuffer,
            mask
        );

        InterestObject closest = null;
        float minSqrDist = float.MaxValue;
        Vector3 myPos = GetFlattenedPosition();

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            InterestObject obj = col.GetComponentInParent<InterestObject>();
            if (obj != null && obj.gameObject.activeInHierarchy)
            {
                Vector3 objPos = obj.transform.position;
                objPos.y = 0f;

                float sqrDist = (objPos - myPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = obj;
                }
            }
        }

        return closest;
    }

    public Vector3 CalculateFlocking()
    {
        List<BoidAgent> localNeighbors = GetLocalBoidNeighbors();

        Vector3 separation = CalculateSeparation(localNeighbors, _separationRadius) * _separationWeight;
        Vector3 alignment = CalculateAlignment(localNeighbors, _flockingRadius) * _alignmentWeight;
        Vector3 cohesion = CalculateCohesion(localNeighbors, _flockingRadius) * _cohesionWeight;

        return separation + alignment + cohesion;
    }

    private List<BoidAgent> GetLocalBoidNeighbors()
    {
        List<BoidAgent> neighbors = new List<BoidAgent>();
        int mask = _boidLayerMask.value != 0 ? _boidLayerMask.value : ~0;
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            _flockingRadius,
            _overlapBuffer,
            mask
        );

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            BoidAgent boid = col.GetComponentInParent<BoidAgent>();
            if (boid != null && boid != this && !boid.IsDead && boid.gameObject.activeInHierarchy)
            {
                if (!neighbors.Contains(boid))
                {
                    neighbors.Add(boid);
                }
            }
        }

        return neighbors;
    }

    private Vector3 CalculateSeparation(List<BoidAgent> neighbors, float radius)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;
        float sqrRadius = radius * radius;
        Vector3 myPos = GetFlattenedPosition();

        for (int i = 0; i < neighbors.Count; i++)
        {
            BoidAgent neighbor = neighbors[i];
            Vector3 neighborPos = neighbor.transform.position;
            neighborPos.y = 0f;

            float distSqr = (neighborPos - myPos).sqrMagnitude;
            if (distSqr <= sqrRadius)
            {
                Vector3 pushVector = myPos - neighborPos;
                float d = Mathf.Sqrt(distSqr);
                if (d < 0.001f)
                {
                    pushVector = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                    d = 0.1f;
                }

                // Fuerza repulsiva inversamente proporcional a la distancia
                desired += pushVector.normalized / Mathf.Max(d, 0.05f);
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        desired /= count;
        return ApplySteeringFormula(desired.normalized * _maxSpeed);
    }

    private Vector3 CalculateAlignment(List<BoidAgent> neighbors, float radius)
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;

        for (int i = 0; i < neighbors.Count; i++)
        {
            BoidAgent neighbor = neighbors[i];
            Vector3 vel = neighbor.Velocity;
            vel.y = 0f;

            averageVelocity += vel;
            count++;
        }

        if (count == 0) return Vector3.zero;

        averageVelocity /= count;
        return ApplySteeringFormula(averageVelocity.normalized * _maxSpeed);
    }

    private Vector3 CalculateCohesion(List<BoidAgent> neighbors, float radius)
    {
        Vector3 centerOfMass = Vector3.zero;
        int count = 0;

        for (int i = 0; i < neighbors.Count; i++)
        {
            BoidAgent neighbor = neighbors[i];
            Vector3 pos = neighbor.transform.position;
            pos.y = 0f;

            centerOfMass += pos;
            count++;
        }

        if (count == 0) return Vector3.zero;

        centerOfMass /= count;
        return Seek(centerOfMass);
    }

    private Vector3 ApplySteeringFormula(Vector3 desiredVelocity)
    {
        desiredVelocity.y = 0f;
        Vector3 steering = desiredVelocity - _velocity;
        steering.y = 0f;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        targetPosition.y = 0f;
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        return ApplySteeringFormula(direction.normalized * _maxSpeed);
    }

    public Vector3 Flee(Vector3 targetPosition)
    {
        targetPosition.y = 0f;
        Vector3 direction = transform.position - targetPosition;
        direction.y = 0f;
        return ApplySteeringFormula(direction.normalized * _maxSpeed);
    }

    public Vector3 Arrive(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        float sqrDist = direction.sqrMagnitude;

        if (sqrDist < 0.04f)
        {
            _velocity = Vector3.zero;
            return Vector3.zero;
        }

        float distance = Mathf.Sqrt(sqrDist);
        float targetSpeed = _maxSpeed * (distance / Mathf.Max(_slowingDistance, 0.1f));
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        return ApplySteeringFormula(direction.normalized * desiredSpeed);
    }

    public Vector3 Evade(Agent target)
    {
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;

        float combinedSpeed = _maxSpeed + target.Velocity.magnitude;
        if (combinedSpeed <= 0.001f) return Flee(target.transform.position);

        float distance = Mathf.Sqrt(direction.sqrMagnitude);
        float predictionTime = distance / combinedSpeed;

        Vector3 futurePos = target.transform.position + (target.Velocity * predictionTime);
        futurePos.y = 0f;

        return Flee(futurePos);
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        _velocity = Vector3.zero;
        UpdateVisualFeedback(isEvading: false, isFeeding: false);
    }

    public void CollectByHunter()
    {
        if (_isCollected) return;

        _isCollected = true;
        SetVisualActive(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnDelay);
        transform.position = new Vector3(
            Random.Range(-15f, 15f),
            0f,
            Random.Range(-15f, 15f)
        );
        _currentHealth = _maxHealth;
        _isDead = false;
        _isCollected = false;
        InitializeRandomVelocity();
        SetVisualActive(true);
        UpdateVisualFeedback(isEvading: false, isFeeding: false);
    }

    private void SetVisualActive(bool active)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = active;
        }
    }

    private void UpdateVisualFeedback(bool isEvading, bool isFeeding)
    {
        if (_feedback == null)
        {
            _feedback = GetComponentInChildren<AgentFeedback>();
        }

        if (_feedback != null)
        {
            _feedback.UpdateBoidStateVisual(_isDead, isEvading, isFeeding);
        }
    }

    private void InitializeRandomVelocity()
    {
        Vector3 randomDir = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;
        _velocity = randomDir * _maxSpeed;
    }

    private Vector3 GetFlattenedPosition()
    {
        Vector3 pos = transform.position;
        pos.y = 0f;
        return pos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _separationRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _flockingRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _hunterDetectionRadius);
    }
}
