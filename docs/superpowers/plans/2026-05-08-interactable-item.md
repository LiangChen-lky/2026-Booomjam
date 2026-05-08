# InteractableItem Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 创建 InteractableItem 组件，统一旅行袋和线索物品的交互逻辑，从 PlayerController 解耦

**Architecture:** InteractableItem 组件持有 UnityEvent<PlayerController>，PlayerController 通过 GetComponent 检测并触发事件。TravelBag 的拾取逻辑迁移到 TravelBag.cs 的公开方法。

**Tech Stack:** Unity 2022+, C#, UnityEvent

---

## 文件清单

| 文件 | 操作 | 职责 |
|---|---|---|
| `Assets/Scripts/Interaction/InteractableItem.cs` | 新建 | 一次性触发式交互组件 |
| `Assets/Scripts/Player/PlayerController.cs` | 修改 | 检测 InteractableItem，移除 travelBagTag 逻辑 |
| `Assets/Scripts/TravelBag.cs` | 修改 | 新增 PickupKey 公开方法 |

---

### Task 1: 创建 InteractableItem 组件

**Files:**
- Create: `Assets/Scripts/Interaction/InteractableItem.cs`

- [ ] **Step 1: 创建 InteractableItem.cs**

在 `Assets/Scripts/Interaction/` 下创建文件（与 Hideable.cs 同目录）：

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 一次性触发式交互物品（旅行袋、纸条、日记等）。
/// 玩家在范围内按交互键时触发 UnityEvent。
/// </summary>
public class InteractableItem : MonoBehaviour
{
    [SerializeField] private UnityEvent<PlayerController> onInteracted;

    public void OnInteracted(PlayerController player)
    {
        onInteracted?.Invoke(player);
    }
}
```

- [ ] **Step 2: 验证编译通过**

在 Unity Editor 中确认无编译错误。

---

### Task 2: 修改 PlayerController — 新增检测方法

**Files:**
- Modify: `Assets/Scripts/Player/PlayerController.cs`

- [ ] **Step 1: 添加 TryGetInteractableItemInRange 方法**

在 `TryGetHideableInRange` 方法之后（约 line 321）添加：

```csharp
public bool TryGetInteractableItemInRange(out InteractableItem item)
{
    item = null;
    if (interactionBox == null) return false;

    Vector2 center = interactionBox.transform.TransformPoint(interactionBox.offset);
    var lossy = interactionBox.transform.lossyScale;
    var size = new Vector2(
        interactionBox.size.x * Mathf.Abs(lossy.x),
        interactionBox.size.y * Mathf.Abs(lossy.y));
    float angle = interactionBox.transform.eulerAngles.z;

    int count = Physics2D.OverlapBoxNonAlloc(
        center, size * 0.5f, angle, interactionOverlapArray, ~0);

    for (int i = 0; i < count; i++)
    {
        var c = interactionOverlapArray[i];
        if (c == null) continue;
        if (c.attachedRigidbody == Rigidbody) continue;
        var ii = c.GetComponent<InteractableItem>();
        if (ii != null) { item = ii; return true; }
    }
    return false;
}
```

---

### Task 3: 修改 PlayerController — 更新交互逻辑

**Files:**
- Modify: `Assets/Scripts/Player/PlayerController.cs`

- [ ] **Step 1: 更新 OnInteractionStarted**

在 Hideable 分支之后、Door 分支之前插入 InteractableItem 分支：

```csharp
// 在 hideable 分支的 return; 之后添加：
if (TryGetInteractableItemInRange(out var item))
{
    item.OnInteracted(this);
    return;
}
```

移除原来的 travelBagTag 分支：

```csharp
// 删除这段：
if (TryGetObjectInRange(travelBagTag, out targetObject))
{
    InteractWithTravelBag(targetObject);
    return;
}
```

- [ ] **Step 2: 更新 UpdateInteractionIcon**

将 `UpdateInteractionIcon` 中的条件：

```csharp
|| TryGetObjectInRange(travelBagTag, out _)
```

替换为：

```csharp
|| TryGetInteractableItemInRange(out _)
```

- [ ] **Step 3: 移除 travelBagTag 字段**

删除：

```csharp
[SerializeField] private string travelBagTag = "TravelBag";
```

- [ ] **Step 4: 移除 InteractWithTravelBag 方法**

删除整个方法（约 line 323-342）：

```csharp
// 与旅行包交互
private void InteractWithTravelBag(GameObject travelBag)
{
    ...
}
```

- [ ] **Step 5: 验证编译通过**

确认无编译错误，无残留的 `travelBagTag` 或 `InteractWithTravelBag` 引用。

---

### Task 4: 修改 TravelBag.cs — 新增 PickupKey 方法

**Files:**
- Modify: `Assets/Scripts/TravelBag.cs`

- [ ] **Step 1: 添加 PickupKey 公开方法**

```csharp
/// <summary>
/// 拾取旅行袋中的钥匙。由 InteractableItem 的 onInteracted 事件调用。
/// </summary>
public void PickupKey(PlayerController player)
{
    var keyT = transform.Find("Key");
    if (keyT == null || !keyT.gameObject.activeSelf) return;
    keyT.gameObject.SetActive(false);
    player.AddKey();
}
```

- [ ] **Step 2: 验证编译通过**

确认无编译错误。

- [ ] **Step 3: 提交代码**

```bash
git add Assets/Scripts/Interaction/InteractableItem.cs Assets/Scripts/Player/PlayerController.cs Assets/Scripts/TravelBag.cs
git commit -m "Add InteractableItem component, migrate TravelBag interaction logic"
```

---

### Task 5: 场景配置（手动，Unity Editor 中操作）

- [ ] **Step 1: TravelBag 物体配置**

1. 在 Hierarchy 中找到 TravelBag 物体
2. Add Component → `InteractableItem`
3. Inspector 中 `onInteracted` → 点 `+` → 拖 TravelBag 自身到 Object 槽
4. 下拉选 `TravelBag.PickupKey (PlayerController)`

- [ ] **Step 2: 线索物品配置（纸条/日记）**

1. 创建新物体或选择已有物体
2. Add Component → `InteractableItem`
3. Inspector 中 `onInteracted` → 绑定显示 UI 的逻辑
   （如创建 `StoryPanel.Show(string)` 方法后绑定）

- [ ] **Step 3: 测试**

1. 运行游戏，走近 TravelBag → 出现交互图标
2. 按 E → 拾取钥匙（Key 子物体消失，KeyNumberText 更新）
3. 走近线索物品 → 出现交互图标
4. 按 E → 触发绑定的 UnityEvent
