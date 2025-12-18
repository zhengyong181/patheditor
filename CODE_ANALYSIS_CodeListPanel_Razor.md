# 代码分析报告: CodeListPanel.razor

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Components/CodeListPanel.razor`
- **命名空间**: `GCodeWorkbench.UI.Components`
- **作用**: 显示 G 代码列表，支持虚拟滚动、拖拽排序、参数编辑。

## 2. 变量与状态 (Variables & State)
- **参数**: `Document`, `SelectedLine`, `CommandManager`.
- **UI 状态**:
    - `_colLineWidth`, `_colInstructionWidth`: 列宽状态（支持拖拽调整）。
    - `_filterText`: 搜索过滤词。
    - `_draggedLine`: 拖拽操作的源对象。

## 3. 函数与逻辑 (Functions & Logic)
- **虚拟化 (`<Virtualize>`)**:
    - 使用 `Items="@Document.GetVisibleLines().ToList()"`。
    - **问题**: `GetVisibleLines()` 返回 `IEnumerable`，`.ToList()` 会立即分配内存并遍历所有元素。如果在 Render 循环中（例如由 `Main` 的仿真 Tick 触发），这将是巨大的性能灾难。
- **拖拽排序**:
    - `HandleDrop`: 计算索引，创建 `MoveItemCommand` 并执行。
    - 限制: 仅允许同级拖拽（`_draggedLine.Parent == targetLine.Parent`）。
- **参数编辑**:
    - 使用 `ParameterPill` 组件。
    - `UpdateParamWithCommand`: 创建 `SetPropertyCommand`，支持 Undo。

## 4. 功能实现 (Functionality)
提供了类似 IDE 的代码浏览体验，功能丰富且交互细节到位（如列宽调整、右键菜单）。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **组件**: `ParameterPill`, `ContextMenu`.
- **服务**: `CommandManager`.

## 6. 代码书写与风格 (Code Style)
- **Tailwind**: 大量使用了 Utility classes (`flex`, `overflow-auto`).
- **交互**: 实现了复杂的列宽调整逻辑（`StartResizing`, `HandleColumnResize`）。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **功能完备**: 拖拽、编辑、搜索、右键菜单一应俱全。
- **Undo 支持**: 所有修改操作都通过 CommandManager，这是一个非常专业的实现。

### 风险点 (Cons/Risks)
- **性能杀手 (GetVisibleLines)**:
    ```csharp
    <Virtualize Items="@Document.GetVisibleLines().ToList()" ...>
    ```
    每次父组件刷新（例如仿真进度更新），`CodeListPanel` 重新渲染。`GetVisibleLines().ToList()` 对于 10万行的代码，意味着每 16ms 分配一个包含 10万个对象的列表。这会导致极高的 GC 压力和 UI 卡顿。
- **Virtualize ItemProvider**: 当前用法没有利用 `ItemsProvider` 委托，而是直接传集合。

### 建议 (Suggestions)
1.  **[Critical] 避免 ToList**: `Main.razor` 的仿真更新不应触发布局组件的全量重绘。即使触发，`CodeListPanel` 也应使用 `ShouldRender` 来阻止不必要的刷新。
2.  **[High] 使用 ItemsProvider**: 改写 `<Virtualize>` 使用 `ItemsProviderDelegate`。仅当虚拟滚动视口变化时才去获取数据，而不是每次渲染都把整个列表传进去。
3.  **[Medium] 搜索过滤**: 当前搜索逻辑未完全实现（`_filterText` 绑定了但 `Virtualize` 的源数据没过滤）。应实现 `FilteredLines` 计算属性，并对其进行缓存。
