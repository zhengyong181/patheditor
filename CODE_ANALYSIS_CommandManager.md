# 代码分析报告: CommandManager.cs

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Services/CommandManager.cs`
- **命名空间**: `GCodeWorkbench.UI.Services`
- **类名**: `CommandManager`
- **作用**: 实现命令模式 (Command Pattern)，管理应用程序的 Undo (撤销) 和 Redo (重做) 操作。

## 2. 变量与状态 (Variables & State)
- **栈**:
    - `_undoStack`: `Stack<ICommand>`，存储已执行的命令。
    - `_redoStack`: `Stack<ICommand>`，存储已撤销的命令。
- **配置**:
    - `_maxHistory`: 限制历史记录深度（默认 200），防止内存无限增长。

## 3. 函数与逻辑 (Functions & Logic)
- **`Execute(ICommand command)`**:
    - 执行新命令。
    - 入 `_undoStack`。
    - **清空 `_redoStack`**: 这是标准逻辑，一旦有新操作，重做历史即失效。
    - **TrimHistory**: 检查栈大小，移除最旧的记录。
    - 触发 `OnStateChanged`。

- **`Undo()` / `Redo()`**:
    - 标准的栈操作：Pop -> Command.Undo/Execute -> Push to other stack。

- **`TrimHistory()`**:
    - **逻辑**: 由于 `Stack<T>` 不支持直接移除底部元素，代码通过 `ToList()` -> `RemoveRange` -> `Clear` -> `Push` 重新构建栈。
    - **性能**: 这是一个 $O(N)$ 操作。虽然 $N=200$ 很小，但每次操作都触发内存分配。

## 4. 功能实现 (Functionality)
为编辑器提供了基本的回退功能，支持属性修改和列表项移动等操作（具体命令定义在 `ICommand.cs`）。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **依赖**: `GCodeWorkbench.UI.Commands.ICommand`
- **被引用**: `Main.razor` (UI 按钮绑定), `CodeListPanel.razor` (拖拽和参数修改).

## 6. 代码书写与风格 (Code Style)
- 简洁明了，实现了经典的命令模式。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **健壮性**: 限制了历史记录数量，防止内存泄漏。
- **解耦**: 通过 `ICommand` 接口，任何操作都可以封装为命令，易于扩展。

### 风险点 (Cons/Risks)
- **栈操作效率**: `TrimHistory` 的实现方式（List 转换）效率较低，虽然在 N=200 时无感，但不够优雅。
- **状态快照**: 当前的 `SetPropertyCommand` 仅保存了修改的值。如果命令涉及复杂对象的状态变更（如 Document 结构变化），简单的属性命令可能不够。目前仅用于简单的参数修改是够用的。

### 建议 (Suggestions)
1.  **[Low] 数据结构优化**: 使用 `LinkedList` 或 `Deque` (双端队列) 来实现固定大小的历史记录栈，避免 `TrimHistory` 中的重建开销。
2.  **[Medium] 事务支持**: 增加 `BeginTransaction` / `EndTransaction`，支持将多个小操作（如批量修改）合并为一个 Undo 步骤。
