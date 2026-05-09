# 音效差距分析 & 功能补全实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 根据 SFX 枚举定义的 28 个音效，补全所有缺失功能和未接入的音效调用，实现游戏核心玩法闭环。

**Architecture:** 在现有 AudioManager + AudioConfig + 枚举体系上扩展。不新建音频架构，只添加新的 MonoBehaviour 脚本和在已有脚本中补充 `AudioManager.Instance.Play()` / `PlayAtPosition()` 调用。新增脚本遵循现有模式：单例/组件 + AudioManager 调用。

**Tech Stack:** Unity 2D, C#, A* Pathfinding, Cinemachine

---

## 文件变更清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `Assets/Scripts/GameManager.cs` | 新建 | 游戏状态管理：逃脱胜利 + 死亡重启 |
| `Assets/Scripts/Monitor/MonitorController.cs` | 新建 | 监控/安全摄像系统 |
| `Assets/Scripts/Monitor/MonitorCameraUI.cs` | 新建 | 监控画面 UI 渲染 |
| `Assets/Scripts/Interaction/GlassBreakable.cs` | 新建 | 可破碎玻璃组件 |
| `Assets/Scripts/Interaction/TutorialTrigger.cs` | 新建 | 教程触发区域 |
| `Assets/Scripts/Interaction/MainDoor.cs` | 新建 | 大门交互（集齐钥匙解锁） |
| `Assets/Scripts/Monster/MonsterController.cs` | 修改 | 补全 4 个怪物音效 |
| `Assets/Scripts/Camera/CameraRoomManager.cs` | 修改 | 环境音切换 |
| `Assets/Scripts/TravelBag.cs` | 修改 | 空书包音效 |
| `Assets/Scripts/MainMenu/MainMenuUI.cs` | 修改 | UI Hover/ConfirmExit 音效 |
| `Assets/Scripts/Interaction/StoryPanel.cs` | 修改 | 故事面板开关音效 |
| `Assets/Scripts/Player/PlayerController.cs` | 修改 | 受伤音效 + 生命值状态管理 |
| `Assets/Scripts/KeyManager.cs` | 修改 | 集齐钥匙时触发大门解锁 |

---

## Phase 1: 核心玩法闭环（P0）

### Task 1: 创建 GameManager — 游戏状态管理

**Files:**
- Create: `Assets/Scripts/GameManager.cs`

GameManager 管理游戏全局状态：逃脱胜利和玩家死亡。当前 `PlayerController.TakeDamage()` 播放 `GameOver` 音效后什么都不做，`KeyManager.HasAllKeys()` 存在但从未被调用。

