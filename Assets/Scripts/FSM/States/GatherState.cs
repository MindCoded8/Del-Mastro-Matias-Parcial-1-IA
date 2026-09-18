

using UnityEngine;

public class GatherState : State
{
    private HunterAgent _hunter;
    private float _gatherDuration = 2f;
    private float _gatherTimer = 0f;

    public GatherState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _gatherTimer = 0f;
        _hunter.UpdateHeadFeedback("GATHER", Color.blue);
    }

    public override void Update()
    {
        BoidAgent target = _hunter.TargetBoid;

        if (target == null || !target.IsDead || target.IsCollected || !target.gameObject.activeInHierarchy)
        {
            _hunter.TargetBoid = null;
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        Vector3 targetPos = target.transform.position;
        targetPos.y = 0f;

        Vector3 hunterPos = _hunter.transform.position;
        hunterPos.y = 0f;

        float sqrDistance = (targetPos - hunterPos).sqrMagnitude;
        float interactDist = 0.8f;

        if (sqrDistance > interactDist * interactDist)
        {
            Vector3 steering = _hunter.Arrive(targetPos);
            _hunter.Velocity += steering;
        }
        else
        {
            _hunter.StopMovement();
            _gatherTimer += Time.deltaTime;

            if (_gatherTimer >= _gatherDuration)
            {
                target.CollectByHunter();
                _hunter.TargetBoid = null;
                StateMachine.ChangeState(HunterStates.Patrol);
            }
        }
    }
}