using UnityEngine;

/// <summary>
/// 回血拾取物：玩家碰撞体进入触发器时给 Player 加血（不超过 maxHp）。
/// 需在玩家碰撞体上打 Player 标签；本物体 Collider 勾选 Is Trigger。
/// </summary>
public class Medical : MonoBehaviour
{
    [Tooltip("一次拾取回复的生命值")]
    public float med = 50f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            Player.Instance.AddHealth(med);
    }
}