- [ ] **Step 1: 创建 GameManager.cs**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 游戏状态管理：逃脱胜利、死亡重启。
/// 通过静态 Instance 访问，场景中需存在一个挂载此脚本的 GameObject。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏设置")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField, Tooltip("Game Over 后延迟重启时间（秒）")]
    private float gameOverDelay = 3f;
    [SerializeField, Tooltip("逃脱成功后延迟返回主菜单时间（秒）")]
    private float escapeDelay = 5f;

    private bool isGameOver = false;
    private bool isEscaped = false;

    public bool IsGameOver => isGameOver;
    public bool IsEscaped => isEscaped;

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
    }

    /// <summary>
    /// 玩家死亡时由 PlayerController 调用。
    /// </summary>
    public void OnPlayerDeath()
    {
        if (isGameOver || isEscaped) return;
        isGameOver = true;

        // 停止 BGM，播放 Game Over 音效
        AudioManager.Instance.StopBGM();
        AudioManager.Instance.StopAmbient();
        AudioManager.Instance.Play(SFX.GameOver);

        // 禁用玩家输入
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.Input.DisablePlayerMoveInput();

        // 延迟重启
        StartCoroutine(RestartAfterDelay());
    }

    /// <summary>
    /// 集齐钥匙后由 MainDoor 调用，触发逃脱。
    /// </summary>
    public void OnPlayerEscape()
    {
        if (isGameOver || isEscaped) return;
        isEscaped = true;

        // 停止探索 BGM，播放逃脱成功 BGM
        AudioManager.Instance.StopBGM();
        AudioManager.Instance.StopAmbient();
        AudioManager.Instance.PlayBGM(BGM.EscapeSuccess);

        // 禁用玩家输入
        var player = FindObjectOfType<PlayerController>();
        if (player != null) player.Input.DisablePlayerMoveInput();

        // 延迟返回主菜单
        StartCoroutine(ReturnToMenuAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSeconds(escapeDelay);
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
```

- [ ] **Step 2: 验证编译**

在 Unity Editor 中确认无编译错误。

---

### Task 2: 修改 PlayerController — 接入 GameManager 死亡逻辑

**Files:**
- Modify: `Assets/Scripts/Player/PlayerController.cs:120-135`

当前 `TakeDamage()` 在 HP≤0 时只播放 GameOver 音效，没有死亡状态管理。需要调用 `GameManager.OnPlayerDeath()`。

- [ ] **Step 1: 修改 TakeDamage 方法**

将 `PlayerController.cs:120-135` 的 `TakeDamage` 方法替换为：

```csharp
public void TakeDamage(int damage = 1)
{
    damage = Mathf.Clamp(damage, 0, CurrentHealth);
    CurrentHealth -= damage;
    HealthBar.value *= (float)CurrentHealth / Data.MaxHealth;

    if (CurrentHealth <= 0)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDeath();
        else
            AudioManager.Instance.Play(SFX.GameOver);
    }
}
```

- [ ] **Step 2: 验证编译**

---

### Task 3: 创建 MainDoor — 大门解锁交互

**Files:**
- Create: `Assets/Scripts/Interaction/MainDoor.cs`

大门是逃脱出口。玩家集齐 3 把钥匙后按交互键解锁，播放 `MainDoorUnlock` 音效，触发 `GameManager.OnPlayerEscape()`。

- [ ] **Step 1: 创建 MainDoor.cs**

```csharp
using UnityEngine;

/// <summary>
/// 大门交互组件。玩家集齐钥匙后按交互键解锁并逃脱。
/// 挂载在大门 GameObject 上，需配合 InteractableItem 使用。
/// </summary>
public class MainDoor : MonoBehaviour
{
    [SerializeField] private SpriteRenderer doorSprite;
    [SerializeField, Range(0f, 1f)] private float unlockedAlpha = 0.5f;

    private bool isUnlocked = false;

    /// <summary>
    /// 由 InteractableItem 的 onInteracted 事件调用。
    /// </summary>
    public void TryUnlock(PlayerController player)
    {
        if (isUnlocked) return;

        var keyManager = FindObjectOfType<KeyManager>();
        if (keyManager == null || !keyManager.HasAllKeys())
        {
            // 钥匙不足，播放锁住提示音（可选：新增 DoorLocked SFX）
            return;
        }

        isUnlocked = true;

        // 播放解锁音效
        AudioManager.Instance.Play(SFX.MainDoorUnlock);

        // 视觉反馈：门变半透明
        if (doorSprite != null)
        {
            Color c = doorSprite.color;
            c.a = unlockedAlpha;
            doorSprite.color = c;
        }

        // 触发逃脱
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerEscape();
    }
}
```

- [ ] **Step 2: 验证编译**

---

### Task 4: 修改 KeyManager — 集齐钥匙时通知 UI

**Files:**
- Modify: `Assets/Scripts/KeyManager.cs`

当前 `HasAllKeys()` 存在但从未被调用。需要确保 `CollectKey()` 中集齐钥匙时有视觉/音效反馈。

- [ ] **Step 1: 修改 CollectKey 方法**

将 `KeyManager.cs:20-25` 的 `CollectKey` 方法替换为：

```csharp
public void CollectKey()
{
    keysCollected++;
    UpdateKeyUI();
    AudioManager.Instance.Play(SFX.KeyFound);

    if (HasAllKeys())
    {
        // 所有钥匙已收集，可以解锁大门
        // 大门交互逻辑由 MainDoor.TryUnlock 处理
        Debug.Log("[KeyManager] 已集齐全部钥匙！前往大门逃脱！");
    }
}
```

- [ ] **Step 2: 验证编译**

---

## Phase 2: 怪物音效补全（P1）

### Task 5: 修改 MonsterController — 补全 4 个怪物音效

**Files:**
- Modify: `Assets/Scripts/Monster/MonsterController.cs`

当前怪物只播放 `DoorOpen` 和 `PlayerHit`，缺少 4 个已定义的怪物音效。需要在状态切换和行为中添加音效调用。

- [ ] **Step 1: 添加音效配置字段**

在 `MonsterController.cs:29` 的 `Header("BGM 切换")` 之前添加：

```csharp
[Header("怪物音效")]
[SerializeField, Tooltip("脚步声播放间隔（秒）")]
private float monsterFootstepInterval = 0.5f;
[SerializeField, Tooltip("咆哮触发距离（进入追踪时）")]
private float growlDistance = 10f;
[SerializeField, Tooltip("追逐音效淡入时间")]
private float chaseFadeIn = 0.5f;

private float lastMonsterFootstepTime;
private bool chaseSoundPlaying = false;
```

- [ ] **Step 2: 修改 SwitchToTracking 添加咆哮和追逐音效**

将 `MonsterController.cs:116-123` 的 `SwitchToTracking` 方法替换为：

```csharp
private void SwitchToTracking()
{
    currentState = MonsterState.Tracking;
    hasWanderTarget = false;

    // 播放发现玩家咆哮音效
    AudioManager.Instance.PlayAtPosition(SFX.MonsterGrowl, transform.position);

    // 播放追逐音效（循环）
    AudioManager.Instance.PlayAtPosition(SFX.MonsterChase, transform.position);
    chaseSoundPlaying = true;
}
```

- [ ] **Step 3: 修改 SwitchToWandering 停止追逐音效**

将 `MonsterController.cs:125-134` 的 `SwitchToWandering` 方法替换为：

```csharp
private void SwitchToWandering()
{
    currentState = MonsterState.Wandering;
    hasWanderTarget = false;
    lastWanderTime = Time.time;
    SetNewWanderPoint();

    // 停止追逐音效
    chaseSoundPlaying = false;
}
```

- [ ] **Step 4: 在 TrackingUpdate 中添加脚步声**

在 `MonsterController.cs:136` 的 `TrackingUpdate()` 方法开头（`CheckPlayerState()` 之前）添加：

```csharp
// 追踪状态脚步声
if (Time.time - lastMonsterFootstepTime >= monsterFootstepInterval)
{
    lastMonsterFootstepTime = Time.time;
    AudioManager.Instance.PlayAtPosition(SFX.MonsterFootstep, transform.position);
}
```

- [ ] **Step 5: 在 WanderingUpdate 中添加脚步声**

在 `MonsterController.cs:173` 的 `WanderingUpdate()` 方法开头（`CheckPlayerState()` 之前）添加：

```csharp
// 漫游状态脚步声（间隔更长）
if (aiPath.velocity.sqrMagnitude > 0.1f && Time.time - lastMonsterFootstepTime >= monsterFootstepInterval * 1.5f)
{
    lastMonsterFootstepTime = Time.time;
    AudioManager.Instance.PlayAtPosition(SFX.MonsterFootstep, transform.position);
}
```

- [ ] **Step 6: 修改 OpenDoor 添加撞墙音效**

在 `MonsterController.cs:284` 的 `AudioManager.Instance.PlayAtPosition(SFX.DoorOpen, transform.position);` 之后添加：

```csharp
AudioManager.Instance.PlayAtPosition(SFX.MonsterWallHit, transform.position);
```

- [ ] **Step 7: 验证编译**

---

### Task 6: 修改 CameraRoomManager — 环境音切换

**Files:**
- Modify: `Assets/Scripts/Camera/CameraRoomManager.cs:51-67`

`PlayAmbient()` API 存在但从未被调用。需要在房间切换时自动播放对应的环境音。房间名与 `AmbientRoom` 枚举需要映射。

- [ ] **Step 1: 添加房间名到 AmbientRoom 映射**

在 `CameraRoomManager.cs:14` 的 `public RoomCamera[] roomCameras;` 之后添加：

```csharp
[Header("环境音映射")]
[Tooltip("房间名到环境音的映射。未映射的房间不播放环境音。")]
public RoomAmbientMapping[] ambientMappings;

[System.Serializable]
public class RoomAmbientMapping
{
    public string roomName;
    public AmbientRoom ambient;
}
```

- [ ] **Step 2: 修改 SwitchRoom 方法**

将 `CameraRoomManager.cs:51-67` 的 `SwitchRoom` 方法替换为：

```csharp
public void SwitchRoom(string newRoom)
{
    if (string.IsNullOrEmpty(newRoom) || newRoom == currentRoom) return;
    if (cameraMap == null || !cameraMap.ContainsKey(newRoom)) return;

    // 禁用所有房间相机
    foreach (var kvp in cameraMap)
    {
        kvp.Value.Priority = 0;
    }

    // 激活目标房间相机
    cameraMap[newRoom].Priority = 100;
    currentRoom = newRoom;

    // 切换环境音
    SwitchAmbientForRoom(newRoom);
}

private void SwitchAmbientForRoom(string roomName)
{
    if (ambientMappings == null) return;

    foreach (var mapping in ambientMappings)
    {
        if (mapping.roomName == roomName)
        {
            AudioManager.Instance.PlayAmbient(mapping.ambient);
            return;
        }
    }
}
```

- [ ] **Step 3: 验证编译**

---

## Phase 3: 新功能系统（P2）

### Task 7: 创建 MonitorController — 监控/安全摄像系统

**Files:**
- Create: `Assets/Scripts/Monitor/MonitorController.cs`
- Create: `Assets/Scripts/Monitor/MonitorCameraUI.cs`

5 个 Monitor SFX 就绪但无任何脚本。推断功能：玩家可以在特定位置打开监控终端，切换查看多个摄像头画面，观察怪物位置。有冷却和信号丢失机制。

- [ ] **Step 1: 创建 MonitorController.cs**

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// 监控终端交互组件。玩家在终端位置按交互键打开监控画面。
/// 支持多摄像头切换、冷却、信号丢失。
/// </summary>
public class MonitorController : MonoBehaviour
{
    [Header("监控设置")]
    [SerializeField] private Camera[] monitorCameras;
    [SerializeField, Tooltip("监控使用后冷却时间（秒）")]
    private float cooldownDuration = 5f;
    [SerializeField, Tooltip("信号丢失概率（0-1）")]
    private float signalLostChance = 0.1f;
    [SerializeField, Tooltip("信号恢复时间（秒）")]
    private float signalRecoverTime = 3f;

    private int currentCameraIndex = 0;
    private bool isMonitorOpen = false;
    private bool isOnCooldown = false;
    private bool isSignalLost = false;
    private float lastCloseTime = -999f;

    public bool IsMonitorOpen => isMonitorOpen;

    /// <summary>
    /// 由 InteractableItem 的 onInteracted 事件调用。
    /// </summary>
    public void ToggleMonitor(PlayerController player)
    {
        if (isMonitorOpen)
        {
            CloseMonitor(player);
        }
        else
        {
            OpenMonitor(player);
        }
    }

    private void OpenMonitor(PlayerController player)
    {
        if (isOnCooldown)
        {
            float remaining = cooldownDuration - (Time.time - lastCloseTime);
            Debug.Log($"[Monitor] 冷却中，剩余 {remaining:F1} 秒");
            return;
        }

        isMonitorOpen = true;
        AudioManager.Instance.Play(SFX.MonitorOpen);

        // 禁用玩家移动
        if (player != null) player.Input.DisablePlayerMoveInput();

        // 激活第一个摄像头
        SwitchToCamera(0);

        // 随机信号丢失
        if (Random.value < signalLostChance)
        {
            StartCoroutine(SignalLostCoroutine());
        }
    }

    private void CloseMonitor(PlayerController player)
    {
        isMonitorOpen = false;
        isSignalLost = false;
        AudioManager.Instance.Play(SFX.MonitorClose);

        // 禁用所有监控摄像头
        DisableAllCameras();

        // 恢复玩家移动
        if (player != null) player.Input.EnablePlayerMoveInput();

        // 开始冷却
        isOnCooldown = true;
        lastCloseTime = Time.time;
        StartCoroutine(CooldownCoroutine());
    }

    /// <summary>
    /// 切换到下一个摄像头（由玩家按键调用）。
    /// </summary>
    public void NextCamera()
    {
        if (!isMonitorOpen || isSignalLost) return;
        if (monitorCameras == null || monitorCameras.Length == 0) return;

        currentCameraIndex = (currentCameraIndex + 1) % monitorCameras.Length;
        SwitchToCamera(currentCameraIndex);
        AudioManager.Instance.Play(SFX.MonitorStatic);
    }

    /// <summary>
    /// 切换到上一个摄像头（由玩家按键调用）。
    /// </summary>
    public void PrevCamera()
    {
        if (!isMonitorOpen || isSignalLost) return;
        if (monitorCameras == null || monitorCameras.Length == 0) return;

        currentCameraIndex = (currentCameraIndex - 1 + monitorCameras.Length) % monitorCameras.Length;
        SwitchToCamera(currentCameraIndex);
        AudioManager.Instance.Play(SFX.MonitorStatic);
    }

    private void SwitchToCamera(int index)
    {
        DisableAllCameras();
        if (monitorCameras != null && index >= 0 && index < monitorCameras.Length)
        {
            if (monitorCameras[index] != null)
                monitorCameras[index].enabled = true;
        }
    }

    private void DisableAllCameras()
    {
        if (monitorCameras == null) return;
        foreach (var cam in monitorCameras)
        {
            if (cam != null) cam.enabled = false;
        }
    }

    private IEnumerator SignalLostCoroutine()
    {
        isSignalLost = true;
        AudioManager.Instance.Play(SFX.MonitorSignalLost);

        yield return new WaitForSeconds(signalRecoverTime);

        isSignalLost = false;
        // 恢复当前摄像头
        SwitchToCamera(currentCameraIndex);
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
        AudioManager.Instance.Play(SFX.MonitorCooldownDone);
    }
}
```

- [ ] **Step 2: 创建 MonitorCameraUI.cs**

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 监控画面 UI 叠加层。显示当前摄像头画面和信号丢失效果。
/// 挂载在 Canvas 下的 RawImage 上。
/// </summary>
public class MonitorCameraUI : MonoBehaviour
{
    [SerializeField] private MonitorController monitorController;
    [SerializeField] private RawImage monitorDisplay;
    [SerializeField] private Image signalLostOverlay;
    [SerializeField] private Color signalLostColor = new Color(0f, 0f, 0f, 0.8f);

    private void Start()
    {
        if (monitorDisplay != null) monitorDisplay.enabled = false;
        if (signalLostOverlay != null) signalLostOverlay.enabled = false;
    }

    private void Update()
    {
        if (monitorController == null) return;

        bool isOpen = monitorController.IsMonitorOpen;
        if (monitorDisplay != null) monitorDisplay.enabled = isOpen;
        if (signalLostOverlay != null) signalLostOverlay.enabled = isOpen;

        // 监控打开时接收玩家输入切换摄像头
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                monitorController.PrevCamera();
            if (Input.GetKeyDown(KeyCode.E))
                monitorController.NextCamera();
        }
    }
}
```

- [ ] **Step 3: 验证编译**

---

### Task 8: 创建 GlassBreakable — 可破碎玻璃

**Files:**
- Create: `Assets/Scripts/Interaction/GlassBreakable.cs`

`GlassBreakable` 组件挂载在可破碎的玻璃物体上。怪物经过或玩家互动时破碎，播放 `GlassBreak` 音效，禁用碰撞体和精灵。

- [ ] **Step 1: 创建 GlassBreakable.cs**

```csharp
using UnityEngine;

