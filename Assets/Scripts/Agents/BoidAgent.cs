

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidAgent : Agent
{
    private static List<BoidAgent> _allBoids = new List<BoidAgent>();

    [Header("Life & Health Stats")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _respawnDelay = 3f;

    [Header("Perception & Movement Radii")]
    [SerializeField] private float _separationRadius = 1.5f;
    [SerializeField] private float _flockingRadius = 4.0f;
    [SerializeField] private float _hunterDetectionRadius = 6.0f;
    [SerializeField] private float _interestObjectDetectionRadius = 10.0f;
    [SerializeField] private float _interactDistance = 0.8f;
    [SerializeField] private float _slowingDistance = 3.0f;

    [Header("Flocking Weights")]
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 1.5f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 1.0f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 1.0f;

    [Header("Interaction Settings")]
    [SerializeField] private float _damageToInterestObjectPerSecond = 20f;

    private Agent _detectedHunter;
    private AgentFeedback _feedback;
    private bool _isDead = false;
    private bool _isCollected = false;

    public static IReadOnlyList<BoidAgent> AllBoids => _allBoids;
    public bool IsDead => _isDead;
    public bool IsCollected => _isCollected;

    protected virtual void Awake()
    {
        if (!_allBoids.Contains(this))
        {
            _allBoids.Add(this);
        }

        if (_feedback == null)
        {
            _feedback = GetComponentInChildren<AgentFeedback>();
        }

        _currentHealth = _maxHealth;
        InitializeRandomVelocity();
    }

    protected virtual void OnDestroy()
    {
        if (_allBoids.Contains(this))
        {
            _allBoids.Remove(this);
        }
    }

    protected override void Update()
    {
        if (_isDead || _isCollected)
        {
            _velocity = Vector3.zero;
            return;
        }

        ValidateHunterMemory();

        Vector3 steering = CalculateDecisionSteering();
        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);

        if (_velocity.sqrMagnitude > 0.001f)
        {
            transform.forward = _velocity.normalized;
        }

        if (Bounds.Instance != null)
        {
            transform.position = Bounds.Instance.OutOfBounds(transform.position);
        }
    }

    private void ValidateHunterMemory()
    {
        if (_detectedHunter != null)
        {
            float sqrDist = (_detectedHunter.transform.position - transform.position).sqrMagnitude;
            if (sqrDist > _hunterDetectionRadius * _hunterDetectionRadius)
            {
                _detectedHunter = null;
            }
        }
    }

    private Vector3 CalculateDecisionSteering()
    {
        bool isEvading = false;

        if (_detectedHunter != null && IsInRange(_detectedHunter.transform.position, _hunterDetectionRadius))
        {
            isEvading = true;
            UpdateVisualFeedback(isEvading);
            return Evade(_detectedHunter);
        }

        UpdateVisualFeedback(isEvading);

        InterestObject closestInterestObject = FindClosestInterestObject();

        if (closestInterestObject != null)
        {
            Vector3 targetPos = closestInterestObject.transform.position;
            targetPos.y = 0f;

            Vector3 boidPos = transform.position;
            boidPos.y = 0f;

            if ((targetPos - boidPos).sqrMagnitude <= _interactDistance * _interactDistance)
            {
                closestInterestObject.TakeDamage(_damageToInterestObjectPerSecond * Time.deltaTime);
            }

            return Arrive(targetPos);
        }

        return CalculateFlocking();
    }

    private InterestObject FindClosestInterestObject()
    {
        float sqrDetectionRadius = _interestObjectDetectionRadius * _interestObjectDetectionRadius;
        InterestObject closest = null;
        float minSqrDist = float.MaxValue;

        Vector3 currentPos = transform.position;
        currentPos.y = 0f;

        for (int i = 0; i < InterestObject.AllInterestObjects.Count; i++)
        {
            InterestObject obj = InterestObject.AllInterestObjects[i];

            if (obj != null && obj.gameObject.activeInHierarchy)
            {
                Vector3 objPos = obj.transform.position;
                objPos.y = 0f;

                float sqrDist = (objPos - currentPos).sqrMagnitude;

                if (sqrDist <= sqrDetectionRadius && sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = obj;
                }
            }
        }

        return closest;
    }

    private void UpdateVisualFeedback(bool isEvading)
    {
        if (_feedback != null)
        {
            _feedback.UpdateBoidStateVisual(_isDead, isEvading);
        }
    }

    public Vector3 CalculateFlocking()
    {
        Vector3 separation = CalculateSeparation(_separationRadius) * _separationWeight;
        Vector3 alignment = CalculateAlignment(_flockingRadius) * _alignmentWeight;
        Vector3 cohesion = CalculateCohesion(_flockingRadius) * _cohesionWeight;

        return separation + alignment + cohesion;
    }

    private Vector3 CalculateSeparation(float radius)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        for (int i = 0; i < _allBoids.Count; i++)
        {
            BoidAgent neighbor = _allBoids[i];

            if (neighbor == this || neighbor == null || neighbor.IsDead) continue;

            if (IsInRange(neighbor.transform.position, radius))
            {
                Vector3 pushVector = transform.position - neighbor.transform.position;
                pushVector.y = 0f;

                if (pushVector.sqrMagnitude < 0.0001f)
                {
                    pushVector = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                }

                desired += pushVector;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        desired /= count;
        Vector3 desiredVelocity = desired.normalized * _maxSpeed;

        return ApplySteeringFormula(desiredVelocity);
    }

    private Vector3 CalculateAlignment(float radius)
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;

        for (int i = 0; i < _allBoids.Count; i++)
        {
            BoidAgent neighbor = _allBoids[i];

            if (neighbor == this || neighbor == null || neighbor.IsDead) continue;

            if (IsInRange(neighbor.transform.position, radius))
            {
                Vector3 neighborVel = neighbor.Velocity;
                neighborVel.y = 0f;

                averageVelocity += neighborVel;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        averageVelocity /= count;
        Vector3 desiredVelocity = averageVelocity.normalized * _maxSpeed;

        return ApplySteeringFormula(desiredVelocity);
    }

    private Vector3 CalculateCohesion(float radius)
    {
        Vector3 centerOfMass = Vector3.zero;
        int count = 0;

        for (int i = 0; i < _allBoids.Count; i++)
        {
            BoidAgent neighbor = _allBoids[i];

            if (neighbor == this || neighbor == null || neighbor.IsDead) continue;

            if (IsInRange(neighbor.transform.position, radius))
            {
                Vector3 neighborPos = neighbor.transform.position;
                neighborPos.y = 0f;

                centerOfMass += neighborPos;
                count++;
            }
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

        Vector3 desired = direction.normalized * _maxSpeed;

        return ApplySteeringFormula(desired);
    }

    public Vector3 Flee(Vector3 targetPosition)
    {
        targetPosition.y = 0f;
        Vector3 direction = transform.position - targetPosition;
        direction.y = 0f;

        Vector3 desired = direction.normalized * _maxSpeed;

        return ApplySteeringFormula(desired);
    }

    public Vector3 Arrive(Vector3 targetPosition)
    {
        targetPosition.y = 0f;
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        float sqrDistance = direction.sqrMagnitude;
        float stopRadius = 0.2f;

        if (sqrDistance < stopRadius * stopRadius)
        {
            _velocity = Vector3.zero;
            return Vector3.zero;
        }

        float safeSlowingDistance = Mathf.Max(_slowingDistance, 0.1f);
        float distance = Mathf.Sqrt(sqrDistance);
        float targetSpeed = _maxSpeed * (distance / safeSlowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        Vector3 desired = direction.normalized * desiredSpeed;

        return ApplySteeringFormula(desired);
    }

    public Vector3 Evade(Agent target)
    {
        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;

        float combinedSpeed = _maxSpeed + target.Velocity.magnitude;

        if (combinedSpeed <= 0.001f) return Flee(target.transform.position);

        float distance = Mathf.Sqrt(direction.sqrMagnitude);
        float predictionTime = distance / combinedSpeed;

        Vector3 futurePosition = target.transform.position + (target.Velocity * predictionTime);
        futurePosition.y = 0f;

        return Flee(futurePosition);
    }

    public void RegisterHunter(Agent hunter)
    {
        _detectedHunter = hunter;
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
        UpdateVisualFeedback(false);
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

        transform.position = new Vector3(Random.Range(-15f, 15f), 0f, Random.Range(-15f, 15f));
        _currentHealth = _maxHealth;
        _isDead = false;
        _isCollected = false;

        InitializeRandomVelocity();
        SetVisualActive(true);
        UpdateVisualFeedback(false);
    }

    private void SetVisualActive(bool active)
    {
        Transform visual = transform.Find("Visual");

        if (visual != null)
        {
            visual.gameObject.SetActive(active);
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = active;
            }
        }
    }

    private void InitializeRandomVelocity()
    {
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        _velocity = randomDir * _maxSpeed;
    }

    private bool IsInRange(Vector3 targetPosition, float radius)
    {
        targetPosition.y = 0f;
        Vector3 currentPos = transform.position;
        currentPos.y = 0f;

        return (targetPosition - currentPos).sqrMagnitude <= radius * radius;
    }
}
