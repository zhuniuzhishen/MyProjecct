using UnityEngine;

// =============================================================================
// 玩家状态机（FSM）：同一时间只有一个 IPlayerState 生效。
// Player 在 Awake 里注册各状态单例，ChangeState 时 Exit 旧状态再 Enter 新状态。
// =============================================================================

/// <summary>玩家逻辑状态枚举。</summary>
public enum PlayerStateId
{
    Idle,
    Move,
    Roll,
    Attack,
    Hurt,
    Dead
}

/// <summary>单个玩家状态需实现的接口：进入/每帧/物理帧/离开。</summary>
public interface IPlayerState
{
    PlayerStateId Id { get; }
    void Enter(Player p);
    void Update(Player p);
    void FixedUpdate(Player p);
    void Exit(Player p);
}

/// <summary>待机：检测攻击、翻滚、移动输入以切换到对应状态；水平速度清零保留竖直速度。</summary>
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

/// <summary>移动：同样优先响应攻击与翻滚；无输入回待机；FixedUpdate 里按 WorldMoveDirection 设速度。</summary>
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

/// <summary>翻滚：计时 rollingTime 结束后根据是否有移动输入回到 Move 或 Idle；速度为翻滚方向 × 更高倍率。</summary>
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

/// <summary>
/// 攻击：在窗口内可 AdvanceAttackCombo 接第 2、3 段；AttackPhaseTimer 超过当前段时长则回 Move/Idle；
/// Exit 时关闭攻击盒并重置连招；FixedUpdate 中保留少量水平位移（attackMoveScale）。
/// </summary>
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

/// <summary>受击硬直：播放 Hurt 动画，HurtStunTimer 结束后根据输入回 Move 或 Idle。</summary>
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

/// <summary>死亡：清空速度，不再响应输入（Player.Update 已因 isDead return）。</summary>
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
