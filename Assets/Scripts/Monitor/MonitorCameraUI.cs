using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 监控终端 UI 叠加层。
/// 负责信号丢失遮罩和 Z/X 切换输入。
/// 挂在场景中任意 GameObject 上即可。
/// </summary>
public class MonitorCameraUI : MonoBehaviour
{
    [SerializeField, Tooltip("信号丢失时的全屏遮罩（可选）")]
    private Image signalLostOverlay;

    [SerializeField, Tooltip("监控背景")]
    private Image ui_monitoring_bg;

    [SerializeField, Tooltip("录制红点指示器")]
    private Image ui_monitoring_rec;

    private void Start()
    {
        if (signalLostOverlay != null) signalLostOverlay.enabled = false;
        Hide();
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
}
