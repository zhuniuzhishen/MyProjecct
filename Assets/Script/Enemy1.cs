using UnityEngine;
using UnityEngine.AI;

/// <summary>近战敌人：视野与攻击距离较短，用子物体 DamageCaster 盒体在动画帧内对玩家造成伤害。</summary>
public class Enemy1 : EnemyBase
{
    public BoxCollider _attackTriggerBox;

    public override float VisionRange => 6f;
    public override float AttackRange => 2f;

    /// <summary>取 NavMeshAgent、Animator；并调用基类生成头顶血条等逻辑。</summary>
    protected override void Awake()
    {
        base.Awake();
        _nav = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
    }

    /// <summary>配置近战攻击力与 DamageCaster 引用。</summary>
    protected override void Start()
    {
        attackPower = 40f;
        _attackTriggerBox = transform.Find("DamageCaster")
            .GetComponent<BoxCollider>();
        base.Start();
    }

    /// <summary>退出攻击状态时确保关闭近战判定盒，避免一直造成伤害。</summary>
    public override void OnExitAttackState()
    {
        if (_attackTriggerBox != null)
            _attackTriggerBox.enabled = false;
    }

    /// <summary>动画事件：挥到判定帧时打开攻击盒。</summary>
    public void OpenAttackTrigger()
    {
        _attackTriggerBox.enabled = true;
        Debug.Log("打开碰撞器");
    }

    /// <summary>动画事件：收招时关闭攻击盒。</summary>
    public void CloseAttackTrigger()
    {
        _attackTriggerBox.enabled = false;
        Debug.Log("关闭碰撞器");
    }
}
