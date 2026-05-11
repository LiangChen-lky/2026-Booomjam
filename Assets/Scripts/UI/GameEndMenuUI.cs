using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Victory / failure end screen with retry and return-to-menu actions.
/// The component can bind to an authored prefab hierarchy or build a fallback UI at runtime.
/// </summary>
public class GameEndMenuUI : MonoBehaviour
{
    private GameObject canvasRoot;
    private GameObject menuRoot;
    private Text titleText;
    private Button retryButton;
    private Button mainMenuButton;

    private UnityEngine.Events.UnityAction retryAction;
    private UnityEngine.Events.UnityAction mainMenuAction;

    private void Awake()
    {
        ResolveUIReferences();
        if (menuRoot == null)
        {
            CreateLegacyUI();
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
    }

    public void Configure(UnityEngine.Events.UnityAction onRetry, UnityEngine.Events.UnityAction onMainMenu)
    {
        retryAction = onRetry;
        mainMenuAction = onMainMenu;

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(HandleRetryClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }
    }

    public void Show()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        AudioManager.Instance.Play(SFX.UIPopupOpen);
    }

    public void Hide()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
    }

    private void HandleRetryClicked()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        ResumeBeforeTransition();
        retryAction?.Invoke();
    }

    private void HandleMainMenuClicked()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        ResumeBeforeTransition();
        mainMenuAction?.Invoke();
    }

    private void ResumeBeforeTransition()
    {
        Time.timeScale = 1f;
    }

    private void ResolveUIReferences()
    {
        canvasRoot = GetComponent<Canvas>() != null ? gameObject : FindChildRecursive(transform, "GameEndCanvas")?.gameObject;
        if (canvasRoot == null)
        {
            canvasRoot = FindChildRecursive(transform, "Canvas")?.gameObject;
        }

        if (canvasRoot != null)
        {
            menuRoot = FindChildRecursive(canvasRoot.transform, "GameEndMenuRoot")?.gameObject;
            if (menuRoot == null)
            {
                menuRoot = FindChildRecursive(canvasRoot.transform, "EndMenuRoot")?.gameObject;
            }
        }

        if (menuRoot != null)
        {
            titleText = FindChildRecursive(menuRoot.transform, "Title")?.GetComponent<Text>();
            retryButton = FindChildRecursive(menuRoot.transform, "RetryButton")?.GetComponent<Button>();
            mainMenuButton = FindChildRecursive(menuRoot.transform, "MainMenuButton")?.GetComponent<Button>();
        }
    }

    private void CreateLegacyUI()
    {
        canvasRoot = new GameObject("GameEndCanvas");
        canvasRoot.transform.SetParent(transform, false);

        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasRoot.AddComponent<GraphicRaycaster>();

        menuRoot = new GameObject("GameEndMenuRoot");
        menuRoot.transform.SetParent(canvasRoot.transform, false);

        var bg = menuRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.78f);
        bg.raycastTarget = true;

        var menuRect = menuRoot.GetComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(menuRoot.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "游戏结束";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 52;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600f, 80f);
        titleRect.anchoredPosition = new Vector2(0f, 120f);

        var buttons = new GameObject("Buttons");
        buttons.transform.SetParent(menuRoot.transform, false);
        var layout = buttons.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var btnRect = buttons.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0f, -20f);
        btnRect.sizeDelta = new Vector2(320f, 180f);

        retryButton = CreateButton(buttons.transform, "RetryButton", "重新开始");
        mainMenuButton = CreateButton(buttons.transform, "MainMenuButton", "返回主菜单");

        ConfigureButton(retryButton, HandleRetryClicked);
        ConfigureButton(mainMenuButton, HandleMainMenuClicked);
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.05f, 0.05f, 1f);
        button.colors = colors;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, 54f);

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(go.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static void ConfigureButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