/// <summary>
/// 可破碎玻璃组件。怪物经过或玩家互动时破碎。
/// </summary>
public class GlassBreakable : MonoBehaviour
{
    [SerializeField] private SpriteRenderer glassSprite;
    [SerializeField] private Collider2D physicsCollider;
    [SerializeField, Tooltip("破碎后碎片粒子预制体（可选）")]
    private GameObject breakEffectPrefab;

    private bool isBroken = false;

    /// <summary>
    /// 破碎玻璃。由怪物碰撞或玩家互动调用。
    /// </summary>
    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        AudioManager.Instance.PlayAtPosition(SFX.GlassBreak, transform.position);

        // 禁用碰撞体
        if (physicsCollider != null) physicsCollider.enabled = false;

        // 隐藏玻璃精灵
        if (glassSprite != null) glassSprite.enabled = false;

        // 生成破碎特效
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// 怪物进入玻璃触发区域时调用。
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken) return;
        if (other.CompareTag("Monster"))
        {
            Break();
        }
    }
}
```

- [ ] **Step 2: 验证编译**

---

### Task 9: 创建 TutorialTrigger — 教程触发区域

**Files:**
- Create: `Assets/Scripts/Interaction/TutorialTrigger.cs`

玩家进入触发区域时显示教程提示，播放 `TutorialHint` 音效。使用 `StoryPanel` 复用已有的对话框 UI。

- [ ] **Step 1: 创建 TutorialTrigger.cs**

```csharp
using UnityEngine;

