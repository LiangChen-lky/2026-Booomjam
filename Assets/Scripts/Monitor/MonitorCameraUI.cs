using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 监控终端 UI 叠加层。
/// 负责信号丢失遮罩和 Z/X 切换输入。
/// 挂在场景中任意 GameObject 上即可。
/// </summary>
public class MonitorCameraUI : MonoBehaviour
{
    private static Sprite fallbackWhiteSprite;

    [SerializeField, Tooltip("信号丢失时的全屏遮罩（可选）")]
    private Image signalLostOverlay;

    [SerializeField, Tooltip("监控背景")]
    private Image ui_monitoring_bg;

    [SerializeField, Tooltip("录制红点指示器")]
    private Image ui_monitoring_rec;

    private void Awake()
    {
        SetupVisuals();
        Hide();
        if (signalLostOverlay != null) signalLostOverlay.enabled = false;
    }

    private void Update()
    {
        var monitor = MonitorController.Instance;
        if (monitor == null || !monitor.IsMonitorOpen) return;

        if (Input.GetKeyDown(KeyCode.Z))
            monitor.PrevCamera();
        if (Input.GetKeyDown(KeyCode.X))
            monitor.NextCamera();
    }

    /// <summary>
    /// 显示监控 UI 元素
    /// </summary>
    public void Show()
    {
        if (ui_monitoring_bg != null) ui_monitoring_bg.enabled = true;
        if (ui_monitoring_rec != null) ui_monitoring_rec.enabled = true;
    }

    /// <summary>
    /// 隐藏监控 UI 元素
    /// </summary>
    public void Hide()
    {
        if (ui_monitoring_bg != null) ui_monitoring_bg.enabled = false;
        if (ui_monitoring_rec != null) ui_monitoring_rec.enabled = false;
    }

    private void SetupVisuals()
    {
        if (ui_monitoring_bg != null)
        {
            ui_monitoring_bg.sprite = EnsureSprite(ui_monitoring_bg.sprite);
            ui_monitoring_bg.color = new Color(0f, 0f, 0f, 1f);
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
