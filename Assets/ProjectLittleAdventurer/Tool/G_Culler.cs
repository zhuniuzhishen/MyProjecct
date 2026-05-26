using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 单个可剔除对象：Awake 根据身上组件判断类型（网格或粒子），并计算用于 CullingGroup 的包围球中心。
/// <see cref="G_CullManager"/> 在可见性变化时调用 <see cref="Cull"/> 开关渲染与播放。
/// </summary>
public class G_Culler : MonoBehaviour
{
    MeshRenderer _renderer;
    ParticleSystem _particleSystem;
    VisualEffect _visualEffect;

    /// <summary>供 CullingGroup 使用的世界空间中心点。</summary>
    public Vector3 Center;

    public enum CullerType
    {
        MeshRenderer,
        ParticleSystem
    }

    /// <summary>粒子类型分支内也会处理 VFX Graph 组件。</summary>
    public CullerType Type;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _particleSystem = GetComponent<ParticleSystem>();
        _visualEffect = GetComponent<VisualEffect>();

        if (_renderer != null)
        {
            Center = _renderer.bounds.center;
            Type = CullerType.MeshRenderer;
        }

        if (_particleSystem != null || _visualEffect != null)
        {
            Center = transform.position;
            Type = CullerType.ParticleSystem;
        }
    }

    /// <param name="isVisiable">true=在摄像机可见范围内，应显示/播放；false=隐藏/停止（参数名保留拼写以免外部反射依赖）。</param>
    public void Cull(bool isVisiable)
    {
        if (_renderer != null)
            _renderer.enabled = isVisiable;

        if (_particleSystem != null)
        {
            if (isVisiable)
                _particleSystem.Play();
            else
                _particleSystem.Stop();
        }

        if (_visualEffect != null)
        {
            if (isVisiable)
                _visualEffect.Play();
            else
                _visualEffect.Stop();
        }
    }
}
