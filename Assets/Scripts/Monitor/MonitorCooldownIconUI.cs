using UnityEngine;
using UnityEngine.UI;

public class MonitorCooldownIconUI : MonoBehaviour
{
    private static Sprite fallbackWhiteSprite;

    [Header("Source")]
    [SerializeField] private MonitorController monitorController;
    [SerializeField] private bool autoFindMonitorController = true;

    [Header("UI")]
    [SerializeField] private Button monitorButton;
    [SerializeField] private Image cooldownMask;
    [SerializeField] private GameObject cooldownRoot;
    [SerializeField] private bool hideWhenReady = true;
    [SerializeField] private Color maskColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Standalone")]
    [SerializeField] private float defaultCooldownDuration = 5f;

    private Text cooldownText;
    private bool standaloneCoolingDown;
    private float standaloneStartTime;
    private float standaloneDuration;

    private void Awake()
    {
        SetupButton();
        SetupMask();
        SetupCooldownText();
        Refresh(0f, 0f);
    }

    private void OnDestroy()
    {
        if (monitorButton != null)
            monitorButton.onClick.RemoveListener(OnMonitorButtonClicked);
    }

    private void Update()
    {
        if (monitorController == null && autoFindMonitorController)
            monitorController = MonitorController.Instance;

        if (monitorController != null)
        {
            Refresh(monitorController.CooldownRemaining, monitorController.CooldownDuration);
            return;
        }

        if (!standaloneCoolingDown)
        {
            Refresh(0f, standaloneDuration);
            return;
        }

        float elapsed = Time.unscaledTime - standaloneStartTime;
        float remaining = Mathf.Max(0f, standaloneDuration - elapsed);
        if (remaining <= 0f)
            standaloneCoolingDown = false;

        Refresh(remaining, standaloneDuration);
    }

    public void BeginCooldown()
    {
        BeginCooldown(defaultCooldownDuration);
    }

    public void BeginCooldown(float duration)
    {
        standaloneDuration = Mathf.Max(0f, duration);
        standaloneStartTime = Time.unscaledTime;
        standaloneCoolingDown = standaloneDuration > 0f;
        Refresh(standaloneDuration, standaloneDuration);
    }

    public void EndCooldown()
    {
        standaloneCoolingDown = false;
        Refresh(0f, standaloneDuration);
    }

    private void Refresh(float remaining, float duration)
    {
        bool coolingDown = remaining > 0f && duration > 0f;

        if (cooldownRoot != null && cooldownRoot != gameObject)
            cooldownRoot.SetActive(coolingDown || !hideWhenReady);

        if (cooldownMask != null)
        {
            cooldownMask.enabled = coolingDown || !hideWhenReady;
            cooldownMask.fillAmount = coolingDown ? Mathf.Clamp01(remaining / duration) : 0f;
        }

        if (cooldownText != null)
        {
            cooldownText.enabled = coolingDown || !hideWhenReady;
            cooldownText.text = coolingDown ? remaining.ToString("0.0") : string.Empty;
        }

        if (monitorButton != null)
        {
            bool monitorOpen = monitorController != null && monitorController.IsMonitorOpen;
            monitorButton.interactable = !coolingDown && !monitorOpen;
        }
    }

    private void SetupButton()
    {
        if (monitorButton == null)
            monitorButton = GetComponent<Button>();

        if (monitorButton == null)
            monitorButton = gameObject.AddComponent<Button>();

        var iconImage = GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.raycastTarget = true;
            monitorButton.targetGraphic = iconImage;
        }

        monitorButton.onClick.RemoveListener(OnMonitorButtonClicked);
        monitorButton.onClick.AddListener(OnMonitorButtonClicked);
    }

    private void OnMonitorButtonClicked()
    {
        if (monitorController == null && autoFindMonitorController)
            monitorController = MonitorController.Instance;

        if (monitorController == null || monitorController.IsMonitorOpen || monitorController.IsOnCooldown)
            return;

        monitorController.OpenMonitor();
    }

    private void SetupCooldownText()
    {
        if (cooldownText == null)
        {
            Transform existingText = transform.Find("cooldownText");
            var textGo = existingText != null ? existingText.gameObject : new GameObject("cooldownText");
            textGo.transform.SetParent(transform, false);
            cooldownText = textGo.GetComponent<Text>();
            if (cooldownText == null)
                cooldownText = textGo.AddComponent<Text>();
        }

        if (cooldownText == null)
            return;

        var rt = cooldownText.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        cooldownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cooldownText.fontSize = 24;
        cooldownText.color = Color.white;
        cooldownText.alignment = TextAnchor.MiddleCenter;
        cooldownText.raycastTarget = false;
        cooldownText.horizontalOverflow = HorizontalWrapMode.Overflow;
        cooldownText.verticalOverflow = VerticalWrapMode.Overflow;
        cooldownText.transform.SetAsLastSibling();
    }

    private void SetupMask()
    {
        if (cooldownMask == null)
        {
            Transform existingMask = transform.Find("cooldownMask");
            if (existingMask != null)
                cooldownMask = existingMask.GetComponent<Image>();

            if (cooldownMask == null)
            {
                var maskGo = existingMask != null ? existingMask.gameObject : new GameObject("cooldownMask");
                maskGo.transform.SetParent(transform, false);
                cooldownMask = maskGo.GetComponent<Image>();
                if (cooldownMask == null)
                    cooldownMask = maskGo.AddComponent<Image>();
            }
        }

        var rt = cooldownMask.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        var iconImage = GetComponent<Image>();
        if (cooldownMask.sprite == null)
            cooldownMask.sprite = iconImage != null && iconImage.sprite != null ? iconImage.sprite : EnsureSprite();

        cooldownMask.type = Image.Type.Filled;
        cooldownMask.fillMethod = Image.FillMethod.Radial360;
        cooldownMask.fillOrigin = (int)Image.Origin360.Top;
        cooldownMask.fillClockwise = false;
        cooldownMask.fillAmount = 0f;
        cooldownMask.color = maskColor;
        cooldownMask.raycastTarget = false;
        cooldownMask.transform.SetAsFirstSibling();
    }

    private static Sprite EnsureSprite()
    {
        if (fallbackWhiteSprite != null)
            return fallbackWhiteSprite;

        fallbackWhiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return fallbackWhiteSprite;
    }
}
