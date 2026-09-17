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

    [Header("Perception Radii")]
    [SerializeField] private float _separationRadius = 1.5f;
    [SerializeField] private float _flockingRadius = 4.0f;
    [SerializeField] private float _hunterDetectionRadius = 6.0f;
    [SerializeField] private float _interestObjectDetectionRadius = 8.0f;
    [SerializeField] private float _interactDistance = 8.5f;

    [Header("Flocking Weights")]
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 1.5f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 1.0f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 1.0f;

    [Header("Interaction Settings")]
    [SerializeField] private float _damageToInterestObjectPerSecond = 20f;

    private Agent _detectedHunter;
    private InterestObject _targetInterestObject;
    private bool isDead = false;
    private bool isCollected = false;

    public static IReadOnlyList<BoidAgent> AllBoids => _allBoids;
    public bool IsDead => isDead;
    public bool IsCollected => isCollected;

    protected virtual void Awake()
    {
        if (!_allBoids.Contains(this))
        {
            _allBoids.Add(this);
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
        if (isDead || isCollected)
        {
            _velocity = Vector3.zero;
            return;
        }

        Vector3 steering = CalculateDecisionSteering();

        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.001f)
        {
            transform.forward = _velocity.normalized;
        }
    }

    private Vector3 CalculateDecisionSteering()
    {
        if (_detectedHunter != null && IsInRange(_detectedHunter.transform.position, _hunterDetectionRadius))
        {
            return Evade(_detectedHunter);
        }

        if (_targetInterestObject != null && _targetInterestObject.gameObject.activeInHierarchy)
        {
            Vector3 targetPos = _targetInterestObject.transform.position;
            if (IsInRange(targetPos, _interestObjectDetectionRadius))
            {
                if ((targetPos - transform.position).sqrMagnitude <= _interactDistance * _interactDistance)
                {
                    _targetInterestObject.TakeDamage(_damageToInterestObjectPerSecond * Time.deltaTime);
                }
                return Arrive(targetPos);
            }
        }

        return CalculateFlocking();
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
                desired += (transform.position - neighbor.transform.position);
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
                averageVelocity += neighbor.Velocity;
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
                centerOfMass += neighbor.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        centerOfMass /= count;
        return Seek(centerOfMass);
    }

    private Vector3 ApplySteeringFormula(Vector3 desiredVelocity)
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

    public Vector3 Flee(Vector3 targetPosition)
    {
        Vector3 direction = (transform.position - targetPosition).normalized;
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

    public Vector3 Evade(Agent target)
    {
        Vector3 direction = target.transform.position - transform.position;
        float sqrDistance = direction.sqrMagnitude;
        float combinedSpeed = _maxSpeed + target.Velocity.magnitude;

        if (combinedSpeed <= 0.001f) return Flee(target.transform.position);

        float distance = Mathf.Sqrt(sqrDistance);
        float predictionTime = distance / combinedSpeed;
        Vector3 futurePosition = target.transform.position + (target.Velocity * predictionTime);

        return Flee(futurePosition);
    }

    public void RegisterHunter(Agent hunter)
    {
        _detectedHunter = hunter;
    }

    public void RegisterInterestObject(InterestObject interestObject)
    {
        _targetInterestObject = interestObject;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        _currentHealth -= amount;

        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        _velocity = Vector3.zero;
    }

    public void CollectByHunter()
    {
        if (isCollected) return;

        isCollected = true;
        gameObject.SetActive(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnDelay);

        transform.position = new Vector3(Random.Range(-15f, 15f), 0f, Random.Range(-15f, 15f));
        _currentHealth = _maxHealth;
        isDead = false;
        isCollected = false;

        InitializeRandomVelocity();
        gameObject.SetActive(true);
    }

    private void InitializeRandomVelocity()
    {
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        _velocity = randomDir * _maxSpeed;
    }

    private bool IsInRange(Vector3 targetPosition, float radius)
    {
        return (targetPosition - transform.position).sqrMagnitude <= radius * radius;
    }
}