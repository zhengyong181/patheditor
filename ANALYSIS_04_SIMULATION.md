# 项目分析报告 04: 仿真与渲染 (Simulation & Rendering)

本报告分析如何将静态的 G 代码数据转化为可视化的图形和动态的仿真动画。

## 1. SvgRenderService.cs (SVG 路径生成)
负责将 `GCodeDocument` 转换为 HTML5 SVG `<path>` 元素的 `d` 属性字符串。

### 1.1. 路径分离
为了视觉区分，服务生成了两组路径：
*   **RapidPath**: 对应 `G00` 快速定位。通常用**黄色虚线**表示，代表刀具空跑。
*   **FeedPath**: 对应 `G01/G02/G03` 切削进给。通常用**青色/绿色实线**表示，代表实际加工路径。
*   **逻辑**: 遍历所有行，维护 `feedPenX/Y` 状态。如果在 Rapid 移动后开始切削，必须先在 `FeedPath` 中插入一个 `M` (MoveTo) 指令，确保切削路径是断开的（不连接 Rapid 的起点）。

### 1.2. SVG Arc 生成
SVG 的 `A` (Arc) 指令参数复杂：`A rx ry x-axis-rotation large-arc-flag sweep-flag x y`。
*   **参数计算**:
    *   `sweep-flag`: G03 (CCW) 为 1，G02 (CW) 为 0。
    *   `large-arc-flag`: 计算起始角和结束角的差值。如果绝对值 > 180度 (PI)，则设为 1，否则为 0。
*   **完整圆处理**: SVG 不支持起点等于终点的单条 Arc 指令画整圆。代码检测到整圆时，自动拆分为两个半圆弧绘制。

### 1.3. 交互层 (`GenerateClickablePaths`)
为了支持“点击图形高亮代码行”，除了可视化的路径外，还生成了一组**隐形路径** (`_clickablePaths`)。
*   每个 `PathSegment` 对应一行 G 代码。
*   包含独立的 `d` 路径数据和 `FlatIndex`。
*   在 UI 中，这些路径被绘制在顶层，透明度高但 stroke-width 较宽，便于鼠标捕捉点击事件。

## 2. SimulationService.cs (时间轴仿真)
负责计算仿真动画的状态。

### 2.1. MotionSegment (运动段)
`Load` 方法将 G 代码行转换为 `List<MotionSegment>`。
*   计算每一段的物理长度 (Distance)。
*   获取该段的进给速度 (Feed Rate)。如果未指定，回退到默认值。
*   **Duration = Distance / FeedRate**。
*   累加计算 `StartTime` 和 `TotalDuration`。

### 2.2. 实时插值 (`Tick`)
当仿真运行时，`Tick` 方法每 16ms 被调用一次。
*   根据 `CurrentTime` 查找当前的 `MotionSegment`。
*   计算段内进度 `t = (CurrentTime - StartTime) / Duration` (0.0 - 1.0)。
*   **位置计算**:
    *   **直线**: `Lerp(Start, End, t)`。
    *   **圆弧**: 使用角度插值。
        *   `CurrentAngle = StartAngle + (EndAngle - StartAngle) * t`。
        *   `X = CenterX + R * cos(CurrentAngle)`。
        *   `Y = CenterY + R * sin(CurrentAngle)`。
*   触发 `OnStateChanged` 事件，通知 UI 更新刀头位置。

## 3. CommandManager.cs (撤销/重做)
实现了标准的命令模式 (Command Pattern) 以支持 Undo/Redo。
*   **ICommand 接口**: 定义 `Execute` 和 `Undo`。
*   **SetPropertyCommand**: 通用泛型命令，用于修改单个属性（如 X, Y, F）。保存 `OldValue` 和 `NewValue`。
*   **MoveItemCommand**: 用于处理列表项的拖拽排序。
*   **栈管理**: 维护 `_undoStack` 和 `_redoStack`，并限制最大历史记录数量 (200步)。

---
*总结：仿真模块通过预计算时间片和实时插值实现了平滑的动画效果，SVG 生成逻辑处理了复杂的圆弧映射，确保了视觉与数据的一致性。*
