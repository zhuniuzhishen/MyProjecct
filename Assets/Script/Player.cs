using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家角色：输入、血量金币、翻滚与三连击攻击由状态机（PlayerStatesFsm）驱动；
/// 物理移动在 FixedUpdate 由各状态的 FixedUpdate 设置 velocity；攻击判定盒通过动画事件 Open/CloseAttackTrigger。
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>全局单例，敌人与 UI 等通过 Player.Instance 访问。</summary>
    public static Player Instance;

    public Rigidbody _rb;
    public Animator _anim;

    // ---------- 移动与输入（每帧 ReadInput 更新） ----------
    public float moveSpeed = 5f;
    public float horizontalInput;
    public float verticalInput;
    public Vector3 inputDirection;

    public bool isMove;

    // ---------- 翻滚：rollingTime 为翻滚持续时间，rollCollingTime 为冷却 ----------
    public bool canRoll = true;
    public float rollingTime = 0.4f;
    public float rollCollingTimer;
    public float rollCollingTime = 1f;

    // ---------- 生命与资源 ----------
    public float hp = 100f;
    public float maxHp = 100f;

    public int coin = 200;

    public float attackPower = 30f;
    /// <summary>子物体 DamageCaster 上的盒体，仅在出刀窗口内 enabled=true。</summary>
    public BoxCollider _attackTriggerBox;

    public bool isDead;

    // ---------- 简易重力：关闭 Rigidbody.useGravity 后自写在 FixedUpdate ----------
    public bool isGrounded = true;
    public float gravity = -3.5f;

    // ---------- 三连击：每段“本段时长”，窗口内可接下一刀；attackMoveScale 为攻击中水平速度比例 ----------
    public float attackHitDuration1 = 0.52f;

 
    public float attackHitDuration2 = 0.52f;

   
    public float attackHitDuration3 = 0.58f;

    
    public float comboChainMinTime = 0.06f;

   
    public float comboChainWindowEnd = 0.9f;

   
    public float attackMoveScale = 0.28f;

    /// <summary>当前连击段 0=第一刀, 1=第二刀, 2=第三刀</summary>
    public int ComboIndex { get; private set; }

 
    public float hurtStunTime = 0.35f;

    public float hurtInvulnTime = 1f;

    public PlayerStateId CurrentStateId { get; private set; } = PlayerStateId.Idle;

    public bool isRolling => CurrentStateId == PlayerStateId.Roll;

    public bool isHurting => _invulnTimer > 0f;

    public bool HasMoveInput { get; private set; }

    public Vector3 WorldMoveDirection { get; private set; }

    public Vector3 RollWorldDirection { get; private set; }

    public float RollPhaseTimer;
    public float AttackPhaseTimer;
    public float HurtStunTimer;

    /// <summary>当前玩家逻辑状态（Idle/Move/Roll/Attack/Hurt/Dead）。</summary>
    IPlayerState _state;
    readonly Dictionary<PlayerStateId, IPlayerState> _states = new Dictionary<PlayerStateId, IPlayerState>();

    /// <summary>受伤后的无敌帧倒计时，大于 0 时 Hurt() 直接 return。</summary>
    float _invulnTimer;

    void Awake()
    {
        Instance = this;

        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();

        _rb.freezeRotation = true;
        _rb.useGravity = false;

        _states[PlayerStateId.Idle] = PlayerIdleState.Instance;
        _states[PlayerStateId.Move] = PlayerMoveState.Instance;
        _states[PlayerStateId.Roll] = PlayerRollState.Instance;
        _states[PlayerStateId.Attack] = PlayerAttackState.Instance;
        _states[PlayerStateId.Hurt] = PlayerHurtState.Instance;
        _states[PlayerStateId.Dead] = PlayerDeadState.Instance;

        _state = _states[PlayerStateId.Idle];
        CurrentStateId = PlayerStateId.Idle;
        _state.Enter(this);
    }

    /// <summary>缓存攻击用 DamageCaster 的 BoxCollider 引用。</summary>
    void Start()
    {
        _attackTriggerBox = transform.Find("DamageCaster").GetComponent<BoxCollider>();
    }

    /// <summary>读取输入、递减无敌与翻滚冷却、驱动状态机 Update，并在可走状态下面朝移动方向。</summary>
    void Update()
    {
        if (isDead)
            return;

        if (_state == null)
            return;

        ReadInput();

        _invulnTimer = Mathf.Max(0f, _invulnTimer - Time.deltaTime);

        if (!canRoll)
        {
            rollCollingTimer += Time.deltaTime;
            if (rollCollingTimer >= rollCollingTime)
            {
                rollCollingTimer = 0f;
                canRoll = true;
            }
        }

        _state.Update(this);

        if (CurrentStateId == PlayerStateId.Idle
            || CurrentStateId == PlayerStateId.Move
            || CurrentStateId == PlayerStateId.Attack)
        {
            if (HasMoveInput && WorldMoveDirection.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(WorldMoveDirection);
        }
    }

    /// <summary>非地面时简单下落；将物理步交给当前状态的 FixedUpdate（通常设置 _rb.velocity）。</summary>
    void FixedUpdate()
    {
        if (!isGrounded)
            transform.Translate(0f, gravity * Time.fixedDeltaTime, 0f);

        if (_state == null)
            return;

        if (isDead)
        {
            _state.FixedUpdate(this);
            return;
        }

        _state.FixedUpdate(this);
    }

    /// <summary>读取 WASD/方向键，斜向归一化；将输入旋转 -45° 得到场景中的世界移动方向（适配斜视角关卡）。</summary>
    void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        inputDirection.Set(horizontalInput, 0f, verticalInput);
        if (inputDirection.sqrMagnitude > 1f)
            inputDirection.Normalize();

        HasMoveInput = inputDirection.sqrMagnitude > 0.0001f;

        if (HasMoveInput)
        {
            var dir = Quaternion.Euler(0f, -45f, 0f) * inputDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                dir.Normalize();
            WorldMoveDirection = dir;
        }
        else
        {
            WorldMoveDirection = Vector3.zero;
        }

        isMove = HasMoveInput;
        _anim.SetBool("isMove", isMove);
    }

    /// <summary>退出旧状态 Enter 新状态，由 Player 与各 IPlayerState 协作调用。</summary>
    public void ChangeState(PlayerStateId next)
    {
        if (_state != null)
            _state.Exit(this);

        _state = _states[next];
        CurrentStateId = next;
        _state.Enter(this);
    }

    /// <summary>当前连击段对应的“本段攻击动作时长”上限，用于切回移动/待机。</summary>
    public float GetCurrentAttackHitDuration()
    {
        switch (ComboIndex)
        {
            case 0: return Mathf.Max(0.05f, attackHitDuration1);
            case 1: return Mathf.Max(0.05f, attackHitDuration2);
            default: return Mathf.Max(0.05f, attackHitDuration3);
        }
    }

    /// <summary>是否处于可接下一刀的时间窗内（由 comboChainMinTime 与 comboChainWindowEnd 控制）。</summary>
    public bool CanAcceptComboInput()
    {
        float d = GetCurrentAttackHitDuration();
        return AttackPhaseTimer >= comboChainMinTime
               && AttackPhaseTimer <= d * comboChainWindowEnd;
    }

    /// <summary>从第一段开始连招：重置段索引与相位计时，触发 Animator Attack。</summary>
    public void BeginAttackCombo()
    {
        ComboIndex = 0;
        AttackPhaseTimer = 0f;
        _anim.SetTrigger("Attack");
    }

    /// <summary>在窗口内接到下一刀输入时：段索引+1 并再次触发 Attack。</summary>
    public void AdvanceAttackCombo()
    {
        if (ComboIndex >= 2)
            return;
        ComboIndex++;
        AttackPhaseTimer = 0f;
        _anim.SetTrigger("Attack");
    }

    /// <summary>离开攻击状态时清空连招（由攻击状态 Exit 调用）。</summary>
    public void ResetAttackCombo()
    {
        ComboIndex = 0;
        AttackPhaseTimer = 0f;
    }

    /// <summary>本帧是否按下鼠标左键（攻击）。</summary>
    public bool WantsAttack()
    {
        return Input.GetMouseButtonDown(0);
    }

    /// <summary>本帧是否按下空格且翻滚不在冷却。</summary>
    public bool WantsRoll()
    {
        return Input.GetKeyDown(KeyCode.Space) && canRoll;
    }

    /// <summary>开始翻滚：锁冷却、记录翻滚世界方向（无输入则朝模型前方）。</summary>
    public void BeginRoll()
    {
        canRoll = false;
        RollPhaseTimer = 0f;

        if (HasMoveInput && WorldMoveDirection.sqrMagnitude > 0.0001f)
            RollWorldDirection = WorldMoveDirection;
        else
        {
            var f = transform.forward;
            RollWorldDirection = new Vector3(f.x, 0f, f.z);
            if (RollWorldDirection.sqrMagnitude < 0.0001f)
                RollWorldDirection = Vector3.forward;
            RollWorldDirection.Normalize();
        }

        _anim.SetTrigger("Roll");
    }

    /// <summary>受到伤害：无敌期跳过；扣血、刷新 UI；血量为 0 进死亡，否则进受击硬直状态。</summary>
    public void Hurt(float damage)
    {
        if (isDead)
            return;

        if (_invulnTimer > 0f)
            return;

        hp = MathF.Max(0f, hp - damage);
        GameManager.Instance.UpdateHealth();
        GameManager.Instance.PlayHealthHurtFlash();

        if (hp <= 0f)
        {
            Dead();
            return;
        }

        HurtStunTimer = hurtStunTime;
        _invulnTimer = hurtInvulnTime;
        ChangeState(PlayerStateId.Hurt);
    }

    /// <summary>进入死亡状态、触发死亡动画并通知 GameManager 游戏结束。</summary>
    public void Dead()
    {
        if (isDead)
            return;

        isDead = true;
        ChangeState(PlayerStateId.Dead);
        _anim.SetTrigger("Dead");
        GameManager.Instance.GameOver();
    }

    /// <summary>碰到带 Floor 标签的碰撞体视为着地。</summary>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Floor"))
            isGrounded = true;
    }

    /// <summary>离开地面碰撞体则视为悬空（用于自写重力）。</summary>
    void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Floor"))
            isGrounded = false;
    }

    /// <summary>持续贴地时保持 isGrounded，避免边缘一帧丢失。</summary>
    void OnCollisionStay(Collision collisionInfo)
    {
        if (collisionInfo.collider.CompareTag("Floor"))
            isGrounded = true;
    }

    /// <summary>动画事件：出刀帧打开攻击盒体。</summary>
    public void OpenAttackTrigger()
    {
        _attackTriggerBox.enabled = true;
        Debug.Log("打开碰撞器");
    }

    /// <summary>动画事件：收刀帧关闭攻击盒体。</summary>
    public void CloseAttackTrigger()
    {
        _attackTriggerBox.enabled = false;
        Debug.Log("关闭碰撞器");
    }

    /// <summary>拾取金币等：增加 coin 并刷新 HUD。</summary>
    public void AddCoin(int c)
    {
        coin += c;
        GameManager.Instance.UpdateCoin();
    }

    /// <summary>治疗：血量不超过 maxHp，并刷新血条。</summary>
    public void AddHealth(float h)
    {
        hp = MathF.Min(hp + h, maxHp);
        GameManager.Instance.UpdateHealth();
    }
}
