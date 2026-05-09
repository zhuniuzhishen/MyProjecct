using UnityEngine;

public enum PlayerStateId
{
    Idle,
    Move,
    Roll,
    Attack,
    Hurt,
    Dead
}

public interface IPlayerState
{
    PlayerStateId Id { get; }
    void Enter(Player p);
    void Update(Player p);
    void FixedUpdate(Player p);
    void Exit(Player p);
}

public sealed class PlayerIdleState : IPlayerState
{
    public static readonly PlayerIdleState Instance = new PlayerIdleState();
    public PlayerStateId Id => PlayerStateId.Idle;
    public void Enter(Player p) { }
    public void Exit(Player p) { }

    public void Update(Player p)
    {
        if (p.WantsAttack())
        {
            p.BeginAttackCombo();
            p.ChangeState(PlayerStateId.Attack);
            return;
        }

        if (p.WantsRoll())
        {
            p.BeginRoll();
            p.ChangeState(PlayerStateId.Roll);
            return;
        }

        if (p.HasMoveInput)
            p.ChangeState(PlayerStateId.Move);
    }

    public void FixedUpdate(Player p)
    {
        var v = p._rb.velocity;
        p._rb.velocity = new Vector3(0f, v.y, 0f);
    }
}

public sealed class PlayerMoveState : IPlayerState
{
    public static readonly PlayerMoveState Instance = new PlayerMoveState();
    public PlayerStateId Id => PlayerStateId.Move;
    public void Enter(Player p) { }
    public void Exit(Player p) { }

    public void Update(Player p)
    {
        if (p.WantsAttack())
        {
            p.BeginAttackCombo();
            p.ChangeState(PlayerStateId.Attack);
            return;
        }

        if (p.WantsRoll())
        {
            p.BeginRoll();
            p.ChangeState(PlayerStateId.Roll);
            return;
        }

        if (!p.HasMoveInput)
            p.ChangeState(PlayerStateId.Idle);
    }

    public void FixedUpdate(Player p)
    {
        p._rb.velocity = p.WorldMoveDirection * p.moveSpeed;
    }
}

public sealed class PlayerRollState : IPlayerState
{
    public static readonly PlayerRollState Instance = new PlayerRollState();
    public PlayerStateId Id => PlayerStateId.Roll;
    public void Enter(Player p) { }
    public void Exit(Player p) { }

    public void Update(Player p)
    {
        p.RollPhaseTimer += Time.deltaTime;
        if (p.RollPhaseTimer >= p.rollingTime)
        {
            p.RollPhaseTimer = 0f;
            p.ChangeState(p.HasMoveInput ? PlayerStateId.Move : PlayerStateId.Idle);
        }
    }

    public void FixedUpdate(Player p)
    {
        p._rb.velocity = p.RollWorldDirection * (p.moveSpeed * 2.5f);
    }
}

public sealed class PlayerAttackState : IPlayerState
{
    public static readonly PlayerAttackState Instance = new PlayerAttackState();
    public PlayerStateId Id => PlayerStateId.Attack;
    public void Enter(Player p)
    {
        // 第一段由 Idle/Move 里 BeginAttackCombo() 已 SetTrigger("Attack")
    }

    public void Exit(Player p)
    {
        if (p._attackTriggerBox != null)
            p._attackTriggerBox.enabled = false;
        p.ResetAttackCombo();
    }

    public void Update(Player p)
    {
        if (p.WantsAttack() && p.ComboIndex < 2 && p.CanAcceptComboInput())
            p.AdvanceAttackCombo();

        p.AttackPhaseTimer += Time.deltaTime;
        if (p.AttackPhaseTimer >= p.GetCurrentAttackHitDuration())
        {
            p.ChangeState(p.HasMoveInput ? PlayerStateId.Move : PlayerStateId.Idle);
        }
    }

    public void FixedUpdate(Player p)
    {
        p._rb.velocity = p.WorldMoveDirection * (p.moveSpeed * p.attackMoveScale);
    }
}

public sealed class PlayerHurtState : IPlayerState
{
    public static readonly PlayerHurtState Instance = new PlayerHurtState();
    public PlayerStateId Id => PlayerStateId.Hurt;
    public void Enter(Player p)
    {
        p._anim.SetTrigger("Hurt");
    }

    public void Exit(Player p) { }

    public void Update(Player p)
    {
        p.HurtStunTimer -= Time.deltaTime;
        if (p.HurtStunTimer <= 0f)
        {
            p.HurtStunTimer = 0f;
            p.ChangeState(p.HasMoveInput ? PlayerStateId.Move : PlayerStateId.Idle);
        }
    }

    public void FixedUpdate(Player p)
    {
        var v = p._rb.velocity;
        p._rb.velocity = new Vector3(0f, v.y, 0f);
    }
}

public sealed class PlayerDeadState : IPlayerState
{
    public static readonly PlayerDeadState Instance = new PlayerDeadState();
    public PlayerStateId Id => PlayerStateId.Dead;
    public void Enter(Player p)
    {
        p._rb.velocity = Vector3.zero;
    }

    public void Exit(Player p) { }

    public void Update(Player p) { }

    public void FixedUpdate(Player p)
    {
        p._rb.velocity = Vector3.zero;
    }
}