/// <summary>
/// 教程触发区域。玩家进入时显示操作提示。
/// 挂载在带有 Trigger Collider2D 的 GameObject 上。
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    [SerializeField, TextArea(2, 5)]
    private string hintText = "按 E 键与物体互动";
    [SerializeField] private bool showOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (showOnlyOnce && hasTriggered) return;

        hasTriggered = true;
        AudioManager.Instance.Play(SFX.TutorialHint);

        var player = other.GetComponent<PlayerController>();
        StoryPanel.Show(hintText, player);
    }
}
```

- [ ] **Step 2: 验证编译**

---

## Phase 4: 润色/体验（P3）

### Task 10: 修改 MainMenuUI — UI 音效补全

**Files:**
- Modify: `Assets/Scripts/MainMenu/MainMenuUI.cs`

当前只有 `UIClick` 音效。需要添加按钮悬停音效和退出确认逻辑。

- [ ] **Step 1: 添加 UIHover 音效**

在 `MainMenuUI.cs` 的 `Awake` 方法中（`exitButton.onClick.AddListener(OnExitGame);` 之后）添加按钮悬停事件：

```csharp
// 按钮悬停音效
var startTrigger = startButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
var startHoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
startHoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
startHoverEntry.callback.AddListener((_) => AudioManager.Instance.Play(SFX.UIHover));
startTrigger.triggers.Add(startHoverEntry);

