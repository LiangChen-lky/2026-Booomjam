# 监控UI预制件设计文档

## 概述

将监控系统的 UI 部分抽取为预制件，支持复用和后续扩展。同时为未来支持两种监控模式（相机模式和图片模式）提供架构基础。

## 背景

当前监控系统由两个脚本组成：
- `MonitorController.cs`：核心逻辑，动态创建 Cinemachine 相机
- `MonitorCameraUI.cs`：输入处理和信号丢失遮罩

需要添加两个新 UI 元素：
- `ui_monitoring_bg`：监控背景
- `ui_monitoring_rec`：录制红点指示器

## 设计方案

### 方案选择

考虑了三种方案：
1. **保持现状 + 新增UI**：最简单但不可复用
2. **监控UI预制件**：UI 可复用，改动适中（**选定**）
3. **完整监控系统预制件**：完全独立但需要较多重构

选择方案2的原因：
- 改动适中，不会过度重构
- UI 可复用，便于维护
- 为后续切换两种模式提供了基础

### 预制件结构

创建 `Assets/Prefabs/UI/MonitorUI.prefab`：

```
MonitorUI (Canvas)
├── ui_monitoring_bg (Image) - 监控背景
├── ui_monitoring_rec (Image) - 录制红点
└── MonitorCameraUI (Script) - 输入处理 + UI控制
```

### 代码修改

#### MonitorCameraUI.cs - 扩展 UI 控制

```csharp
// 新增字段
[SerializeField] private Image ui_monitoring_bg;
[SerializeField] private Image ui_monitoring_rec;

// 新增方法
public void Show() { /* 显示所有 UI 元素 */ }
public void Hide() { /* 隐藏所有 UI 元素 */ }
```

#### MonitorController.cs - 添加预制件引用和模式切换

```csharp
// 新增字段
[SerializeField] private MonitorCameraUI monitorUIPrefab;
private MonitorCameraUI monitorUIInstance;

// 新增枚举
public enum MonitorMode { Camera, Image }
[SerializeField] private MonitorMode currentMode = MonitorMode.Camera;

// OpenMonitor() 中实例化并显示 UI
// CloseMonitor() 中隐藏 UI
```

### 两种监控模式支持

| 模式 | 实现方式 | 适用场景 |
|------|----------|----------|
| CameraMode | 现有 Cinemachine 相机俯视 | 动态场景、需要实时渲染 |
| ImageMode | 预渲染图片 + 切换逻辑 | 性能优化、静态场景 |

### 实施步骤

1. 创建 MonitorUI 预制件（UI 元素 + 脚本）
2. 修改 MonitorCameraUI.cs 添加 UI 引用和控制方法
3. 修改 MonitorController.cs 添加预制件引用和模式切换逻辑
4. 在场景中配置 MonitorController 引用预制件

## 影响范围

- 新增文件：`Assets/Prefabs/UI/MonitorUI.prefab`
- 修改文件：
  - `Assets/Scripts/Monitor/MonitorCameraUI.cs`
  - `Assets/Scripts/Monitor/MonitorController.cs`
- 场景配置：需要在场景中配置 MonitorController 的预制件引用

## 后续扩展

此设计为以下功能提供了扩展基础：
- 支持图片场景模式（ImageMode）
- 可在不同场景中复用监控 UI
- 便于添加新的 UI 元素或监控功能
