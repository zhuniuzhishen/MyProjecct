using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 视锥剔除管理器（单例）：收集子物体上的 <see cref="G_Culler"/>，用 Unity <see cref="CullingGroup"/>
/// 根据主摄像机可见性回调，批量开关渲染器/粒子，减少屏幕外开销。
/// </summary>
public class G_CullManager : MonoBehaviour
{
    public static G_CullManager Instance;

    /// <summary>参与剔除的物体列表，Start 时从子级自动收集。</summary>
    public List<G_Culler> _Cullers;

    CullingGroup _cullingGroup;
    BoundingSphere[] _boundingSphere;

    [Tooltip("MeshRenderer 类型使用的包围球半径")]
    public float CullingRadius = 10f;

    [Tooltip("粒子 / VFX 类型使用的包围球半径")]
    public float ParticleCullingRadius = 5f;

    private void Start()
    {
        _Cullers = new List<G_Culler>(GetComponentsInChildren<G_Culler>());
        SetupCullingGroup();
    }

    /// <summary>创建 CullingGroup、注册包围球与可见性变化回调；初始全部按不可见处理（Cull false）。</summary>
    private void SetupCullingGroup()
    {
        if (_Cullers == null)
            return;

        _cullingGroup = new CullingGroup();
        _cullingGroup.targetCamera = Camera.main;

        _boundingSphere = new BoundingSphere[_Cullers.Count];

        for (int i = 0; i < _Cullers.Count; i++)
        {
            float r = _Cullers[i].Type == G_Culler.CullerType.MeshRenderer ? CullingRadius : ParticleCullingRadius;
            _boundingSphere[i] = new BoundingSphere(_Cullers[i].Center, r);
            _Cullers[i].Cull(false);
        }

        _cullingGroup.SetBoundingSpheres(_boundingSphere);
        _cullingGroup.SetBoundingSphereCount(_boundingSphere.Length);
        _cullingGroup.onStateChanged += StateChangedMethod;
    }

    /// <summary>某个包围球可见性变化时，通知对应 G_Culler 开关显示。</summary>
    private void StateChangedMethod(CullingGroupEvent evt)
    {
        if (_Cullers == null || evt.index < 0 || evt.index >= _Cullers.Count)
            return;
        _Cullers[evt.index].Cull(evt.isVisible);
    }

    /// <summary>运行时动态加入剔除对象（当前实现未重建 BoundingSphere 数组，适合开局前注册）。</summary>
    public void AddCuller(G_Culler culler)
    {
        if (!_Cullers.Contains(culler))
            _Cullers.Add(culler);
    }

    private void OnDestroy()
    {
        if (_cullingGroup == null)
            return;

        _cullingGroup.onStateChanged -= StateChangedMethod;
        _cullingGroup.Dispose();
        _cullingGroup = null;
    }
}