var exitTrigger = exitButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
var exitHoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
exitHoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
exitHoverEntry.callback.AddListener((_) => AudioManager.Instance.Play(SFX.UIHover));
exitTrigger.triggers.Add(exitHoverEntry);
```

- [ ] **Step 2: 添加退出确认音效**

将 `OnExitGame` 方法替换为：

```csharp
void OnExitGame()
{
    AudioManager.Instance.Play(SFX.UIConfirmExit);
    Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
}
```

- [ ] **Step 3: 验证编译**

---

### Task 11: 修改 StoryPanel — 故事面板音效

**Files:**
- Modify: `Assets/Scripts/Interaction/StoryPanel.cs:95-112`

故事面板打开/关闭时完全没有音效。添加 `UIPopupOpen` 和 `UIPopupClose`。

- [ ] **Step 1: 修改 Show 方法**

将 `StoryPanel.cs:95-104` 的 `Show` 方法替换为：

```csharp
public static void Show(string text, PlayerController player)
{
    if (instance == null) return;
    instance.textComponent.text = text;
    instance.panelRoot.SetActive(true);
    instance.currentPlayer = player;
    instance.isShowing = true;
    instance.readyToClose = false;
    if (player != null) player.Input.DisablePlayerMoveInput();
    AudioManager.Instance.Play(SFX.UIPopupOpen);
}
```

- [ ] **Step 2: 修改 Hide 方法**

在 `StoryPanel.cs:106` 的 `Hide` 方法中，在 `panelRoot.SetActive(false);` 之前添加：

```csharp
AudioManager.Instance.Play(SFX.UIPopupClose);
```

- [ ] **Step 3: 验证编译**

---

### Task 12: 修改 TravelBag — 空书包音效

**Files:**
- Modify: `Assets/Scripts/TravelBag.cs:11-18`

当前搜索空书包时什么都不发生。添加 `EmptyBag` 音效反馈。

- [ ] **Step 1: 修改 PickupKey 方法**

将 `TravelBag.cs:11-18` 的 `PickupKey` 方法替换为：

```csharp
public void PickupKey(PlayerController player)
{
    var keyT = transform.Find("Key");
    if (keyT == null || !keyT.gameObject.activeSelf)
    {
        // 空书包
        AudioManager.Instance.Play(SFX.EmptyBag);
        return;
    }
    keyT.gameObject.SetActive(false);
    player.AddKey();
    AudioManager.Instance.Play(SFX.BagSearch);
}
```

- [ ] **Step 2: 验证编译**

---

## Phase 5: 验证 & 清理

### Task 13: 全面验证

- [ ] **Step 1: 检查所有新文件编译通过**

在 Unity Console 中确认无编译错误。

- [ ] **Step 2: 检查 AudioManager 调用完整性**

验证以下 SFX 枚举全部有对应的 `Play()` / `PlayAtPosition()` 调用：

| SFX | 调用位置 |
|-----|---------|
| `DoorOpen` | PlayerController, MonsterController |
| `DoorClose` | PlayerController |
| `KeyPickup` | Key |
| `MainDoorUnlock` | MainDoor (新) |
| `HideIn` | Hideable |
| `HideOut` | Hideable |
| `BagSearch` | TravelBag |
| `GlassBreak` | GlassBreakable (新) |
| `TutorialHint` | TutorialTrigger (新) |
| `PlayerFootstep` | PlayerController |
| `PlayerHit` | MonsterController |
| `MonsterFootstep` | MonsterController (新增) |
| `MonsterGrowl` | MonsterController (新增) |
| `MonsterWallHit` | MonsterController (新增) |
| `MonsterChase` | MonsterController (新增) |
| `GameOver` | GameManager (新) |
| `UIHover` | MainMenuUI (新增) |
| `UIClick` | MainMenuUI |
| `UIPopupOpen` | StoryPanel (新增) |
| `UIPopupClose` | StoryPanel (新增) |
| `UIConfirmExit` | MainMenuUI (新增) |
| `MonitorOpen` | MonitorController (新) |
| `MonitorClose` | MonitorController (新) |
| `MonitorCooldownDone` | MonitorController (新) |
| `MonitorStatic` | MonitorController (新) |
| `MonitorSignalLost` | MonitorController (新) |
| `KeyFound` | KeyManager |
| `EmptyBag` | TravelBag (新增) |

- [ ] **Step 3: 检查 BGM 枚举完整性**

| BGM | 调用位置 |
|-----|---------|
| `MainMenu` | MainMenuUI |
| `Exploration` | MainMenuUI, MonsterController |
| `MonsterNear` | MonsterController |
| `EscapeSuccess` | GameManager (新) |

- [ ] **Step 4: 检查 AmbientRoom 枚举完整性**

| AmbientRoom | 调用位置 |
|-------------|---------|
| `Hall` | CameraRoomManager (通过 Inspector 映射) |
| `Corridor` | CameraRoomManager (通过 Inspector 映射) |
| `Classroom` | CameraRoomManager (通过 Inspector 映射) |
| `Dorm` | CameraRoomManager (通过 Inspector 映射) |
| `Toilet` | CameraRoomManager (通过 Inspector 映射) |
| `AncientHouse` | CameraRoomManager (通过 Inspector 映射) |

---

## 音效枚举覆盖总结

实施完成后，28 个 SFX + 5 个 BGM + 7 个 AmbientRoom 将 **100% 覆盖**：

- **新建脚本 (6):** GameManager, MainDoor, MonitorController, MonitorCameraUI, GlassBreakable, TutorialTrigger
- **修改脚本 (7):** PlayerController, MonsterController, CameraRoomManager, TravelBag, MainMenuUI, StoryPanel, KeyManager
- **新增音效调用 (14):** MonsterFootstep, MonsterGrowl, MonsterWallHit, MonsterChase, MainDoorUnlock, GlassBreakable, TutorialHint, UIHover, UIPopupOpen, UIPopupClose, UIConfirmExit, MonitorOpen/Close/Static/SignalLost/CooldownDone, EmptyBag, GameOver (via GameManager)
