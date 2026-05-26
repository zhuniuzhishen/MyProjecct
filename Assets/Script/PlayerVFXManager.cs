using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 玩家视觉特效：根据是否在移动开关脚步 VFX；三段挥砍由动画事件分别调用 PlayAttackVFX01~03。
/// heal 字段预留，可在拾血动画事件中调用 Play。
/// </summary>
public class PlayerVFXManager : MonoBehaviour
{
    public VisualEffect footStep;
    public VisualEffect heal;
    public ParticleSystem blade01;
    public ParticleSystem blade02;
    public ParticleSystem blade03;

    /// <summary>与 Player.isMove 同步，用于检测“移动状态是否变化”以开关脚步。</summary>
    public bool isPlayingFoot = false;

    void Update()
    {
        if (Player.Instance == null || footStep == null)
            return;

        // 异或：移动状态与特效开关不一致时才切换，避免每帧重复 Play/Stop
        if (isPlayingFoot ^ Player.Instance.isMove)
        {
            Update_FootStp(Player.Instance.isMove);
            isPlayingFoot = !isPlayingFoot;
        }
    }

    private void Update_FootStp(bool state)
    {
        if (footStep == null)
            return;

        if (state)
            footStep.Play();
        else
            footStep.Stop();
    }

    /// <summary>动画事件：第一刀挥砍粒子。</summary>
    public void PlayAttackVFX01()
    {
        if (blade01 != null)
            blade01.Play();
    }

    /// <summary>动画事件：第二刀挥砍粒子。</summary>
    public void PlayAttackVFX02()
    {
        if (blade02 != null)
            blade02.Play();
    }

    /// <summary>动画事件：第三刀挥砍粒子。</summary>
    public void PlayAttackVFX03()
    {
        if (blade03 != null)
            blade03.Play();
    }
}
