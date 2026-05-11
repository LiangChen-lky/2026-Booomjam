# 监控UI预制件实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将监控系统的 UI 部分抽取为预制件，添加 ui_monitoring_bg 和 ui_monitoring_rec，支持复用和后续扩展。

**Architecture:** 创建 MonitorUI 预制件包含 UI 元素，修改 MonitorCameraUI.cs 添加 UI 控制方法，修改 MonitorController.cs 添加预制件引用和模式切换逻辑。

**Tech Stack:** Unity, C#, Cinemachine, uGUI

---

## 文件结构

### 新增文件
- `Assets/Prefabs/UI/MonitorUI.prefab` - 监控UI预制件

### 修改文件
- `Assets/Scripts/Monitor/MonitorCameraUI.cs` - 添加 UI 引用和控制方法
- `Assets/Scripts/Monitor/MonitorController.cs` - 添加预制件引用和模式切换逻辑

---

## Task 1: 修改 MonitorCameraUI.cs 添加 UI 控制

**Files:**
- Modify: `Assets/Scripts/Monitor/MonitorCameraUI.cs`

- [ ] **Step 1: 添加 UI 引用字段**

在 MonitorCameraUI.cs 中添加新的序列化字段：

```csharp
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
```

- [ ] **Step 2: 验证代码编译**

在 Unity 编辑器中等待编译完成，确保没有语法错误。

- [ ] **Step 3: 提交更改**

```bash
git add Assets/Scripts/Monitor/MonitorCameraUI.cs
git commit -m "feat(monitor): add UI control methods to MonitorCameraUI"
```

---

## Task 2: 修改 MonitorController.cs 添加预制件引用和模式切换

**Files:**
- Modify: `Assets/Scripts/Monitor/MonitorController.cs`

- [ ] **Step 1: 添加监控模式枚举和预制件引用**

在 MonitorController.cs 中添加新的字段和枚举：

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

/// <summary>
/// 监控终端组件。按 R 键全局开关监控，按 Z/X 切换房间视角。
/// 自动根据 RoomBounds 碰撞体生成俯视相机，不依赖 CameraRoomManager.roomCameras。
/// 挂在场景中任意 GameObject 上即可。
/// </summary>
public class MonitorController : MonoBehaviour
{
    public static MonitorController Instance { get; private set; }

    /// <summary>
    /// 监控模式枚举
    /// </summary>
    public enum MonitorMode
    {
        Camera,  // 相机模式：使用 Cinemachine 虚拟相机
        Image    // 图片模式：使用预渲染图片
    }

    [Header("监控设置")]
    [SerializeField, Tooltip("监控相机高度（俯视距离）")]
    private float cameraHeight = 30f;
    [SerializeField, Tooltip("监控相机正交大小")]
    private float cameraOrthoSize = 12f;
    [SerializeField, Tooltip("监控使用后冷却时间（秒）")]
    private float cooldownDuration = 5f;
    [SerializeField, Tooltip("信号丢失概率（0-1）")]
    private float signalLostChance = 0.1f;
    [SerializeField, Tooltip("信号恢复时间（秒）")]
    private float signalRecoverTime = 3f;
    [SerializeField, Tooltip("监控模式下的相机 Priority")]
    private int monitorPriority = 200;
    [SerializeField, Tooltip("当前监控模式")]
    private MonitorMode currentMode = MonitorMode.Camera;

    [Header("UI 设置")]
    [SerializeField, Tooltip("监控 UI 预制件")]
    private MonitorCameraUI monitorUIPrefab;
    private MonitorCameraUI monitorUIInstance;

    private struct MonitorCam
    {
        public string roomName;
        public CinemachineVirtualCamera vcam;
    }

    private List<MonitorCam> monitorCameras = new List<MonitorCam>();
    private int currentCameraIndex = 0;
    private bool isMonitorOpen = false;
    private bool isOnCooldown = false;
    private bool isSignalLost = false;
    private float lastCloseTime = -999f;
    private string savedRoom;
    private PlayerController playerRef;
    private MonsterController monsterRef;
    private GameObject monitorCameraRoot;
    private CinemachineBrain brainRef;
    private CinemachineBlendDefinition savedBlend;
    private Coroutine restoreBlendCoroutine;

