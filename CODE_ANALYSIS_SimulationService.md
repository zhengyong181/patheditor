# 代码分析报告: SimulationService.cs

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Services/SimulationService.cs`
- **命名空间**: `GCodeWorkbench.UI.Services`
- **类名**: `SimulationService`
- **作用**: 负责 G 代码的运动仿真，计算时间轴，提供播放控制，并实时计算刀具位置。

## 2. 变量与状态 (Variables & State)
- **核心数据**: `List<MotionSegment> _segments`。将 G 代码行转换为带时间信息的运动段。
- **时间状态**: `_currentTime`, `_totalDuration`。
- **播放状态**: `_isPlaying`, `_speedMultiplier` (倍速)。
- **配置**: `RapidFeedRate` (默认 100mm/s)。因为 G00 通常不指定速度，必须由机器参数决定。

## 3. 函数与逻辑 (Functions & Logic)
- **`Load(GCodeDocument doc)`**:
    - **预计算**: 遍历文档，计算每一步的距离。
    - **时间计算**: `Duration = Distance / FeedRate`。
    - **累加**: 构建时间轴索引。
    - **Arc 处理**: 正确计算圆弧长度 (`AngleDiff * Radius`)。注意处理了 CW/CCW 的角度差计算（跨越 0/360 度的情况）。

- **`Tick(TimeSpan realTimeDelta)`**:
    - **逻辑**: 驱动仿真时钟。`_currentTime += realTimeDelta * _speedMultiplier`。
    - **循环**: 到达终点后自动暂停。
    - **通知**: 调用 `Notify()` 触发 UI 更新。

- **`Notify()`**:
    - **查找**: 遍历 `_segments` 找到包含当前时间的段。这里使用了简单的线性查找 `FirstOrDefault`。
    - **插值 (Lerp)**:
        - **直线**: 线性插值。
        - **圆弧**: 角度线性插值，再转换为笛卡尔坐标。这是正确的做法。
    - **事件**: `OnStateChanged?.Invoke(state)`。

## 4. 功能实现 (Functionality)
实现了基于物理时间的精确仿真，支持直线和圆弧插补。UI 可以绑定其进度和坐标。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **依赖**: `GCodeWorkbench.UI.Models.GCodeDocument`
- **被引用**: `GCodeWorkbench.UI.Components.Main.razor` (调用 Tick), `CanvasPanel.razor` (显示 Tool).

## 6. 代码书写与风格 (Code Style)
- **数学**: 插值逻辑清晰。
- **结构**: 标准的 Service 模式。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **精确性**: 正确处理了圆弧插补的数学逻辑。
- **解耦**: 仿真逻辑与 UI 渲染解耦，通过事件通信。

### 风险点 (Cons/Risks)
- **性能瓶颈 (查找)**: `Notify` 中使用 `_segments.FirstOrDefault(...)` 是 $O(N)$ 操作。在 `Tick` (60FPS) 中调用，且 `_segments` 可能有数万个，这将导致随着进度推移，性能线性下降。
    - *解释*: 对于 10万行的代码，播放到末尾时，每帧都要遍历 10万次列表。
- **GC 压力**: `Notify` 每一帧都分配一个新的 `SimulationState` 对象。

### 建议 (Suggestions)
1.  **[High] 查找优化**: 维护一个 `_currentSegmentIndex` 索引。因为时间是单调增加的，下一帧的段落大概率是当前段或下一段。直接从 `_currentSegmentIndex` 开始查找，将复杂度降为 $O(1)$。
2.  **[Medium] 内存优化**: 使用 `struct` 或重用同一个 `SimulationState` 对象来减少 GC。
3.  **[Low] 二分查找**: 如果支持随机跳转 (Seek)，应使用二分查找来定位时间段。
