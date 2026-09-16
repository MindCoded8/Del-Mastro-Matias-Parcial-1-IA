using UnityEngine;

public class AutonomousAgent : Agent
{
    [Header("Arrival Settings")]
    [SerializeField] private float _slowingDistance = 4f;
    [SerializeField] private float _stopDistance = 0.2f;

    [Header("Target Reference (For Testing)")]
    [SerializeField] private Agent _targetAgent;

    public enum SteeringMode
    {
        Seek,
        Flee,
        Arrive,
        Evade
    }

    [Header("Steering Mode")]
    [SerializeField] private SteeringMode _currentMode = SteeringMode.Seek;

    protected override void Update()
    {
        if (_targetAgent == null) return;

        Vector3 steering = CalculateSteeringVector();
        _velocity += steering;

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.001f)
        {
            transform.forward = _velocity.normalized;
        }
    }

    private Vector3 CalculateSteeringVector()
    {
        switch (_currentMode)
        {
            case SteeringMode.Seek:
                return Seek(_targetAgent.transform.position);

            case SteeringMode.Flee:
                return Flee(_targetAgent.transform.position);

            case SteeringMode.Arrive:
                return Arrive(_targetAgent.transform.position);

            case SteeringMode.Evade:
                return Evade(_targetAgent);

            default:
                return Vector3.zero;
        }
    }

    private Vector3 ApplySteeringFormula(Vector3 desiredVelocity)
    {
        Vector3 steering = desiredVelocity - _velocity;

        return Vector3.ClampMagnitude(
            steering,
            _maxSteering * Time.deltaTime
        );
    }

    private Vector3 CalculateDesiredVelocity(Vector3 targetPosition)
    {
        Vector3 direction =
            (targetPosition - transform.position).normalized;

        return direction * _maxSpeed;
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        Vector3 desired = CalculateDesiredVelocity(targetPosition);

        return ApplySteeringFormula(desired);
    }

    public Vector3 Flee(Vector3 targetPosition)
    {
        Vector3 desired = CalculateDesiredVelocity(targetPosition);

        return ApplySteeringFormula(-desired);
    }

    public Vector3 Arrive(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        float sqrDistance = direction.sqrMagnitude;

        if (sqrDistance < _stopDistance * _stopDistance)
        {
            _velocity = Vector3.zero;

            return Vector3.zero;
        }

        float distance = Mathf.Sqrt(sqrDistance);

        float targetSpeed =
            _maxSpeed * (distance / _slowingDistance);

        float desiredSpeed =
            Mathf.Min(targetSpeed, _maxSpeed);

        Vector3 desired =
            direction.normalized * desiredSpeed;

        return ApplySteeringFormula(desired);
    }

    public Vector3 Evade(Agent target)
    {
        Vector3 futurePosition =
            PredictFuturePosition(target);

        return Flee(futurePosition);
    }

    private Vector3 PredictFuturePosition(Agent target)
    {
        Vector3 direction =
            target.transform.position - transform.position;

        float sqrDistance = direction.sqrMagnitude;

        float combinedSpeed =
            _maxSpeed + target.Velocity.magnitude;

        if (combinedSpeed <= 0.001f)
        {
            return target.transform.position;
        }

        float distance = Mathf.Sqrt(sqrDistance);

        float predictionTime =
            distance / combinedSpeed;

        return target.transform.position +
               (target.Velocity * predictionTime);
    }
}