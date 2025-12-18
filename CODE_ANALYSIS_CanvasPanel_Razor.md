# 代码分析报告: CanvasPanel.razor

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Components/CanvasPanel.razor`
- **命名空间**: `GCodeWorkbench.UI.Components`
- **作用**: 负责显示 G 代码的图形路径，处理鼠标交互（缩放/平移/点击选择）。

## 2. 变量与状态 (Variables & State)
- **参数 (Parameters)**:
    - `Document`: 数据源。
    - `SelectedLineIndex`: 选中行索引。
    - `ToolX`, `ToolY`: 仿真刀头位置。
    - `RefreshKey`: 强制刷新触发器。
- **内部状态**:
    - `_clickablePaths`: 存储用于交互的路径段列表。
    - `_rapidPath`, `_feedPath`: 渲染用的 SVG 路径字符串。
    - `_selectedPath`: 高亮选中行的路径。

## 3. 函数与逻辑 (Functions & Logic)
- **`OnParametersSet`**:
    - 检测 `Document` 或 `RefreshKey` 变化。
    - 调用 `SvgRenderService.GeneratePaths` 生成可视路径。
    - 调用 `SvgRenderService.GenerateClickablePaths` 生成交互层。
    - 计算 `BoundingBox` 并初始化 ViewBox。
    - **性能**: 这是一个昂贵的操作，正确地使用了 `_lastDocument` 检查来避免不必要的重算。
- **`GenerateSelectedPath`**:
    - 动态计算当前选中行的路径。如果是 Polyline，需要生成包含所有子段的路径。
- **`OnAfterRenderAsync`**:
    - 调用 JS `canvasHelper.init`。
    - 调用 JS `setToolPosition` 更新刀头。
    - **逻辑**: `ToolX/Y` 的更新完全交由 JS 处理，不触发 Blazor DOM 更新。这是高性能的关键。

## 4. 功能实现 (Functionality)
实现了高性能的 SVG 渲染和仿真显示。利用 JS 互操作解决了 Blazor 在高频动画下的性能问题。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **依赖**: `SvgRenderService`.
- **JS**: `canvasHelper.v2.js`.

## 6. 代码书写与风格 (Code Style)
- **SVG 结构**:
    - `<g transform="scale(1, -1)">`: 巧妙地解决了 CNC 坐标系 (Y向上) 与 SVG 坐标系 (Y向下) 的差异。
    - `vector-effect="non-scaling-stroke"`: 保证缩放时线宽不变。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **JS 互操作**: 刀头位置更新绕过了 Blazor 渲染树，设计非常优秀。
- **路径缓存**: `OnParametersSet` 中的缓存逻辑避免了死循环和过度计算。

### 风险点 (Cons/Risks)
- **Clickable Paths**: 再次强调，`@foreach (var segment in _clickablePaths)` 循环会生成大量 DOM。对于大文件（如 3D 浮雕加工代码），这将导致页面卡死。
- **Params 传递**: `ToolX` 和 `ToolY` 作为 Parameter 传递给组件，虽然组件内部没有用它们渲染 DOM（除了传给 JS），但父组件 `Main` 每次更新这些值都会触发 `CanvasPanel` 的 `OnParametersSet` 和 Render 流程。这其实浪费了 CPU。

### 建议 (Suggestions)
1.  **[High] 参数隔离**: 从 `Main.razor` 中移除 `ToolX/ToolY` 参数绑定。让 `CanvasPanel` 直接订阅 `SimulationService` 事件，或者创建一个轻量级的 `ToolMarker` 组件来专门处理这个高频更新。
2.  **[High] 虚拟化渲染**: 既然 `ClickablePaths` 导致 DOM 过多，考虑仅在 SVG 上层覆盖一个透明 Canvas 用于点击检测（通过数学计算点到线段距离），完全移除 `<path ... onclick>` 的方式。
