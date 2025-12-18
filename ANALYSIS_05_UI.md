# 项目分析报告 05: UI 组件与交互 (UI Components & Interaction)

本报告分析 `GCodeWorkbench.UI/Components` 中的关键 Blazor 组件及其与 JavaScript 的互操作。

## 1. Main.razor (主布局)
这是应用的根组件，负责协调各子模块。
*   **布局管理**: 使用 Flexbox 管理左右分栏。引入了可拖拽的 `Splitter` 组件，允许用户动态调整左侧代码列表和右侧画布的宽度比例。
*   **状态协调**:
    *   持有 `GCodeDocument` 实例。
    *   订阅 `SimulationService.OnStateChanged`，更新进度条和播放状态。
    *   处理文件导入/导出事件。
    *   **高亮同步**: 当仿真运行时，根据 `SimulationState.CurrentLine` 自动调用 `HandleLineSelected`，实现代码行与动画的同步高亮。

## 2. CanvasPanel.razor (画布面板)
最复杂的 UI 组件，负责图形显示。

### 2.1. 渲染策略
*   **静态层**: 使用 SVG `<path>` 渲染路径。利用 `vector-effect="non-scaling-stroke"` 确保在缩放时线条宽度保持不变（不会变粗或变细）。
*   **动态层**: 刀头 (`.tool-marker`) 是一个 HTML `div` 元素，而不是 SVG 元素。
    *   **原因**: 防止每次刀头移动都触发整个 Blazor 组件树的重渲染。
    *   **实现**: 刀头位置由 JS 直接控制 DOM `style.left/top`。

### 2.2. JS Interop (`canvasHelper.v2.js`)
为了实现流畅的 CAD 级交互（60FPS 缩放/平移），绕过了 Blazor 的事件机制，直接使用原生 JS 事件。
*   **ViewBox 管理**: JS 维护 `vbX, vbY, vbW, vbH` 状态。
*   **Pan (平移)**: 监听 `mousedown` / `mousemove`，计算鼠标位移并更新 viewBox。
*   **Zoom (缩放)**: 监听 `wheel` 事件。核心算法是**以鼠标为中心的缩放**——计算鼠标在 ViewBox 中的相对位置，缩放后修正原点，确保鼠标下的点保持不动。
*   **SetToolPosition**: JS 接收逻辑坐标 (X,Y)，结合当前的 ViewBox 变换矩阵，计算出屏幕像素坐标，并更新刀头 `div` 的位置。

## 3. CodeListPanel.razor (代码列表)
*   **虚拟化**: 使用 `<Virtualize>` 组件。这是处理成千上万行 G 代码而不卡顿的关键。它只渲染可视区域内的 DOM 节点。
*   **拖拽排序**: 实现了 HTML5 Drag & Drop API。
    *   `ondragstart`: 记录被拖拽的行。
    *   `ondrop`: 计算目标位置，调用 `CommandManager` 执行移动命令。
*   **行内编辑**: 使用 `ParameterPill` 组件。
    *   点击参数值变为 `<input>`。
    *   失去焦点或回车时提交修改，并推入 Undo 栈。

## 4. DxfImportDialog.razor
*   一个模态弹窗。
*   使用 Tailwind 的 `fixed inset-0` 遮罩层。
*   绑定 `DxfImportOptions` 对象。
*   实现了简单的预设逻辑：选择 "PMAC" 或 "Beckhoff" 控制器时，自动填充对应的 Header/Footer 和指令风格。

## 5. 其他组件
*   `ContextMenu`: 自定义右键菜单，支持动态定位。
*   `SimulationControls`: 浮动的播放控制条，包含进度条（`<input type="range">`）和倍速控制。
*   `Splitter`: 通用分割条，支持水平和垂直拖拽，通过 JS 回调通知父组件调整尺寸。

---
*总结：UI 层充分利用了 Blazor 的组件化优势，同时在性能瓶颈处（Canvas 交互、虚拟列表）采用了正确的技术选型（JS Interop, Virtualization），保证了应用的流畅度。*
