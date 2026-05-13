using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monitor terminal overlay.
/// Handles signal-loss overlay and image feed display.
/// </summary>
public class MonitorCameraUI : MonoBehaviour
{
    private static Sprite fallbackWhiteSprite;

    [SerializeField, Tooltip("Signal lost fullscreen overlay, optional.")]
    private Image signalLostOverlay;

    [SerializeField, Tooltip("Monitor background overlay.")]
    private Image ui_monitoring_bg;

    [SerializeField, Tooltip("Recording red indicator.")]
    private Image ui_monitoring_rec;

    [SerializeField, Tooltip("TMP text named RoomName in the monitor UI prefab.")]
    private TMP_Text roomNameText;

    [SerializeField, Tooltip("Button named CloseButton in the monitor UI prefab.")]
    private Button closeButton;

    private Image cameraFeedImage;
    private Image switchStaticImage;
    private readonly System.Collections.Generic.List<Image> trackedBlipImages = new System.Collections.Generic.List<Image>();
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        SetupCanvasGroup();
        SetupVisuals();
        SetupCameraFeedImage();
        SetupSwitchStaticImage();
        SetupRoomNameText();
        SetupCloseButton();
        Hide();
        if (signalLostOverlay != null) signalLostOverlay.enabled = false;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
    }

    public void Show()
    {
        SetVisible(true);
        if (cameraFeedImage != null) cameraFeedImage.enabled = cameraFeedImage.sprite != null;
        if (switchStaticImage != null) switchStaticImage.enabled = false;
        if (ui_monitoring_bg != null) ui_monitoring_bg.enabled = true;
        if (ui_monitoring_rec != null) ui_monitoring_rec.enabled = true;
        if (roomNameText != null) roomNameText.enabled = true;
        RefreshRoomName();
    }

    public void Hide()
    {
        if (cameraFeedImage != null) cameraFeedImage.enabled = false;
        if (switchStaticImage != null) switchStaticImage.enabled = false;
        SetTrackedBlips(null, 0);
        if (ui_monitoring_bg != null) ui_monitoring_bg.enabled = false;
        if (ui_monitoring_rec != null) ui_monitoring_rec.enabled = false;
        if (roomNameText != null) roomNameText.enabled = false;
        if (signalLostOverlay != null) signalLostOverlay.enabled = false;
        SetVisible(false);
    }

    public void SetCameraFeed(Sprite feedSprite)
    {
        SetupCameraFeedImage();

        if (cameraFeedImage == null)
            return;

        cameraFeedImage.sprite = feedSprite;
        cameraFeedImage.enabled = feedSprite != null && gameObject.activeInHierarchy;
    }

    public void SetSwitchStatic(Sprite staticSprite, bool visible)
    {
        SetupSwitchStaticImage();

        if (switchStaticImage == null)
            return;

        switchStaticImage.sprite = staticSprite;
        switchStaticImage.enabled = visible && staticSprite != null && gameObject.activeInHierarchy;
    }

    public void SetTrackedBlips(System.Collections.Generic.IList<MonitorController.MonitorTrackedBlip> blips, int count)
    {
        SetupCameraFeedImage();

        int visibleCount = blips != null ? Mathf.Clamp(count, 0, blips.Count) : 0;
        EnsureTrackedBlipImages(visibleCount);

        RectTransform feedRect = cameraFeedImage != null ? cameraFeedImage.rectTransform : null;
        for (int i = 0; i < trackedBlipImages.Count; i++)
        {
            var blipImage = trackedBlipImages[i];
            if (blipImage == null)
                continue;

            bool visible = i < visibleCount && gameObject.activeInHierarchy && feedRect != null;
            blipImage.enabled = visible;
            if (!visible)
                continue;

            var blip = blips[i];
            blipImage.color = blip.color;

            RectTransform rt = blipImage.rectTransform;
            rt.SetParent(feedRect, false);
            Vector2 displayPosition = GetFeedDisplayPosition(blip.normalizedPosition, feedRect);
            rt.anchorMin = displayPosition;
            rt.anchorMax = displayPosition;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(blip.size, blip.size);
        }
    }

    private void LateUpdate()
    {
        var monitor = MonitorController.Instance;
        if (monitor == null || !monitor.IsMonitorOpen) return;

        RefreshRoomName();
    }

    private void SetupVisuals()
    {
        if (ui_monitoring_bg != null)
        {
            ui_monitoring_bg.sprite = EnsureSprite(ui_monitoring_bg.sprite);
            ui_monitoring_bg.color = new Color(0f, 0f, 0f, 0.08f);
            ui_monitoring_bg.raycastTarget = false;
        }

        if (ui_monitoring_rec != null)
        {
            ui_monitoring_rec.sprite = EnsureSprite(ui_monitoring_rec.sprite);
            ui_monitoring_rec.color = new Color(1f, 0.08f, 0.08f, 1f);
            ui_monitoring_rec.raycastTarget = false;

            var rt = ui_monitoring_rec.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(32f, -32f);
            rt.sizeDelta = new Vector2(56f, 24f);
        }

        if (signalLostOverlay != null)
        {
            signalLostOverlay.sprite = EnsureSprite(signalLostOverlay.sprite);
            signalLostOverlay.color = new Color(1f, 1f, 1f, 1f);
            signalLostOverlay.raycastTarget = false;
        }
    }

    private void SetupCanvasGroup()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            SetupCanvasGroup();

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void SetupCameraFeedImage()
    {
        if (cameraFeedImage == null)
        {
            var feedGo = new GameObject("cameraFeedImage");
            feedGo.transform.SetParent(transform, false);
            cameraFeedImage = feedGo.AddComponent<Image>();
        }

        if (cameraFeedImage == null)
            return;

        var rt = cameraFeedImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        cameraFeedImage.type = Image.Type.Simple;
        cameraFeedImage.preserveAspect = true;
        cameraFeedImage.color = Color.white;
        cameraFeedImage.raycastTarget = false;
        cameraFeedImage.transform.SetAsFirstSibling();
    }

    private Vector2 GetFeedDisplayPosition(Vector2 normalizedPosition, RectTransform feedRect)
    {
        if (cameraFeedImage == null || cameraFeedImage.sprite == null || feedRect == null)
            return normalizedPosition;

        Rect rect = feedRect.rect;
        if (rect.width <= 0.01f || rect.height <= 0.01f)
            return normalizedPosition;

        Rect spriteRect = cameraFeedImage.sprite.rect;
        if (spriteRect.width <= 0.01f || spriteRect.height <= 0.01f)
            return normalizedPosition;

        float feedAspect = rect.width / rect.height;
        float spriteAspect = spriteRect.width / spriteRect.height;
        Vector2 displayPosition = normalizedPosition;

        if (spriteAspect > feedAspect)
        {
            float displayedHeight = feedAspect / spriteAspect;
            float verticalPadding = (1f - displayedHeight) * 0.5f;
            displayPosition.y = verticalPadding + normalizedPosition.y * displayedHeight;
        }
        else
        {
            float displayedWidth = spriteAspect / feedAspect;
            float horizontalPadding = (1f - displayedWidth) * 0.5f;
            displayPosition.x = horizontalPadding + normalizedPosition.x * displayedWidth;
        }

        return displayPosition;
    }

    private void EnsureTrackedBlipImages(int count)
    {
        while (trackedBlipImages.Count < count)
        {
            var blipGo = new GameObject("trackedBlip");
            blipGo.transform.SetParent(cameraFeedImage != null ? cameraFeedImage.transform : transform, false);
            var blipImage = blipGo.AddComponent<Image>();
            blipImage.sprite = EnsureSprite(null);
            blipImage.type = Image.Type.Simple;
            blipImage.raycastTarget = false;
            trackedBlipImages.Add(blipImage);
        }
    }

    private void SetupSwitchStaticImage()
    {
        if (switchStaticImage == null)
        {
            var staticGo = new GameObject("switchStaticImage");
            staticGo.transform.SetParent(transform, false);
            switchStaticImage = staticGo.AddComponent<Image>();
        }

        if (switchStaticImage == null)
            return;

        var rt = switchStaticImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        switchStaticImage.type = Image.Type.Simple;
        switchStaticImage.preserveAspect = false;
        switchStaticImage.color = Color.white;
        switchStaticImage.raycastTarget = false;
        switchStaticImage.enabled = false;
        switchStaticImage.transform.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
    }

    private void SetupRoomNameText()
    {
        if (roomNameText != null)
            return;

        var texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == "RoomName")
            {
                roomNameText = texts[i];
                break;
            }
        }
    }

    private void SetupCloseButton()
    {
        if (closeButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == "CloseButton")
                {
                    closeButton = buttons[i];
                    break;
                }
            }
        }

        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnCloseButtonClicked()
    {
        var monitor = MonitorController.Instance;
        if (monitor != null)
            monitor.CloseMonitor();
    }

    private void RefreshRoomName()
    {
        if (roomNameText == null)
            return;

        var monitor = MonitorController.Instance;
        roomNameText.text = monitor != null && monitor.IsMonitorOpen
            ? GetLocalizedRoomName(monitor.CurrentCameraRoomName)
            : string.Empty;
    }

    private static string GetLocalizedRoomName(string roomName)
    {
        switch (roomName)
        {
            case "Classroom":
                return "教室";
            case "Corridor1":
                return "走廊一";
            case "Corridor2":
                return "走廊二";
            case "Dorm":
                return "宿舍";
            case "Hall":
                return "大厅";
            case "Toilet":
                return "厕所";
            case "Guardroom":
                return "禁闭室";
            default:
                return roomName;
        }
    }

    private static Sprite EnsureSprite(Sprite sprite)
    {
        if (sprite != null)
            return sprite;

        if (fallbackWhiteSprite == null)
        {
            fallbackWhiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        return fallbackWhiteSprite;
    }
}
