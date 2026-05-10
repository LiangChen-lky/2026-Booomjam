using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    private GameObject settingsPanel;

    void Awake()
    {
        startButton.onClick.AddListener(OnStartGame);
        settingsButton.onClick.AddListener(OnSettings);
        exitButton.onClick.AddListener(OnExitGame);

        // 按钮悬停音效
        AddHoverSound(startButton);
        AddHoverSound(settingsButton);
        AddHoverSound(exitButton);

        // 创建设置面板
        CreateSettingsPanel();
    }

    private void Start()
    {
        // 放到 Start，避免和 AudioManager 的 Awake 初始化顺序打架
        AudioManager.Instance.PlayBGM(BGM.MainMenu);
    }

    private void AddHoverSound(Button button)
    {
        var trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var hoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        hoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        hoverEntry.callback.AddListener((_) => AudioManager.Instance.Play(SFX.UIHover));
        trigger.triggers.Add(hoverEntry);
    }

    private void CreateSettingsPanel()
    {
        var canvas = FindMainMenuCanvas();

        if (canvas == null) return;

        settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(canvas.transform, false);
        var settingsRect = settingsPanel.AddComponent<RectTransform>();
        settingsRect.anchorMin = Vector2.zero;
        settingsRect.anchorMax = Vector2.one;
        settingsRect.sizeDelta = Vector2.zero;
        var settingsMenu = settingsPanel.AddComponent<SettingsMenu>();
        settingsMenu.Initialize(ReturnFromSettings);
        settingsPanel.SetActive(false);
    }

    private Canvas FindMainMenuCanvas()
    {
        var canvases = FindObjectsOfType<Canvas>(true);
        foreach (var canvas in canvases)
        {
            if (canvas != null && canvas.gameObject.scene == gameObject.scene)
            {
                return canvas;
            }
        }

        return null;
    }

    void OnStartGame()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        AudioManager.Instance.PlayBGM(BGM.Exploration);
        SceneManager.LoadScene("SampleScene");
    }

    void OnSettings()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void ReturnFromSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void OnExitGame()
    {
        AudioManager.Instance.Play(SFX.UIConfirmExit);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
