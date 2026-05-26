using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 敌人脚步与攻击表现：由 Animator 动画事件调用 FootStepVFX / AttackVFX，与 AI 逻辑解耦。
/// </summary>
public class EnemyVFXManager : MonoBehaviour
{
    public VisualEffect footStep;
    public VisualEffect attackVFX;

    public EnemyBase enemyBase;

    private void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    /// <summary>动画事件：脚底落地时播放脚步。</summary>
    public void FootStepVFX()
    {
        footStep.Play();
    }

    /// <summary>动画事件：挥击或开火瞬间播放攻击特效。</summary>
    public void AttackVFX()
    {
        attackVFX.Play();
    }
}
