using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏全局管理器（单例 Instance）：负责暂停/恢复时间缩放、胜利与失败 UI、
/// 血条与金币界面更新，以及 DOTween 驱动的面板与血条动画。场景中需存在同名 UI 物体供 Find 绑定。
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>全局访问入口，任意脚本可用 GameManager.Instance 调用。</summary>
    public static GameManager Instance;

    // ---------- 面板与按钮（Awake 里通过 GameObject.Find 绑定） ----------
    public GameObject PausePanel;
    public GameObject GameOverPanel;
    public GameObject GameWinPanel;

    public Button ResumeButton;
    public Button MainMenuButton;
    public Button RestartButton;
    public Button QuitButton;

    public bool isWin = false;
    public bool isFail = false;

    public Slider _healthSlider;
    public TMP_Text _coinText;

    /// <summary>血条填充图，用于受伤时闪红。</summary>
    Image _healthFillImage;
    /// <summary>血条默认颜色，闪红后 tween 回该色。</summary>
    Color _healthFillColorBase;

    // ---------- 可在 Inspector 调整的动画参数 ----------
    [SerializeField] float panelFadeDuration = 0.28f;
    [SerializeField] Vector2 pausePanelHiddenOffset = new Vector2(0f, -90f);
    [SerializeField] float healthTweenDuration = 0.35f;
    [SerializeField] Color healthHurtFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] float healthFlashPeakDuration = 0.07f;
    [SerializeField] float healthFlashRecoverDuration = 0.22f;
    [SerializeField] float endScreenDuration = 0.4f;
    [SerializeField] float autoMenuDelay = 4f;

    /// <summary>UI 上正在显示的金币数字（用于数字滚动动画）。</summary>
    int _displayedCoin = int.MinValue;
    Tweener _coinCountTween;

    Vector2 _pausePanelAnchoredRest;
    /// <summary>是否已成功缓存暂停面板的展开 anchoredPosition；为 true 才做位移类动画。</summary>
    bool _pausePanelLayoutCached;
    bool _pauseMenuOpen;

    private void Awake()
    {
        Time.timeScale = 1f;

        Instance = this;
        PausePanel = GameObject.Find("PausePanel");
        GameOverPanel = GameObject.Find("GameOverPanel");
        GameWinPanel = GameObject.Find("GameWinPanel");

        ResumeButton = GameObject.Find("ResumeButton").GetComponent<Button>();
        MainMenuButton = GameObject.Find("MainMenuButton").GetComponent<Button>();
        RestartButton = GameObject.Find("RestartButton").GetComponent<Button>();
        QuitButton = GameObject.Find("QuitButton").GetComponent<Button>();

        _coinText = GameObject.Find("CoinText").GetComponent<TMP_Text>();
        _healthSlider = GameObject.Find("HealthSlider").GetComponent<Slider>();

        if (_healthSlider != null && _healthSlider.fillRect != null)
        {
            _healthFillImage = _healthSlider.fillRect.GetComponent<Image>();
            if (_healthFillImage != null)
                _healthFillColorBase = _healthFillImage.color;
        }

        CachePausePanelRestLayout();
        HideAllPanelsImmediate();
        UpdateCoin(animateNumber: false);
        UpdateHealth(immediate: true);
    }

    /// <summary>开局时立刻隐藏暂停/结束面板，避免闪一下。</summary>
    void HideAllPanelsImmediate()
    {
        HidePausePanelImmediate();
        SetPanelHidden(GameOverPanel);
        SetPanelHidden(GameWinPanel);
    }

    /// <summary>记录暂停面板“展开位置”，用于从下方滑入/滑出动画。</summary>
    void CachePausePanelRestLayout()
    {
        if (PausePanel == null)
            return;
        var rt = PausePanel.GetComponent<RectTransform>();
        if (rt == null)
            return;
        _pausePanelAnchoredRest = rt.anchoredPosition;
        _pausePanelLayoutCached = true;
    }

    /// <summary>不播放动画，立刻把暂停面板藏到屏幕外并透明。</summary>
    void HidePausePanelImmediate()
    {
        if (PausePanel == null)
            return;

        var cg = PausePanel.GetComponent<CanvasGroup>();
        var rt = PausePanel.GetComponent<RectTransform>();
        if (cg == null)
            return;

        DOTween.Kill(cg, false);
        if (rt != null)
            DOTween.Kill(rt, false);

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        if (rt != null)
        {
            rt.localScale = Vector3.one;
            if (_pausePanelLayoutCached)
                rt.anchoredPosition = _pausePanelAnchoredRest + pausePanelHiddenOffset;
        }

        _pauseMenuOpen = false;
    }

    /// <summary>将带 CanvasGroup 的面板设为不可见且不拦截点击。</summary>
    static void SetPanelHidden(GameObject panel)
    {
        if (panel == null)
            return;

        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
            return;

        DOTween.Kill(cg, false);
        var rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            DOTween.Kill(rt, false);
            rt.localScale = Vector3.one;
        }

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    /// <summary>外部调用：刷新金币显示（带数字滚动动画）。</summary>
    public void UpdateCoin()
    {
        UpdateCoin(animateNumber: true);
    }

    /// <summary>根据 Player.coin 刷新金币文字；可选数字 tween 与结束时轻 punch 效果。</summary>
    void UpdateCoin(bool animateNumber)
    {
        if (_coinText == null || Player.Instance == null)
            return;

        int target = Player.Instance.coin;

        if (!animateNumber || _displayedCoin == int.MinValue)
        {
            _coinCountTween?.Kill();
            _displayedCoin = target;
            _coinText.text = target.ToString();
            return;
        }

        if (target == _displayedCoin)
            return;

        _coinCountTween?.Kill();
        _coinCountTween = DOTween.To(() => _displayedCoin, x =>
        {
            _displayedCoin = x;
            _coinText.text = x.ToString();
        }, target, 0.45f).SetEase(Ease.OutQuad).SetUpdate(true)
            .OnComplete(() =>
            {
                _coinText.transform.DOPunchScale(Vector3.one * 0.12f, 0.22f, 6, 0.4f).SetUpdate(true);
            });
    }

    /// <summary>外部调用：根据当前 Player 血量比例 tween 血条。</summary>
    public void UpdateHealth()
    {
        UpdateHealth(immediate: false);
    }

    /// <summary>根据 Player 血量比例更新 Slider；immediate 时直接设值无动画。</summary>
    void UpdateHealth(bool immediate)
    {
        if (_healthSlider == null || Player.Instance == null)
            return;

        float v = Player.Instance.hp / Player.Instance.maxHp;

        if (immediate)
        {
            _healthSlider.DOKill(false);
            _healthSlider.value = v;
            return;
        }

        _healthSlider.DOKill(false);
        _healthSlider.DOValue(v, healthTweenDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>玩家受伤时调用：血条填充色短暂变红再恢复。</summary>
    public void PlayHealthHurtFlash()
    {
        if (_healthFillImage == null)
            return;

        DOTween.Kill(_healthFillImage, false);
        var seq = DOTween.Sequence();
        seq.Append(_healthFillImage.DOColor(healthHurtFlashColor, healthFlashPeakDuration));
        seq.Append(_healthFillImage.DOColor(_healthFillColorBase, healthFlashRecoverDuration));
    }

    void Start()
    {
        ResumeButton.onClick.AddListener(ResumeGame);
        RestartButton.onClick.AddListener(RestartGame);
        QuitButton.onClick.AddListener(ExitGame);
        MainMenuButton.onClick.AddListener(GoMainMenu);
    }

    /// <summary>ESC：未结束时切换暂停；已暂停则恢复（胜利/失败时不响应）。</summary>
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (isWin || isFail)
            return;

        if (_pauseMenuOpen)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>打开暂停面板并把 Time.timeScale 设为 0（游戏逻辑暂停）。</summary>
    public void PauseGame()
    {
        if (_pauseMenuOpen)
            return;

        var cg = PausePanel != null ? PausePanel.GetComponent<CanvasGroup>() : null;
        var rt = PausePanel != null ? PausePanel.GetComponent<RectTransform>() : null;
        if (cg == null)
            return;

        if (isWin || isFail)
            return;

        _pauseMenuOpen = true;

        DOTween.Kill(PausePanel.transform, false);

        cg.blocksRaycasts = true;
        cg.interactable = true;

        if (rt != null && _pausePanelLayoutCached)
        {
            rt.anchoredPosition = _pausePanelAnchoredRest + pausePanelHiddenOffset;
            cg.alpha = 0f;
        }

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(cg.DOFade(1f, panelFadeDuration).SetEase(Ease.OutQuad));
        if (rt != null && _pausePanelLayoutCached)
            seq.Join(rt.DOAnchorPos(_pausePanelAnchoredRest, panelFadeDuration).SetEase(Ease.OutCubic));

        Time.timeScale = 0;
    }

    /// <summary>关闭暂停面板，动画结束后恢复 Time.timeScale = 1。</summary>
    public void ResumeGame()
    {
        if (!_pauseMenuOpen)
            return;

        var cg = PausePanel != null ? PausePanel.GetComponent<CanvasGroup>() : null;
        var rt = PausePanel != null ? PausePanel.GetComponent<RectTransform>() : null;
        if (cg == null)
            return;

        DOTween.Kill(PausePanel.transform, false);

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(cg.DOFade(0f, panelFadeDuration).SetEase(Ease.InQuad));
        if (rt != null && _pausePanelLayoutCached)
            seq.Join(rt.DOAnchorPos(_pausePanelAnchoredRest + pausePanelHiddenOffset, panelFadeDuration).SetEase(Ease.InCubic));
        seq.OnComplete(() =>
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
            Time.timeScale = 1f;
            _pauseMenuOpen = false;
        });
    }

    /// <summary>关卡胜利：标记 isWin、显示胜利面板、延迟自动回主菜单。</summary>
    public void Win()
    {
        isWin = true;
        ShowEndPanel(GameWinPanel);
        ScheduleAutoReturnToMenu();
    }

    /// <summary>玩家死亡等：标记 isFail、显示失败面板、延迟自动回主菜单。</summary>
    public void GameOver()
    {
        isFail = true;
        ShowEndPanel(GameOverPanel);
        ScheduleAutoReturnToMenu();
    }

    /// <summary>胜利/失败面板的淡入与轻微缩放弹出动画。</summary>
    void ShowEndPanel(GameObject panel)
    {
        if (panel == null)
            return;

        var cg = panel.GetComponent<CanvasGroup>();
        var rt = panel.GetComponent<RectTransform>();
        if (cg == null)
            return;

        DOTween.Kill(panel.transform, false);

        cg.alpha = 0f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        if (rt != null)
            rt.localScale = Vector3.one * 0.94f;

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(cg.DOFade(1f, endScreenDuration * 1.05f).SetEase(Ease.OutQuad));
        if (rt != null)
            seq.Join(rt.DOScale(1f, endScreenDuration).SetEase(Ease.OutBack, 1.35f));
    }

    /// <summary>加载场景索引 0（主菜单），并恢复时间缩放。</summary>
    public void GoMainMenu()
    {
        DOTween.Kill(this, false);
        _pauseMenuOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    /// <summary>退出应用程序（编辑器内可能无效果）。</summary>
    public void ExitGame()
    {
        Application.Quit();
    }

    /// <summary>重新加载当前关卡场景。</summary>
    public void RestartGame()
    {
        DOTween.Kill(this, false);
        _pauseMenuOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>胜利/失败后等待 autoMenuDelay 秒自动打开主菜单。</summary>
    void ScheduleAutoReturnToMenu()
    {
        DOTween.Kill(this, false);
        DOVirtual.DelayedCall(autoMenuDelay, GoMainMenu).SetId(this).SetUpdate(true);
    }

    /// <summary>销毁时杀掉本管理器相关的 Tween，避免切场景后仍回调。</summary>
    void OnDestroy()
    {
        DOTween.Kill(this, false);
        if (PausePanel != null)
            DOTween.Kill(PausePanel.transform, false);
        if (GameOverPanel != null)
            DOTween.Kill(GameOverPanel.transform, false);
        if (GameWinPanel != null)
            DOTween.Kill(GameWinPanel.transform, false);
        if (_healthSlider != null)
            _healthSlider.DOKill(false);
        if (_healthFillImage != null)
            DOTween.Kill(_healthFillImage, false);
        if (_coinText != null)
            DOTween.Kill(_coinText.transform, false);
    }
}