    public bool IsMonitorOpen => isMonitorOpen;
    public MonitorMode CurrentMode => currentMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        CleanupCameras();
        CleanupUI();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;

        if (isMonitorOpen)
            CloseMonitor();
        else
            OpenMonitor();
    }

    private void OpenMonitor()
    {
        if (isOnCooldown)
        {
            float remaining = cooldownDuration - (Time.unscaledTime - lastCloseTime);
            Debug.Log($"[Monitor] 冷却中，剩余 {remaining:F1} 秒");
            return;
        }

        // 根据模式选择不同的初始化方式
        if (currentMode == MonitorMode.Camera)
        {
            BuildCameras();

            if (monitorCameras.Count == 0)
            {
                Debug.LogWarning("[MonitorController] 没有找到房间边界 (RoomBounds_XXX)");
                return;
            }
        }

        isMonitorOpen = true;
        AudioManager.Instance.Play(SFX.MonitorOpen);

        // 保存当前房间
        var camRoomManager = FindObjectOfType<CameraRoomManager>();
        if (camRoomManager != null)
            savedRoom = camRoomManager.currentRoom;

        // 暂停怪物
        monsterRef = FindObjectOfType<MonsterController>();
        if (monsterRef != null) monsterRef.enabled = false;

        // 禁用玩家
        playerRef = FindObjectOfType<PlayerController>();
        if (playerRef != null) playerRef.Input.DisablePlayerMoveInput();

        // 隐藏玩家视觉遮罩
        SetVisionMaskEnabled(false);

        // 切换为瞬切模式
        brainRef = Camera.main.GetComponent<CinemachineBrain>();
        if (brainRef != null)
        {
            savedBlend = brainRef.m_DefaultBlend;
            brainRef.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
        }

        // 显示 UI
        ShowUI();

        if (currentMode == MonitorMode.Camera)
        {
            ShowCamera(0);

            if (Random.value < signalLostChance)
                StartCoroutine(SignalLostCoroutine());
        }
    }

    private void CloseMonitor()
    {
        isMonitorOpen = false;
        isSignalLost = false;
        AudioManager.Instance.Play(SFX.MonitorClose);

        // 隐藏所有监控相机
        if (currentMode == MonitorMode.Camera)
        {
            HideAllCameras();
        }

        // 隐藏 UI
        HideUI();

        // 恢复玩家视觉遮罩
        SetVisionMaskEnabled(true);

        // 关闭监控时强制瞬切回角色相机，避免沿用 monitor 的 blend 过渡
        if (brainRef != null)
        {
            brainRef.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
        }

        // 恢复原来房间视角
        var camRoomManager = FindObjectOfType<CameraRoomManager>();
        if (camRoomManager != null && !string.IsNullOrEmpty(savedRoom))
            camRoomManager.SwitchRoom(savedRoom);

        // 下一帧再恢复原本的混合模式，保证当前切换是瞬切
        if (brainRef != null)
        {
            if (restoreBlendCoroutine != null)
                StopCoroutine(restoreBlendCoroutine);
            restoreBlendCoroutine = StartCoroutine(RestoreBlendNextFrame(savedBlend));
        }

        // 恢复怪物
        if (monsterRef != null) monsterRef.enabled = true;
        monsterRef = null;

        // 恢复玩家
        if (playerRef != null) playerRef.Input.EnablePlayerMoveInput();
        playerRef = null;

        isOnCooldown = true;
        lastCloseTime = Time.unscaledTime;
        StartCoroutine(CooldownCoroutine());
    }

    private IEnumerator RestoreBlendNextFrame(CinemachineBlendDefinition blend)
    {
        yield return null;

        if (brainRef != null)
        {
            brainRef.m_DefaultBlend = blend;
        }

        restoreBlendCoroutine = null;
    }

    public void NextCamera()
    {
        if (!isMonitorOpen || isSignalLost) return;
        if (monitorCameras.Count == 0) return;

        currentCameraIndex = (currentCameraIndex + 1) % monitorCameras.Count;
        ShowCamera(currentCameraIndex);
        AudioManager.Instance.Play(SFX.MonitorStatic);
    }

    public void PrevCamera()
    {
        if (!isMonitorOpen || isSignalLost) return;
        if (monitorCameras.Count == 0) return;

        currentCameraIndex = (currentCameraIndex - 1 + monitorCameras.Count) % monitorCameras.Count;
        ShowCamera(currentCameraIndex);
        AudioManager.Instance.Play(SFX.MonitorStatic);
    }

    private void ShowCamera(int index)
    {
        for (int i = 0; i < monitorCameras.Count; i++)
        {
            var cam = monitorCameras[i];
            if (cam.vcam != null)
                cam.vcam.Priority = (i == index) ? monitorPriority : 0;
        }
    }

    private void HideAllCameras()
    {
        foreach (var cam in monitorCameras)
        {
            if (cam.vcam != null)
                cam.vcam.Priority = 0;
        }
    }

    private void SetVisionMaskEnabled(bool enabled)
    {
        var mask = FindObjectOfType<PlayerVisionMaskSystem>();
        if (mask == null) return;

        mask.SetForceHidden(!enabled);
    }

    private void BuildCameras()
    {
        // 每次打开时清理重建，确保和场景同步
        CleanupCameras();

        monitorCameraRoot = new GameObject("[MonitorCameras]");

        // 找到场景中所有 RoomBounds_XXX
        var roomBounds = FindObjectsOfType<PolygonCollider2D>();
        foreach (var col in roomBounds)
        {
            if (!col.name.StartsWith("RoomBounds_")) continue;

            string roomName = col.name.Replace("RoomBounds_", "");
            Vector2 center = col.bounds.center;
            Vector2 size = col.bounds.size;

            // 创建 CinemachineVirtualCamera
            var camGo = new GameObject($"MonitorCam_{roomName}");
            camGo.transform.SetParent(monitorCameraRoot.transform);
            camGo.transform.position = new Vector3(center.x, center.y, -cameraHeight);

            var vcam = camGo.AddComponent<CinemachineVirtualCamera>();
            vcam.Priority = 0;
            vcam.m_Lens.OrthographicSize = cameraOrthoSize;
            vcam.m_Lens.Orthographic = true;

            // 添加 Confiner2D 限制在房间边界内
            var confiner = camGo.AddComponent<CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = col;
            confiner.m_Damping = 0f;
            confiner.m_MaxWindowSize = 0;
            confiner.InvalidateCache();

            monitorCameras.Add(new MonitorCam
            {
                roomName = roomName,
                vcam = vcam
            });
        }

        // 按房间名排序，保证顺序一致
        monitorCameras.Sort((a, b) => string.Compare(a.roomName, b.roomName, System.StringComparison.Ordinal));
    }

    private void CleanupCameras()
    {
        if (monitorCameraRoot != null)
            Destroy(monitorCameraRoot);
        monitorCameras.Clear();
    }

    private void ShowUI()
    {
        if (monitorUIPrefab == null) return;

        if (monitorUIInstance == null)
        {
            monitorUIInstance = Instantiate(monitorUIPrefab);
        }

        monitorUIInstance.Show();
    }

    private void HideUI()
    {
        if (monitorUIInstance != null)
        {
            monitorUIInstance.Hide();
        }
    }

    private void CleanupUI()
    {
        if (monitorUIInstance != null)
        {
            Destroy(monitorUIInstance.gameObject);
            monitorUIInstance = null;
        }
    }

    private IEnumerator SignalLostCoroutine()
    {
        isSignalLost = true;
        AudioManager.Instance.Play(SFX.MonitorSignalLost);

        yield return new WaitForSecondsRealtime(signalRecoverTime);

        isSignalLost = false;
        ShowCamera(currentCameraIndex);
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSecondsRealtime(cooldownDuration);
        isOnCooldown = false;
        AudioManager.Instance.Play(SFX.MonitorCooldownDone);
    }
}
```

- [ ] **Step 2: 验证代码编译**

在 Unity 编辑器中等待编译完成，确保没有语法错误。

- [ ] **Step 3: 提交更改**

```bash
git add Assets/Scripts/Monitor/MonitorController.cs
git commit -m "feat(monitor): add prefab reference and mode switching to MonitorController"
```

---

## Task 3: 创建 MonitorUI 预制件

**Files:**
- Create: `Assets/Prefabs/UI/MonitorUI.prefab`

- [ ] **Step 1: 在 Unity 编辑器中创建预制件**

1. 在 Hierarchy 中创建 Canvas（如果场景中没有）
2. 在 Canvas 下创建空 GameObject，命名为 "MonitorUI"
3. 在 MonitorUI 下创建 Image，命名为 "ui_monitoring_bg"
4. 在 MonitorUI 下创建 Image，命名为 "ui_monitoring_rec"
5. 将 MonitorCameraUI 脚本挂载到 MonitorUI 上
6. 在 Inspector 中配置：
   - 将 ui_monitoring_bg 拖拽到 Signal Lost Overlay 字段
   - 将 ui_monitoring_bg 拖拽到 Ui Monitoring Bg 字段
   - 将 ui_monitoring_rec 拖拽到 Ui Monitoring Rec 字段
7. 将 MonitorUI 拖拽到 Assets/Prefabs/UI/ 目录创建预制件

- [ ] **Step 2: 配置 UI 元素**

配置 ui_monitoring_bg：
- Anchor: Stretch-Stretch
- Pivot: (0.5, 0.5)
- Size Delta: (0, 0) - 全屏覆盖
- Color: 黑色或深色半透明

配置 ui_monitoring_rec：
- Anchor: Top-Right 或其他合适位置
- Pivot: (0.5, 0.5)
- Size: 适当大小（如 20x20）
- Color: 红色
- 添加动画（可选）：闪烁效果

- [ ] **Step 3: 提交预制件**

```bash
git add Assets/Prefabs/UI/MonitorUI.prefab
git commit -m "feat(monitor): create MonitorUI prefab with bg and rec elements"
```

---

## Task 4: 配置场景中的 MonitorController

**Files:**
- Modify: 场景文件（SampleScene.scene 或其他使用监控的场景）

- [ ] **Step 1: 在场景中配置 MonitorController**

1. 在场景中找到 MonitorController 组件
2. 在 Inspector 中配置：
   - Monitor UI Prefab 字段：拖拽 Assets/Prefabs/UI/MonitorUI.prefab
   - Current Mode 字段：选择 Camera 或 Image

- [ ] **Step 2: 测试监控功能**

1. 运行游戏
2. 按 R 键打开监控
3. 验证：
   - ui_monitoring_bg 显示
   - ui_monitoring_rec 显示
   - 按 Z/X 键可以切换摄像头
   - 信号丢失效果正常
4. 按 R 键关闭监控
5. 验证：
   - UI 隐藏
   - 冷却时间正常

- [ ] **Step 3: 提交场景配置**

```bash
git add Assets/Scenes/
git commit -m "feat(monitor): configure MonitorController with UI prefab in scene"
```

---

## Task 5: 验证和清理

**Files:**
- Review: 所有修改的文件

- [ ] **Step 1: 代码审查**

检查所有修改的文件：
- MonitorCameraUI.cs：确保 UI 控制方法正确
- MonitorController.cs：确保预制件引用和模式切换逻辑正确
- MonitorUI.prefab：确保 UI 元素配置正确

- [ ] **Step 2: 功能测试**

完整测试监控功能：
1. 打开监控：R 键
2. 切换摄像头：Z/X 键
3. 信号丢失：随机触发
4. 关闭监控：R 键
5. 冷却时间：5 秒
6. 重复测试多次确保稳定性

- [ ] **Step 3: 最终提交**

```bash
git add -A
git commit -m "feat(monitor): complete MonitorUI prefab implementation"
```

---

## 后续扩展

此实施计划为以下功能提供了扩展基础：
- 支持图片场景模式（ImageMode）：需要在 Task 2 中添加 ImageMode 的具体实现
- 可在不同场景中复用监控 UI：直接引用 MonitorUI 预制件
- 便于添加新的 UI 元素或监控功能：在 MonitorUI 预制件中添加新的子元素
