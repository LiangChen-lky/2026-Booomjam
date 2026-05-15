using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 主菜单游戏说明面板。
/// </summary>
public class GameInstructionsMenu : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [TextArea(8, 24)]
    [SerializeField]
    private string instructionsText =
        "【基本操作】\n" +
        "W / A / S / D：移动\n" +
        "鼠标：控制角色朝向\n" +
        "E：与物体互动\n" +
        "Alt：显示 / 隐藏鼠标光标\n" +
        "Esc：暂停游戏\n\n" +
        "【监控平板】\n" +
        "R：打开 / 关闭监控\n" +
        "Z / X：切换监控画面\n\n" +
        "【游戏目标】\n" +
        "在黑暗的校园中寻找钥匙与线索，躲避怪物，最终从校门逃脱。";

    private UnityAction closeAction;
    private bool uiBuilt;

    private void Awake()
    {
        EnsureUI();
        CacheReferences();
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    public void Initialize(UnityAction onClose)
    {
        closeAction = onClose;
    }

    private void EnsureUI()
    {
        if (uiBuilt)
            return;

        var rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = gameObject.AddComponent<RectTransform>();

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var dim = gameObject.GetComponent<Image>();
        if (dim == null)
            dim = gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(920f, 720f);

        var panelBg = panel.AddComponent<Image>();
        var panelSprite = LoadSprite("Assets/Sprites/ui/ui_setup_bg.jpg");
        if (panelSprite != null)
        {
            panelBg.sprite = panelSprite;
            panelBg.type = Image.Type.Simple;
            panelBg.color = Color.white;
        }
        else
        {
            panelBg.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);
        }

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var title = titleObj.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.text = "游戏说明";
        title.fontSize = 40;
        title.fontStyle = FontStyle.Bold;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(500f, 60f);

        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panel.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = new Vector2(0f, 10f);
        scrollRect.sizeDelta = new Vector2(780f, 500f);

        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        var viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        var viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.25f);
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 500f);

        var bodyObj = new GameObject("BodyText");
        bodyObj.transform.SetParent(contentObj.transform, false);
        var body = bodyObj.AddComponent<Text>();
        body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        body.text = instructionsText;
        body.fontSize = 24;
        body.color = Color.white;
        body.alignment = TextAnchor.UpperLeft;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        var bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = new Vector2(-40f, 500f);
        bodyRect.offsetMin = new Vector2(20f, 0f);
        bodyRect.offsetMax = new Vector2(-20f, 0f);

        var fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var bodyFitter = bodyObj.AddComponent<ContentSizeFitter>();
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        var backObj = new GameObject("BackButton");
        backObj.transform.SetParent(panel.transform, false);
        var backRect = backObj.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 36f);
        backRect.sizeDelta = new Vector2(180f, 48f);

        var backImage = backObj.AddComponent<Image>();
        var backSprite = LoadSprite("Assets/Sprites/ui/button.png");
        if (backSprite != null)
        {
            backImage.sprite = backSprite;
            backImage.type = Image.Type.Sliced;
            backImage.color = Color.white;
        }
        else
        {
            backImage.color = new Color(0.55f, 0.1f, 0.1f, 1f);
        }

        backButton = backObj.AddComponent<Button>();
        backButton.targetGraphic = backImage;

        var backLabelObj = new GameObject("Text");
        backLabelObj.transform.SetParent(backObj.transform, false);
        var backLabel = backLabelObj.AddComponent<Text>();
        backLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        backLabel.text = "返回";
        backLabel.fontSize = 26;
        backLabel.color = Color.white;
        backLabel.alignment = TextAnchor.MiddleCenter;
        var backLabelRect = backLabelObj.GetComponent<RectTransform>();
        backLabelRect.anchorMin = Vector2.zero;
        backLabelRect.anchorMax = Vector2.one;
        backLabelRect.offsetMin = Vector2.zero;
        backLabelRect.offsetMax = Vector2.zero;

        uiBuilt = true;
    }

    private void CacheReferences()
    {
        if (backButton == null)
            backButton = FindChildComponent<Button>("BackButton");
    }

    private void OnBackClicked()
    {
        AudioManager.Instance.Play(SFX.UIClick);
        if (closeAction != null)
            closeAction.Invoke();
        else
            gameObject.SetActive(false);
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var child in transforms)
        {
            if (child.name == childName)
                return child.GetComponent<T>();
        }

        return null;
    }

    private static Sprite LoadSprite(string assetPath)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
        return null;
#endif
    }
}
