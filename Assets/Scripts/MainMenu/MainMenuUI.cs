using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject settingsPanelPrefab;

    private GameObject settingsPanel;

    private void Awake()
    {
        ResolveReferences();
        startButton.onClick.AddListener(OnStartGame);
        settingsButton.onClick.AddListener(OnSettings);
        exitButton.onClick.AddListener(OnExitGame);

        // 按钮悬停音效
        AddHoverSound(startButton);
        AddHoverSound(settingsButton);
        AddHoverSound(exitButton);

        EnsureSettingsPanel();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
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

    private void ResolveReferences()
    {
        if (startButton == null) startButton = FindChildComponent<Button>("StartButton");
        if (settingsButton == null) settingsButton = FindChildComponent<Button>("SettingsButton");
        if (exitButton == null) exitButton = FindChildComponent<Button>("ExitButton");
    }

    private void EnsureSettingsPanel()
    {
        if (settingsPanel != null)
            return;

        var prefab = settingsPanelPrefab != null ? settingsPanelPrefab : LoadSettingsPanelPrefab();
        if (prefab == null)
            return;

        var canvas = FindMainMenuCanvas();
        if (canvas == null)
            return;

        settingsPanel = Instantiate(prefab, canvas.transform, false);
        settingsPanel.name = "SettingsPanel";
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

    private T FindChildComponent<T>(string childName) where T : Component
    {
        var child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private GameObject LoadSettingsPanelPrefab()
    {
#if UNITY_EDITOR
        const string prefabPath = "Assets/Prefabs/UI/SettingsPanel.prefab";
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#else
        return null;
#endif
    }

    private void OnStartGame()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        AudioManager.Instance.PlayBGM(BGM.Exploration);
        SceneManager.LoadScene("SampleScene");
    }

    private void OnSettings()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        if (settingsPanel == null)
            EnsureSettingsPanel();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void ReturnFromSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnExitGame()
    {
        AudioManager.Instance.Play(SFX.UIConfirmExit);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
