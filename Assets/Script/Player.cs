using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance;

    public Rigidbody _rb;
    public Animator _anim;

    public float moveSpeed = 5f;
    public float horizontalInput;
    public float verticalInput;
    public Vector3 inputDirection;

    public bool isMove;

    public bool canRoll = true;
    public float rollingTime = 0.4f;
    public float rollCollingTimer;
    public float rollCollingTime = 1f;

    public float hp = 100f;
    public float maxHp = 100f;

    public int coin = 200;

    public float attackPower = 30f;
    public BoxCollider _attackTriggerBox;

    public bool isDead;

    public bool isGrounded = true;
    public float gravity = -3.5f;

    [Header("FSM — Attack combo")]
    [Tooltip("第 1 段攻击时长（与 ATTACK_01 动画大致对齐）")]
    public float attackHitDuration1 = 0.52f;

    [Tooltip("第 2 段攻击时长（与 ATTACK_02 动画大致对齐）")]
    public float attackHitDuration2 = 0.52f;

    [Tooltip("第 3 段攻击时长（与 ATTACK_03 动画大致对齐）")]
    public float attackHitDuration3 = 0.58f;

    [Tooltip("当前段开始后至少经过这么久，才接受下一次点击连段（防止同一帧误吞）")]
    public float comboChainMinTime = 0.06f;

    [Range(0.5f, 1f)]
    [Tooltip("当前段时长的比例：超过后不再接受连段输入（避免动画末尾才触发下一段）")]
    public float comboChainWindowEnd = 0.9f;

    [Range(0f, 1f)]
    [Tooltip("攻击中水平位移 = moveSpeed * 该系数")]
    public float attackMoveScale = 0.28f;

    /// <summary>当前连击段 0=第一刀, 1=第二刀, 2=第三刀</summary>
    public int ComboIndex { get; private set; }

    [Header("FSM — Hurt")]
    [Tooltip("硬直期间无法移动、翻滚、攻击")]
    public float hurtStunTime = 0.35f;

    [Tooltip("无敌帧：期间再次受伤无效（可大于硬直）")]
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

    IPlayerState _state;
    readonly Dictionary<PlayerStateId, IPlayerState> _states = new Dictionary<PlayerStateId, IPlayerState>();

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

    void Start()
    {
        _attackTriggerBox = transform.Find("DamageCaster").GetComponent<BoxCollider>();
    }

    void Update()
    {
        if (isDead)
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

    void FixedUpdate()
    {
        if (!isGrounded)
            transform.Translate(0f, gravity * Time.fixedDeltaTime, 0f);

        if (isDead)
        {
            _state.FixedUpdate(this);
            return;
        }

        _state.FixedUpdate(this);
    }

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

    public void ChangeState(PlayerStateId next)
    {
        if (_state != null)
            _state.Exit(this);

        _state = _states[next];
        CurrentStateId = next;
        _state.Enter(this);
    }

    public float GetCurrentAttackHitDuration()
    {
        switch (ComboIndex)
        {
            case 0: return Mathf.Max(0.05f, attackHitDuration1);
            case 1: return Mathf.Max(0.05f, attackHitDuration2);
            default: return Mathf.Max(0.05f, attackHitDuration3);
        }
    }

    public bool CanAcceptComboInput()
    {
        float d = GetCurrentAttackHitDuration();
        return AttackPhaseTimer >= comboChainMinTime
               && AttackPhaseTimer <= d * comboChainWindowEnd;
    }

    public void BeginAttackCombo()
    {
        ComboIndex = 0;
        AttackPhaseTimer = 0f;
        _anim.SetTrigger("Attack");
    }

    public void AdvanceAttackCombo()
    {
        if (ComboIndex >= 2)
            return;
        ComboIndex++;
        AttackPhaseTimer = 0f;
        _anim.SetTrigger("Attack");
    }

    public void ResetAttackCombo()
    {
        ComboIndex = 0;
        AttackPhaseTimer = 0f;
    }

    public bool WantsAttack()
    {
        return Input.GetMouseButtonDown(0);
    }

    public bool WantsRoll()
    {
        return Input.GetKeyDown(KeyCode.Space) && canRoll;
    }

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

    public void Hurt(float damage)
    {
        if (isDead)
            return;

        if (_invulnTimer > 0f)
            return;

        hp = MathF.Max(0f, hp - damage);
        GameManager.Instance.UpdateHealth();

        if (hp <= 0f)
        {
            Dead();
            return;
        }

        HurtStunTimer = hurtStunTime;
        _invulnTimer = hurtInvulnTime;
        ChangeState(PlayerStateId.Hurt);
    }

    public void Dead()
    {
        if (isDead)
            return;

        isDead = true;
        ChangeState(PlayerStateId.Dead);
        _anim.SetTrigger("Dead");
        GameManager.Instance.GameOver();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Floor"))
            isGrounded = true;
    }

    void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Floor"))
            isGrounded = false;
    }

    void OnCollisionStay(Collision collisionInfo)
    {
        if (collisionInfo.collider.CompareTag("Floor"))
            isGrounded = true;
    }

    public void OpenAttackTrigger()
    {
        _attackTriggerBox.enabled = true;
        Debug.Log("打开碰撞器");
    }

    public void CloseAttackTrigger()
    {
        _attackTriggerBox.enabled = false;
        Debug.Log("关闭碰撞器");
    }

    public void AddCoin(int c)
    {
        coin += c;
        GameManager.Instance.UpdateCoin();
    }

    public void AddHealth(float h)
    {
        hp = MathF.Min(hp + h, maxHp);
        GameManager.Instance.UpdateHealth();
    }
}
