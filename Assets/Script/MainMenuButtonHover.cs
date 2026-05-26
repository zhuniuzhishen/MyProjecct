using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 按钮悬停：鼠标移入时变色并略放大，移出恢复。需场景中有 EventSystem，且按钮的 Graphic 可接收射线。
/// 由 <see cref="MainMenuController"/> 在运行时挂上并调用 <see cref="Configure"/>。
/// </summary>
[RequireComponent(typeof(Button))]
public class MainMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Button _button;
    RectTransform _rect;
    Graphic _graphic;

    Color _normalColor;
    Vector3 _baseScale;

    Color _hoverColor;
    float _hoverScaleMul = 1.08f;
    float _duration = 0.12f;

    /// <summary>记录当前配色与缩放，关闭 Button 自带 ColorTint，避免与手动改色冲突。</summary>
    public void Configure(Color hoverColor, float hoverScaleMul, float duration)
    {
        _button = GetComponent<Button>();
        _rect = GetComponent<RectTransform>();
        _graphic = _button.targetGraphic != null ? _button.targetGraphic : GetComponent<Graphic>();

        if (_graphic != null)
            _normalColor = _graphic.color;
        _baseScale = _rect != null ? _rect.localScale : Vector3.one;

        _hoverColor = hoverColor;
        _hoverScaleMul = Mathf.Max(0.01f, hoverScaleMul);
        _duration = Mathf.Max(0.01f, duration);

        if (_button != null)
            _button.transition = Selectable.Transition.None;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enabled || _rect == null)
            return;

        KillTweens();
        _rect.DOScale(_baseScale * _hoverScaleMul, _duration).SetEase(Ease.OutQuad).SetUpdate(true);

        if (_graphic != null)
            _graphic.DOColor(_hoverColor, _duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enabled || _rect == null)
            return;

        KillTweens();
        _rect.DOScale(_baseScale, _duration).SetEase(Ease.OutQuad).SetUpdate(true);

        if (_graphic != null)
            _graphic.DOColor(_normalColor, _duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    void KillTweens()
    {
        if (_rect != null)
            _rect.DOKill(false);
        if (_graphic != null)
            _graphic.DOKill(false);
    }

    void OnDisable()
    {
        KillTweens();
        if (_rect != null)
            _rect.localScale = _baseScale;
        if (_graphic != null)
            _graphic.color = _normalColor;
    }

    void OnDestroy()
    {
        KillTweens();
    }
}
