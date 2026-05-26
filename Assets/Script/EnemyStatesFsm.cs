using UnityEngine;

// =============================================================================
// 敌人状态机：根据与玩家的距离在 Idle / Chase / Attack 间切换，受伤进 Hurt，死亡进 Dead。
// =============================================================================

/// <summary>敌人 AI 状态枚举。</summary>
public enum EnemyStateId
{
    Idle,
    Chase,
    Attack,
    Hurt,
    Dead
}

/// <summary>单个敌人状态接口。</summary>
public interface IEnemyState
{
    EnemyStateId Id { get; }
    void Enter(EnemyBase e);
    void Update(EnemyBase e);
    void Exit(EnemyBase e);
}

/// <summary>待机：速度为 0；玩家在视野内则开始追击。</summary>
public sealed class EnemyIdleState : IEnemyState
{
    public static readonly EnemyIdleState Instance = new EnemyIdleState();
    public EnemyStateId Id => EnemyStateId.Idle;

    public void Enter(EnemyBase e)
    {
        if (e._nav != null)
        {
            e._nav.speed = 0f;
            e._anim.SetFloat("speed", 0f);
        }
    }

    public void Exit(EnemyBase e) { }

    public void Update(EnemyBase e)
    {
        if (!e.HasValidTarget())
            return;

        float d = e.DistanceToTarget();
        if (d <= e.VisionRange)
            e.ChangeState(EnemyStateId.Chase);
    }
}

/// <summary>追击：NavMesh 设目标为玩家；太近则切攻击，太远出视野则回待机。</summary>
public sealed class EnemyChaseState : IEnemyState
{
    public static readonly EnemyChaseState Instance = new EnemyChaseState();
    public EnemyStateId Id => EnemyStateId.Chase;

    public void Enter(EnemyBase e) { }

    public void Exit(EnemyBase e) { }

    public void Update(EnemyBase e)
    {
        if (!e.HasValidTarget())
        {
            e.ChangeState(EnemyStateId.Idle);
            return;
        }

        float d = e.DistanceToTarget();

        if (d > e.VisionRange)
        {
            e.ChangeState(EnemyStateId.Idle);
            return;
        }

        if (d < e.AttackRange)
        {
            e.ChangeState(EnemyStateId.Attack);
            return;
        }

        if (e._nav == null)
            return;

        e._nav.speed = e.chaseSpeed;
        e._nav.SetDestination(e.targetPlayer.position);
        e._anim.SetFloat("speed", e._nav.speed);
    }
}

/// <summary>攻击：站定转向玩家；不在挥砍中且 CD 结束则 PerformAttack；目标走远则切回追击。</summary>
public sealed class EnemyAttackState : IEnemyState
{
    public static readonly EnemyAttackState Instance = new EnemyAttackState();
    public EnemyStateId Id => EnemyStateId.Attack;

    public void Enter(EnemyBase e) { }

    public void Exit(EnemyBase e)
    {
        e.OnExitAttackState();
    }

    public void Update(EnemyBase e)
    {
        if (!e.HasValidTarget())
        {
            e.ChangeState(EnemyStateId.Idle);
            return;
        }

        float d = e.DistanceToTarget();

        if (d > e.VisionRange)
        {
            e.ChangeState(EnemyStateId.Idle);
            return;
        }

        if (d >= e.AttackRange)
        {
            e.ChangeState(EnemyStateId.Chase);
            return;
        }

        if (e.isAttacking)
            return;

        if (e._nav != null)
        {
            e._nav.SetDestination(e.targetPlayer.position);
            e._nav.speed = 0f;
            e._anim.SetFloat("speed", 0f);
        }

        Vector3 direction = e.targetPlayer.position - e.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            e.transform.rotation = Quaternion.Slerp(
                e.transform.rotation,
                targetRotation,
                Time.deltaTime * e.rotationSpeed);
        }

        if (e.canAttack)
            e.PerformAttack();
    }
}

/// <summary>受击：硬直计时结束后按距离回到 Idle/Chase/Attack 之一。</summary>
public sealed class EnemyHurtState : IEnemyState
{
    public static readonly EnemyHurtState Instance = new EnemyHurtState();
    public EnemyStateId Id => EnemyStateId.Hurt;

    public void Enter(EnemyBase e)
    {
        e.HurtStunTimer = e.hurtStunTime;
        e._anim.SetTrigger("Hurt");
    }

    public void Exit(EnemyBase e) { }

    public void Update(EnemyBase e)
    {
        e.HurtStunTimer -= Time.deltaTime;
        if (e.HurtStunTimer > 0f)
            return;

        e.HurtStunTimer = 0f;

        if (!e.HasValidTarget())
        {
            e.ChangeState(EnemyStateId.Idle);
            return;
        }

        float d = e.DistanceToTarget();
        if (d > e.VisionRange)
            e.ChangeState(EnemyStateId.Idle);
        else if (d < e.AttackRange)
            e.ChangeState(EnemyStateId.Attack);
        else
            e.ChangeState(EnemyStateId.Chase);
    }
}

/// <summary>死亡：关闭 NavMeshAgent，不再 Update 行为。</summary>
public sealed class EnemyDeadState : IEnemyState
{
    public static readonly EnemyDeadState Instance = new EnemyDeadState();
    public EnemyStateId Id => EnemyStateId.Dead;

    public void Enter(EnemyBase e)
    {
        if (e._nav != null)
            e._nav.enabled = false;
    }

    public void Exit(EnemyBase e) { }

    public void Update(EnemyBase e) { }
}
