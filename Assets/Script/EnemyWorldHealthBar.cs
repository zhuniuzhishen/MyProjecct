using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 世界空间 UI Slider 血条：挂在敌人子物体上，跟随敌人本地位移，每帧朝向摄像机。
/// </summary>
[DisallowMultipleComponent]
public class EnemyWorldHealthBar : MonoBehaviour
{
    static Sprite s_simpleWhite;

    [SerializeField] Vector3 canvasScale = new Vector3(0.004f, 0.004f, 0.004f);
    [SerializeField] int sortingOrder = 50;

    Slider _slider;
    EnemyBase _enemy;

    static Sprite SimpleWhiteSprite()
    {
        if (s_simpleWhite == null)
        {
            var t = Texture2D.whiteTexture;
            s_simpleWhite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return s_simpleWhite;
    }

    void Awake()
    {
        _enemy = GetComponentInParent<EnemyBase>();
        BuildUi();
        RefreshValue();
    }

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

        RefreshValue();
    }

    void RefreshValue()
    {
        if (_enemy == null || _slider == null)
            return;
        float max = _enemy.maxHp > 0f ? _enemy.maxHp : 1f;
        _slider.value = Mathf.Clamp01(_enemy.hp / max);
    }

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

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
