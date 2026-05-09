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

    private void Start()
    {
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
}
