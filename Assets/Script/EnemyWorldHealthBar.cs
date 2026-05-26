using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 世界空间 UI 血条：挂在敌人子物体上，跟随敌人本地位移；
/// 每帧 LateUpdate 朝向主摄像机（Billboard）；用 DOTween 平滑血量变化；死亡后隐藏。
/// 若 EnemyBase 启用自动生成，会在运行时 BuildUi 动态创建 Slider 与 Image。
/// </summary>
[DisallowMultipleComponent]
public class EnemyWorldHealthBar : MonoBehaviour
{
    static Sprite s_simpleWhite;

    [SerializeField] Vector3 canvasScale = new Vector3(0.004f, 0.004f, 0.004f);
    [SerializeField] int sortingOrder = 50;
    [SerializeField] float fillTweenDuration = 0.28f;

    Slider _slider;
    EnemyBase _enemy;
    float _cachedHp = float.NaN;

    static Sprite SimpleWhiteSprite()
    {
        if (s_simpleWhite == null)
        {
            var t = Texture2D.whiteTexture;
            s_simpleWhite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return s_simpleWhite;
    }

    /// <summary>查找父级敌人并生成 UI，首次把 Slider 拉到当前血量比例。</summary>
    void Awake()
    {
        _enemy = GetComponentInParent<EnemyBase>();
        BuildUi();
        RefreshValueTweened();
    }

    /// <summary>Billboard + 死亡隐藏 + 血量变化时 tween Slider。</summary>
    void LateUpdate()
    {
        if (_enemy == null || _slider == null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        transform.rotation = cam.transform.rotation;

        if (_enemy.isDead)
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        RefreshValueTweened();
    }

    /// <summary>将 Slider.value 设为 hp/maxHp；血量相对上次缓存变化时用 DOValue 过渡。</summary>
    void RefreshValueTweened()
    {
        if (_enemy == null || _slider == null)
            return;

        float max = _enemy.maxHp > 0f ? _enemy.maxHp : 1f;
        float target = Mathf.Clamp01(_enemy.hp / max);

        if (float.IsNaN(_cachedHp))
        {
            _cachedHp = _enemy.hp;
            _slider.DOKill(false);
            _slider.value = target;
            return;
        }

        if (Mathf.Approximately(_enemy.hp, _cachedHp))
            return;

        _cachedHp = _enemy.hp;
        _slider.DOKill(false);
        _slider.DOValue(target, fillTweenDuration).SetEase(Ease.OutQuad);
    }

    void OnDisable()
    {
        if (_slider != null)
            _slider.DOKill(false);
    }

    /// <summary>运行时拼出一个 World Space Canvas + Slider（背景、填充区、隐藏把手区以满足 Slider 结构）。</summary>
    void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = sortingOrder;

        var canvasRt = GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(260f, 30f);
        canvasRt.localScale = canvasScale;

        var sliderGo = new GameObject("HealthSlider");
        sliderGo.transform.SetParent(transform, false);
        var sliderRt = sliderGo.AddComponent<RectTransform>();
        StretchFull(sliderRt);

        _slider = sliderGo.AddComponent<Slider>();
        _slider.transition = Selectable.Transition.None;
        _slider.interactable = false;
        _slider.navigation = new Navigation { mode = Navigation.Mode.None };

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = SimpleWhiteSprite();
        bgImg.type = Image.Type.Simple;
        bgImg.color = new Color(0f, 0f, 0f, 0.686f);
        bgImg.raycastTarget = false;

        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.sprite = SimpleWhiteSprite();
        fillImg.type = Image.Type.Simple;
        fillImg.color = new Color(0.92f, 0.18f, 0.18f, 1f);
        fillImg.raycastTarget = false;

        var handleAreaGo = new GameObject("Handle Slide Area");
        handleAreaGo.transform.SetParent(sliderGo.transform, false);
        handleAreaGo.SetActive(false);
        var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
        StretchFull(handleAreaRt);
        handleAreaRt.offsetMin = new Vector2(10f, 0f);
        handleAreaRt.offsetMax = new Vector2(-10f, 0f);

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        var handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.zero;
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(20f, 0f);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = SimpleWhiteSprite();
        handleImg.color = new Color(1f, 1f, 1f, 0.02f);
        handleImg.raycastTarget = false;

        _slider.fillRect = fillRt;
        _slider.handleRect = handleRt;
        _slider.targetGraphic = handleImg;
        _slider.direction = Slider.Direction.LeftToRight;
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
        _slider.value = 1f;
    }

    /// <summary>把 RectTransform 四锚点拉满父级，用于铺满条形容器。</summary>
    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
