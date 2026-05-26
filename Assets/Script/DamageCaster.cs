using UnityEngine;

/// <summary>攻击方阵营：玩家挥刀判敌用 Player，敌人近战判玩家用 Enemy。</summary>
public enum CampType
{
    Player,
    Enemy
}

/// <summary>
/// 挂在攻击判定子物体上的触发盒：默认关闭，由动画事件在“出刀帧”打开、“收刀帧”关闭。
/// OnTriggerEnter 根据 CampType 对敌人（EnemyBase）或玩家（Tag Player）调用 Hurt。
/// </summary>
public class DamageCaster : MonoBehaviour
{
    public BoxCollider _box;
    /// <summary>敌人类物体上才有；玩家攻击时此项为空，走 Player 攻击力。</summary>
    public EnemyBase enemyBase;
    /// <summary>在 Inspector 中设为 Player 或 Enemy，决定伤害流向。</summary>
    public CampType currentCamp;

    private void Awake()
    {
        _box = GetComponent<BoxCollider>();
        // 开局关闭，避免静止时也碰到人
        _box.enabled = false;

        // 父物体应为敌人根；玩家身上无 EnemyBase 则 enemyBase 为 null
        enemyBase = transform.parent.GetComponent<EnemyBase>();
    }

    /// <summary>触发器进入：玩家阵营打父级带 EnemyBase 的碰撞体；敌人阵营打 Tag 为 Player 的碰撞体。</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (currentCamp == CampType.Player)
        {
            // 用 EnemyBase 判定，避免根物体漏打 Enemy 标签时无法受伤
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                Debug.Log("检测到敌人");
                enemy.Hurt(Player.Instance.attackPower);
            }
        }
        else if (currentCamp == CampType.Enemy)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("检测到玩家");
                Player.Instance.Hurt(enemyBase.attackPower);
            }
        }
    }
}
