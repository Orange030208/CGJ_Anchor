# 宣传片系统 - TrailerPromo 场景

## 概述

我创建了一个全新的宣传片系统，独立于现有的 `Gaming1_Trailer` 系统。这个系统位于 `Assets/Scenes/TrailerPromo.unity`，通过程序化控制镜头、UI 和时间轴来展示游戏的核心玩法。

## 核心文件

### 1. TrailerDirector.cs
**路径**: `Assets/Scripts/Trailer/TrailerDirector.cs`

主导演脚本，控制整个宣传片的流程：
- **镜头序列管理**: 定义了 9 个镜头（Shot），每个镜头有开始时间、持续时间和回调
- **时间轴控制**: 精确到 0.1 秒的时序控制
- **UI 协调**: 控制字幕卡片的显示/隐藏和淡入淡出动画
- **场景引用**: 连接主摄像机、Canvas、UI 管理器等

**镜头列表**:
1. **Logo Flash (0-2s)**: 黑屏淡入显示"今日开工"标题
2. **Studio Overview (2-7s)**: 工作室全景，展示三个房间，镜头缓慢推进
3. **Code Room Closeup (7-9.5s)**: 程序员房间特写，显示"程序 - 降低Bug"
4. **Art Room Closeup (9.5-12s)**: 美术房间特写，显示"美术 - 提升画面"
5. **Audio Room Closeup (12-14.5s)**: 音效房间特写，显示"音效 - 营造氛围"
6. **Shop Montage (14.5-22s)**: 商店系统，快速展示道具卡片
7. **Event Panel (22-26s)**: 事件面板展示
8. **Weekly Actions (26-30s)**: 每周行动和房间互动
9. **Settlement Screen (30-35s)**: 结算界面
10. **End Credits (35-40s)**: 片尾字幕"今日开工 - 48h Game Jam"

### 2. TrailerCameraController.cs
**路径**: `Assets/Scripts/Trailer/TrailerCameraController.cs`

摄像机控制组件，使用 DOTween 实现平滑镜头动画：
- `SetPositionImmediate()`: 立即设置位置（无动画）
- `SetOrthoSizeImmediate()`: 立即设置正交相机尺寸
- `MoveTo()`: 平滑移动到目标位置
- `ZoomTo()`: 平滑缩放
- `MoveAndZoom()`: 组合移动和缩放

默认使用 `Ease.InOutQuad` 缓动曲线，过渡时间 1 秒。

## 场景设置

### TrailerPromo.unity 结构

**根物体**:
- `Main Camera`: 主摄像机（正交，size=8.31）
- `Global Light 2D`: 2D 光照
- `Level`: 游戏关卡（包含 Rooms、Virtual Camera 等）
- `Canvas`: 游戏 UI Canvas（包含所有面板管理器）
- `EventSystem`: Unity 事件系统
- `TrailerDirector`: 导演脚本 + TrailerCameraController
- `TitleCardOverlay`: 字幕卡片 UI（独立的 Overlay Canvas）

### TitleCardOverlay 结构

Screen Space Overlay Canvas（sortingOrder=100）：
- `Background`: 半透明黑色遮罩（alpha=0.8）
- `TitleText`: 主标题文本（96pt，居中，白色）
- `SubtitleText`: 副标题文本（48pt，居中，浅灰色）

**字体**: 使用 `fusion-pixel-10px-monospaced-zh_hans SDF` 支持中文显示

### 禁用的组件

为了避免自动流程干扰：
- `BeginPanel`: 开始界面已禁用
- `GameFlowRunner`: 游戏流程控制器已禁用（原本会在 Awake 中启动游戏）

## 房间位置

程序化定义的关键位置（世界坐标）：
- **工作室全景**: `(-7.64, -2.78, -10)`
- **程序员房间**: `(-8.17, 0.37, -10)`
- **美术房间**: `(-12.19, -6.17, -10)`
- **音效房间**: `(-3.30, -6.49, -10)`

## 使用方法

### 播放宣传片

1. 在 Unity 中打开 `Assets/Scenes/TrailerPromo.unity`
2. 点击 Play 按钮
3. 宣传片将自动播放 40 秒

### 调整镜头

编辑 `TrailerDirector.cs` 中的 `SetupShots()` 方法：
- 修改 `startTime` 和 `duration` 调整时序
- 修改 `onEnter` 中的摄像机位置和尺寸
- 修改 `ShowTitleCard()` 的参数更改字幕内容

### 自定义房间位置

在 Inspector 中调整 `TrailerDirector` 组件的 Room Positions：
- `Studio Overview Pos`
- `Code Room Pos`
- `Art Room Pos`
- `Audio Room Pos`

## 技术特点

1. **时间轴驱动**: 基于协程的精确时序控制
2. **DOTween 动画**: 平滑的镜头过渡和 UI 动画
3. **模块化设计**: Shot 系统支持灵活添加/删除/重排镜头
4. **中文支持**: 使用项目中已有的像素字体资源
5. **非破坏性**: 不修改原始 Gaming1 场景，完全独立运行

## 已知问题

1. **EventSystem 警告**: 运行时出现"2 个 EventSystem"警告，但不影响播放
   - 原因：从 Gaming1 复制的 Level 包含嵌套的 EventSystem
   - 影响：仅警告，不影响功能

2. **UI 管理器交互**: 当前 Shop/Event/Week 面板的 `Open()` 方法被注释
   - 原因：需要 GameFlowRunner 提供数据
   - 待办：实现 mock 数据或直接激活 GameObject

## 下一步优化

1. **音频**: 添加 BGM 和音效（trailerBGM 字段已预留）
2. **UI 动画**: 实现商店卡片的快速切换动画
3. **过渡效果**: 添加镜头间的视觉过渡（淡入淡出、划屏等）
4. **帧捕获**: 实现 `Time.captureFramerate` 离线渲染
5. **导出视频**: 使用 Unity Recorder 或 ffmpeg 导出 mp4

## 文件清单

- `Assets/Scenes/TrailerPromo.unity` - 宣传片场景
- `Assets/Scripts/Trailer/TrailerDirector.cs` - 导演脚本
- `Assets/Scripts/Trailer/TrailerCameraController.cs` - 摄像机控制器
- `Assets/Resources/Fonts/fusion-pixel-10px-monospaced-zh_hans SDF.asset` - 中文字体

---

**创建日期**: 2026-10-12  
**总时长**: 40 秒  
**镜头数量**: 9 个  
**状态**: 可播放（基础功能完成）
