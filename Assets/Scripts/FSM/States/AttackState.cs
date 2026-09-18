

using UnityEngine;

public class AttackState : State
{
    private HunterAgent _hunter;
    private float _meleeDamage = 100f;
    private float _rangeDamage = 50f;

    public AttackState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _hunter.UpdateHeadFeedback("ATTACK!", Color.red);
    }

    public override void Update()
    {
        BoidAgent target = _hunter.TargetBoid;

        if (target == null || target.IsDead || target.IsCollected || !target.gameObject.activeInHierarchy)
        {
            _hunter.TargetBoid = null;
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        Vector3 hunterPos = _hunter.transform.position;
        hunterPos.y = 0f;

        Vector3 targetPos = target.transform.position;
        targetPos.y = 0f;

        float sqrDistance = (targetPos - hunterPos).sqrMagnitude;
        float sqrPerception = _hunter.PerceptionRadius * _hunter.PerceptionRadius;

        if (sqrDistance > sqrPerception)
        {
            _hunter.TargetBoid = null;
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        float sqrMelee = _hunter.MeleeAttackRadius * _hunter.MeleeAttackRadius;
        float sqrRange = _hunter.RangeAttackRadius * _hunter.RangeAttackRadius;

        if (sqrDistance <= sqrMelee)
        {
            PerformMeleeAttack(target);
        }
        else if (sqrDistance <= sqrRange)
        {
            PerformRangeAttack(target);
        }
        else
        {
            Vector3 steering = _hunter.Seek(targetPos);
            _hunter.Velocity += steering;
        }
    }

    private void PerformMeleeAttack(BoidAgent target)
    {
        target.TakeDamage(_meleeDamage);
        OnAttackSuccessful();
    }

    private void PerformRangeAttack(BoidAgent target)
    {
        target.TakeDamage(_rangeDamage);
        OnAttackSuccessful();
    }

    private void OnAttackSuccessful()
    {
        _hunter.ResetTBATimer();
        _hunter.TargetBoid = null;
        StateMachine.ChangeState(HunterStates.Patrol);
    }
}