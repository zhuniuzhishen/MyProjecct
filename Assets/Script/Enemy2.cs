using UnityEngine;
using UnityEngine.AI;

/// <summary>远程敌人：视野与攻击距离更大；攻击动画事件中 Shoot 生成子弹飞向玩家。</summary>
public class Enemy2 : EnemyBase
{
    public GameObject bullet;
    public Transform _shootPos;

    public override float VisionRange => 8f;
    public override float AttackRange => 6f;

    /// <summary>加载子弹预制体与枪口 Transform。</summary>
    protected override void Awake()
    {
        base.Awake();
        _nav = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();

        bullet = Resources.Load<GameObject>("Prefab/Bullet");
        _shootPos = transform.Find("ShootPos");
    }

    /// <summary>远程敌人较高单发伤害。</summary>
    protected override void Start()
    {
        attackPower = 80f;
        base.Start();
    }

    /// <summary>在枪口位置实例化子弹并传入朝向与攻击力。</summary>
    public void Shoot()
    {
        if (targetPlayer == null || bullet == null || _shootPos == null)
            return;

        Vector3 direction = (targetPlayer.position - transform.position).normalized;

        GameObject go = Instantiate(bullet, _shootPos.position, Quaternion.identity);
        go.GetComponent<Bullet>().Init(direction, attackPower);
    }
}
