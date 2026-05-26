using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单（场景 0）：绑定开始/退出；无入场动画。悬停变色与放大由 <see cref="MainMenuButtonHover"/> 实现。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public Button Button_Start;
    public Button Button_Quit;

    [Header("悬停（两个按钮共用）")]
    [SerializeField] Color hoverTint = new Color(1f, 0.92f, 0.65f, 1f);

    [SerializeField] float hoverScale = 1.08f;

    [SerializeField] float hoverTweenDuration = 0.12f;

    private void Awake()
    {
        Time.timeScale = 1f;

        Button_Start = GameObject.Find("Button_Start").GetComponent<Button>();
        Button_Quit = GameObject.Find("Button_Quit").GetComponent<Button>();
    }

    void Start()
    {
        Button_Start.onClick.AddListener(StartGame);
        Button_Quit.onClick.AddListener(ExitGame);

        SetupHover(Button_Start);
        SetupHover(Button_Quit);
    }

    void SetupHover(Button b)
    {
        if (b == null)
            return;

        var hover = b.GetComponent<MainMenuButtonHover>();
        if (hover == null)
            hover = b.gameObject.AddComponent<MainMenuButtonHover>();

        hover.Configure(hoverTint, hoverScale, hoverTweenDuration);
    }

    /// <summary>进入游戏关卡（Build Settings 里索引 1 应对应游戏场景）。</summary>
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    /// <summary>退出应用（编辑器播放模式可能看不出效果）。</summary>
    public void ExitGame()
    {
        Application.Quit();
    }
}
