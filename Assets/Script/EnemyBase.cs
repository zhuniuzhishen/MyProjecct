using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人基类：NavMesh 追击、攻击节奏、血量与死亡、头顶血条（可选）、状态机（EnemyStatesFsm）。
/// 具体敌人 Enemy1/Enemy2 继承并配置数值、近战触发器或远程射击。
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected bool showWorldHealthBar = true;
    [SerializeField] protected Vector3 healthBarLocalOffset = new Vector3(0f, 1.85f, 0f);

    public Animator _anim; //动画器
    public NavMeshAgent _nav;
    public Transform targetPlayer;

    public float chaseSpeed = 3.5f;

    /// <summary>发现玩家的最远距离（子类可覆盖）</summary>
    public virtual float VisionRange => 6f;

    /// <summary>进入近战/停步攻击的距离阈值（子类可覆盖）</summary>
    public virtual float AttackRange => 2f;

    //攻击相关
    public float attackTimer = 0; //攻击计时器
    public float attackInterval = 5f; //攻击间隔, 两次攻击的间隔
    public float attackConsumerTime = 2.3f; //单次攻击的耗时
    public bool isAttacking = false; //是否正在攻击
    public bool canAttack = true; //是否能够攻击
    public float attackPower; //攻击力

    public float hurtStunTime = 0.35f;
    public float HurtStunTimer;

    //移动相关
    public float rotationSpeed = 3f; //转身速度
    public bool isMoving = false; //是否正在移动

    public float hp = 100f; //敌人血量
    public float maxHp = 100f; //敌人最大血量
    public bool isDead = false; //是否死亡

    public EnemyStateId CurrentStateId { get; private set; } = EnemyStateId.Idle;

    IEnemyState _state;
    readonly Dictionary<EnemyStateId, IEnemyState> _states = new Dictionary<EnemyStateId, IEnemyState>();

    /// <summary>若启用且子级没有血条组件，则动态创建一个挂 EnemyWorldHealthBar。</summary>
    protected virtual void Awake()
    {
        if (!showWorldHealthBar || GetComponentInChildren<EnemyWorldHealthBar>(true) != null)
            return;
        var hbGo = new GameObject("WorldHealthBar");
        hbGo.transform.SetParent(transform, false);
        hbGo.transform.localPosition = healthBarLocalOffset;
        hbGo.AddComponent<EnemyWorldHealthBar>();
    }

    /// <summary>锁定玩家引用并初始化状态机。</summary>
    protected virtual void Start()
    {
        if (Player.Instance != null)
            targetPlayer = Player.Instance.transform;

        InitFsm();
    }

    /// <summary>每帧推进攻击 CD、驱动当前状态；死亡后仍 Update 死亡状态（通常无逻辑）。</summary>
    protected virtual void Update()
    {
        if (isDead)
        {
            _state?.Update(this);
            return;
        }

        if (_nav != null)
            isMoving = _nav.speed > 0f;

        TickAttackTimers();
        _state?.Update(this);
    }

    /// <summary>注册 Idle/Chase/Attack/Hurt/Dead 五个状态并从 Idle 进入。</summary>
    protected void InitFsm()
    {
        _states[EnemyStateId.Idle] = EnemyIdleState.Instance;
        _states[EnemyStateId.Chase] = EnemyChaseState.Instance;
        _states[EnemyStateId.Attack] = EnemyAttackState.Instance;
        _states[EnemyStateId.Hurt] = EnemyHurtState.Instance;
        _states[EnemyStateId.Dead] = EnemyDeadState.Instance;

        _state = _states[EnemyStateId.Idle];
        CurrentStateId = EnemyStateId.Idle;
        _state.Enter(this);
    }

    /// <summary>切换敌人 AI 状态。</summary>
    public void ChangeState(EnemyStateId next)
    {
        if (_state == null)
            return;

        _state.Exit(this);
        _state = _states[next];
        CurrentStateId = next;
        _state.Enter(this);
    }

    /// <summary>
    /// 维护 attackTimer：isAttacking 为 true 时在 attackInterval-attackConsumerTime 后清除挥砍中标记；
    /// 非攻击且 canAttack 为 false 时递减 CD 直至可再次攻击。
    /// </summary>
    protected void TickAttackTimers()
    {
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= (attackInterval - attackConsumerTime))
                isAttacking = false;
        }

        if (!canAttack && !isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = 0f;
                canAttack = true;
            }
        }
    }

    /// <summary>玩家是否仍存在（未被销毁）。</summary>
    public bool HasValidTarget()
    {
        return targetPlayer != null;
    }

    public float DistanceToTarget()
    {
        if (!HasValidTarget())
            return float.PositiveInfinity;
        return Vector3.Distance(targetPlayer.position, transform.position);
    }

    /// <summary>子类可重写：近战挥砍或远程开火入口；默认只播攻击动画并进入攻击 CD。</summary>
    public virtual void PerformAttack()
    {
        Debug.Log("触发攻击");
        _anim.SetTrigger("Attack");
        canAttack = false;
        isAttacking = true;
        attackTimer = attackInterval;
    }

    /// <summary>离开 Attack 状态时调用，Enemy1 用于关闭攻击盒。</summary>
    public virtual void OnExitAttackState() { }

    /// <summary>扣血；血量为 0 调用 Dead；否则切入或刷新 Hurt 表现。</summary>
    public void Hurt(float damage)
    {
        if (isDead)
            return;

        hp = MathF.Max(0, hp - damage);

        if (hp <= 0)
        {
            Dead();
            return;
        }

        if (_state != null && CurrentStateId != EnemyStateId.Hurt)
            ChangeState(EnemyStateId.Hurt);
        else
            _anim.SetTrigger("Hurt");
    }

    /// <summary>禁用 AI、播放死亡动画、关闭碰撞、减少 Spawner 剩余敌人数并给玩家金币。</summary>
    public void Dead()
    {
        if (isDead)
            return;

        isDead = true;

        if (_anim != null)
        {
            _anim.ResetTrigger("Attack");
            _anim.ResetTrigger("Hurt");
            _anim.SetFloat("speed", 0f);
            _anim.SetBool("isDead", true);
        }

        if (_state != null && _states.Count > 0)
        {
            _state.Exit(this);
            _state = _states[EnemyStateId.Dead];
            CurrentStateId = EnemyStateId.Dead;
            _state.Enter(this);
        }

        DisableCollidersAsCorpse();

        Spawner.Instance.remainEnemies -= 1;
        Player.Instance.AddCoin(50);
    }

    /// <summary>死亡后关闭碰撞，玩家可穿过尸体</summary>
    protected void DisableCollidersAsCorpse()
    {
        foreach (var c in GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        var cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;
    }
}
