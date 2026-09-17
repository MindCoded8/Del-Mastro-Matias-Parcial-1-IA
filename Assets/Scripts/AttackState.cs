using UnityEngine;

public class AttackState : State
{
    private HunterAgent _hunter;
    private float _attackDamage = 100f;

    public AttackState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Update()
    {
        BoidAgent target = _hunter.TargetBoid;

        if (target == null || !target.gameObject.activeInHierarchy || target.IsDead)
        {
            _hunter.TargetBoid = null;
            stateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        Vector3 targetPos = target.transform.position;
        float sqrDistance = (targetPos - _hunter.transform.position).sqrMagnitude;
        float sqrPerception = _hunter.PerceptionRadius * _hunter.PerceptionRadius;

        // Regla estricta: Si escapa del rango de visión, vuelve a Patrol SIN reiniciar TBA
        if (sqrDistance > sqrPerception)
        {
            _hunter.TargetBoid = null;
            stateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        float sqrMelee = _hunter.MeleeAttackRadius * _hunter.MeleeAttackRadius;
        float sqrRange = _hunter.RangeAttackRadius * _hunter.RangeAttackRadius;

        if (sqrDistance <= sqrMelee || sqrDistance <= sqrRange)
        {
            PerformAttack(target);
        }
        else
        {
            // Perseguir hasta alcanzar rango de ataque
            Vector3 steering = _hunter.Seek(targetPos);
            _hunter.Velocity += steering;
        }
    }

    private void PerformAttack(BoidAgent target)
    {
        target.TakeDamage(_attackDamage);

        // Reiniciar temporizador TBA y salir de Attack
        _hunter.TBATimer = 0f;
        _hunter.TargetBoid = null;
        stateMachine.ChangeState(HunterStates.Patrol);
    }
}
