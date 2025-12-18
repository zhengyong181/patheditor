# 代码分析报告: canvasHelper.v2.js

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench/wwwroot/js/canvasHelper.v2.js`
- **作用**: 负责 Canvas (SVG) 的高性能交互操作（Pan/Zoom）和刀头位置更新。弥补 Blazor Server/Hybrid 在高频 UI 更新（60FPS）时的性能短板。

## 2. 变量与状态 (Variables & State)
- **`instances`**: 存储每个 Canvas 实例的状态（支持多文档/多 Tab）。
- **`state` 对象**:
    - `vbX, vbY, vbW, vbH`: 当前 SVG ViewBox。
    - `panning`: 拖拽标志位。
    - `panStart...`: 拖拽起始状态。
    - `toolX, toolY`: 刀头逻辑坐标。

## 3. 函数与逻辑 (Functions & Logic)
- **`init`**:
    - 绑定 DOM 事件 (`mousedown`, `mousemove`, `wheel`, `contextmenu`)。
    - 初始化 ViewBox。
- **交互逻辑**:
    - **Pan**: 鼠标拖拽。计算像素位移 (`pxDx`)，根据当前缩放比例 (`scale`) 转换为 ViewBox 单位，反向更新 `vbX/vbY`。
    - **Zoom**: 鼠标滚轮。
        - 核心算法：以鼠标指针为不动点。
        - 1. 计算鼠标在 SVG 元素中的相对位置 (0..1)。
        - 2. 计算鼠标在逻辑 ViewBox 中的坐标 (`mouseVbX`)。
        - 3. 缩放 ViewBox 宽高 (`newW, newH`)。
        - 4. 根据相对位置反推新的 `vbX`：`vbX = mouseVbX - relX * newW`。
        - 逻辑严密，手感自然。
- **`updateToolScreenPosition`**:
    - 将刀头的逻辑坐标 (G 代码坐标) 转换为屏幕像素坐标 (CSS `left/top`)。
    - 考虑了 SVG 的 `preserveAspectRatio="xMidYMid meet"` 带来的偏移 (`offsetX`, `offsetY`)。
    - 考虑了 SVG Y 轴翻转 (`scale(1, -1)`)。

## 4. 功能实现 (Functionality)
提供了类似 CAD 软件的流畅平移缩放体验，以及独立的刀头图层渲染。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **被引用**: `GCodeWorkbench.UI.Components.CanvasPanel.razor` 通过 `IJSRuntime` 调用。

## 6. 代码书写与风格 (Code Style)
- 使用原生 JS (ES5/ES6 混合)，无外部依赖。
- 包含了 `debug` 辅助函数。
- 所有的状态都封装在 `instances` 中，避免污染全局变量。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **性能**: 将高频的 ViewBox 更新和刀头移动放在 JS 端，避免了 Blazor 的 Diff 算法开销，实现了 60FPS 的平滑效果。
- **正确性**: 缩放算法正确处理了 SVG 的 Aspect Ratio 行为。

### 风险点 (Cons/Risks)
- **SVG DOM 性能**: 虽然 ViewBox 更新很快，但如果 SVG 内部有数万个 `<path>` 元素，浏览器重绘（Repaint/Composite）仍然会很慢。这是 SVG 技术的固有限制。

### 建议 (Suggestions)
1.  **[High] Canvas 渲染**: 对于极复杂的图纸（>10万图元），建议完全放弃 SVG，改用 HTML5 `<canvas>` (Context 2D) 或 WebGL 进行渲染。JS 代码需要大幅重构以支持 Canvas 绘图指令。
2.  **[Medium] 节流**: `mousemove` 事件处理 Pan 操作时非常频繁，虽然目前逻辑简单可能不卡，但建议加入 `requestAnimationFrame` 进行节流，确保与显示器刷新率同步。
