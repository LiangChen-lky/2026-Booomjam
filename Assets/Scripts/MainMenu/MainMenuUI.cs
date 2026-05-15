using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button instructionsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Sprite instructionsButtonSprite;
    [SerializeField] private GameObject settingsPanelPrefab;

    private GameObject settingsPanel;
    private GameObject instructionsPanel;

    private void Awake()
    {
        ResolveReferences();
        EnsureInstructionsButton();
        startButton.onClick.AddListener(OnStartGame);
        if (instructionsButton != null)
            instructionsButton.onClick.AddListener(OnInstructions);
        settingsButton.onClick.AddListener(OnSettings);
        exitButton.onClick.AddListener(OnExitGame);

        // 按钮悬停音效
        AddHoverSound(startButton);
        if (instructionsButton != null)
            AddHoverSound(instructionsButton);
        AddHoverSound(settingsButton);
        AddHoverSound(exitButton);

        EnsureSettingsPanel();
        EnsureInstructionsPanel();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
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
        if (instructionsButton == null) instructionsButton = FindChildComponent<Button>("ExplainButton");
        if (settingsButton == null) settingsButton = FindChildComponent<Button>("SettingsButton");
        if (exitButton == null) exitButton = FindChildComponent<Button>("ExitButton");
    }

    private void EnsureInstructionsButton()
    {
        if (instructionsButton != null)
            return;

        var buttonsParent = FindSceneTransform("Buttons");
        if (buttonsParent == null)
            return;

        var template = buttonsParent.Find("StartButton");
        if (template == null)
            template = buttonsParent.Find("SettingsButton");
        if (template == null)
            return;

        var buttonObject = Instantiate(template.gameObject, buttonsParent);
        buttonObject.name = "ExplainButton";
        buttonObject.transform.SetSiblingIndex(Mathf.Min(1, buttonsParent.childCount - 1));

        var image = buttonObject.GetComponent<Image>();
        var sprite = instructionsButtonSprite != null ? instructionsButtonSprite : LoadDefaultInstructionsButtonSprite();
        if (image != null && sprite != null)
            image.sprite = sprite;

        instructionsButton = buttonObject.GetComponent<Button>();
    }

    private static Sprite LoadDefaultInstructionsButtonSprite()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ui/ui_start_说明.png");
#else
        return null;
#endif
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
        var child = FindSceneTransform(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindSceneTransform(string childName)
    {
        var found = FindChildRecursive(transform, childName);
        if (found != null)
            return found;

        var canvas = FindMainMenuCanvas();
        if (canvas == null)
            return null;

        return FindChildRecursive(canvas.transform, childName);
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
        var fromResources = Resources.Load<GameObject>("UI/SettingsPanel");
        if (fromResources != null)
            return fromResources;
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
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
        if (settingsPanel == null)
            EnsureSettingsPanel();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void OnInstructions()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (instructionsPanel == null)
            EnsureInstructionsPanel();
        if (instructionsPanel != null)
            instructionsPanel.SetActive(true);
    }

    public void ReturnFromSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ReturnFromInstructions()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
    }

    private void EnsureInstructionsPanel()
    {
        if (instructionsPanel != null)
            return;

        var canvas = FindMainMenuCanvas();
        if (canvas == null)
            return;

        instructionsPanel = new GameObject("InstructionsPanel");
        instructionsPanel.transform.SetParent(canvas.transform, false);
        var menu = instructionsPanel.AddComponent<GameInstructionsMenu>();
        menu.Initialize(ReturnFromInstructions);
        instructionsPanel.SetActive(false);
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
