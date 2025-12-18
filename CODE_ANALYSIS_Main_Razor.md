# 代码分析报告: Main.razor

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Main.razor`
- **命名空间**: `GCodeWorkbench.UI`
- **类名**: `Main` (Blazor Component)
- **作用**: 应用程序的根 UI 组件，负责布局管理和状态协调。

## 2. 变量与状态 (Variables & State)
- **核心模型**: `_document` (GCodeDocument).
- **仿真状态**: `_isPlaying`, `_simulationProgress`, `_toolX`, `_toolY`, `_currentTime`.
- **布局状态**: `_leftPanelWidth`, `_editorCollapsed`, `_canvasFlexBasis`.
- **服务引用**: 注入了 `IProjectService`, `SimulationService`, `CommandManager`.
- **计时器**: `_simTimer` (System.Threading.Timer) 用于驱动仿真 Loop。

## 3. 函数与逻辑 (Functions & Logic)
- **生命周期 (`OnInitialized`)**:
    - 订阅 `SimulationService.OnStateChanged`。
    - 订阅 `CommandManager.OnStateChanged`。
    - 启动 60FPS (16ms) 的仿真定时器 `_simTimer`。
- **事件处理**:
    - **`OnSimStateChanged`**:
        - 这是一个高频回调。
        - 使用 `InvokeAsync(StateHasChanged)` 确保在 UI 线程更新。
        - 包含一个逻辑：如果正在播放，自动高亮当前行 (`HandleLineSelected`)。这提供了很好的视觉反馈。
    - **文件操作**: `HandleImportDxf`, `HandleOpenProject` 等，调用 Service 并重置状态。
    - **布局调整**: `HandleLeftSplitterResize` 处理拖拽事件。

## 4. 功能实现 (Functionality)
作为“控制器”角色，将数据（Document）、逻辑（Services）和视图（Sub-Components）粘合在一起。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **子组件**:
    - `Header`, `CodeListPanel`, `Splitter`, `CanvasPanel`, `EditorPanel`, `DxfImportDialog`.
- **服务**: 几乎引用了所有 Service。

## 6. 代码书写与风格 (Code Style)
- **HTML/CSS**: 使用 Tailwind CSS (`flex`, `h-screen`) 构建响应式布局。
- **C#**: 逻辑清晰，事件回调命名规范 (`Handle...`)。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **布局灵活**: 实现了类似 VS Code 的可拖拽面板布局。
- **状态同步**: 仿真状态更新逻辑正确处理了线程上下文。

### 风险点 (Cons/Risks)
- **StateHasChanged 滥用**: `OnSimStateChanged` 每一帧 (16ms) 都调用 `StateHasChanged()`。这会触发布局中**所有**子组件的参数检查和潜在的重渲染。对于复杂的 `CodeListPanel`（即使有 Virtualize），这也可能导致 CPU 占用过高。
- **Timer 精度**: `System.Threading.Timer` 在 Windows 上精度约 15ms，但在负载高时不稳定。

### 建议 (Suggestions)
1.  **[High] 渲染优化**: 避免在根组件 `Main` 上调用 `StateHasChanged`。应该将仿真状态（Progress, ToolPos）直接传递给 `CanvasPanel` 和 `SimulationControls`，并让这些子组件单独刷新，或者是使用 `CascadingValue` 但控制刷新范围。
    - *优化方案*: 仅在 `CanvasPanel` 内部订阅仿真事件进行局部刷新（甚至仅通过 JS 更新刀头，完全不刷新 Blazor 组件）。
2.  **[Medium] 布局存储**: 将 `_leftPanelWidth` 等布局偏好保存到本地配置中，下次打开恢复。
