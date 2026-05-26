using System.Collections;
using UnityEngine;

/// <summary>
/// 关卡栅栏门：Spawner 在“第一波剩余 2 只敌人”时调用 OpenGate；
/// 协程在 duration 秒内把门从当前位置沿世界 Y 轴插值移动到 targetY（负值表示下降）。
/// </summary>
public class Gate : MonoBehaviour
{
    public static Gate Instance;

    [Tooltip("相对当前位置沿 Y 轴移动的距离（负值=向下开门）")]
    public float targetY = -2.5f;

    [Tooltip("开门动画总时长（秒）")]
    public float duration = 2f;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>启动开门协程（可重复调用会叠加多个协程，一般只设计调用一次）。</summary>
    public void OpenGate()
    {
        StartCoroutine(OpenGateAnim());
    }

    IEnumerator OpenGateAnim()
    {
        float currentDuration = 0;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * targetY;

        while (currentDuration < duration)
        {
            currentDuration += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, currentDuration / duration);
            yield return null;
        }
    }
}
