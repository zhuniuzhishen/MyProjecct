using UnityEngine;

/// <summary>
/// 远程子弹：沿 direction 匀速飞行，超时自毁；触发器碰到带 Player 标签的对象则造成伤害并销毁。
/// 由 Enemy2.Shoot 在枪口位置 Instantiate 后调用 Init。
/// </summary>
public class Bullet : MonoBehaviour
{
    public float damage;
    private float speed = 4f;
    public Vector3 direction;

    public float timeout = 5f;
    public float timer = 0;

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        timer += Time.deltaTime;

        // 防止子弹永远留在场景里占资源
        if (timer >= timeout)
            Destroy(gameObject);
    }

    /// <summary>设置子弹伤害数值。</summary>
    public void SetDamage(float d)
    {
        damage = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player.Instance.Hurt(damage);
            Destroy(gameObject);
        }
    }

    /// <summary>设置飞行方向、伤害，并把模型朝向飞行方向（再绕 X 轴 90° 适配你的子弹模型轴向）。</summary>
    public void Init(Vector3 d, float attackPower)
    {
        SetDamage(attackPower);
        direction = d;

        transform.rotation = Quaternion.LookRotation(direction);
        transform.Rotate(90f, 0, 0, Space.Self);
    }
}
