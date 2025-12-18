# 代码分析报告: GCodeDocument.cs

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Models/GCodeDocument.cs` (包含 `GCodeLine.cs` 的分析)
- **命名空间**: `GCodeWorkbench.UI.Models`
- **类名**: `GCodeDocument`, `GCodeLine`
- **作用**: 定义应用程序的核心数据模型。

## 2. GCodeDocument 结构
- **属性**:
    - `Lines`: `List<GCodeLine>`。由于支持层级（多段线），这实际上是一个树（森林）。
    - `SelectedIndex`: 扁平化索引。
    - `SelectedLine`: 通过 `GetFlatLines()` 动态计算。这可能是一个性能隐患。
- **方法**:
    - **`GetFlatLines()`**: **关键性能点**。
        - 逻辑：递归/遍历所有行及其子行，展平为一维列表。
        - 用途：Canvas 渲染、仿真计算都需要扁平列表。
        - **问题**: 每次调用都重新分配 `List<GCodeLine>` 并遍历。如果是高频调用（如在 Property Getter 中），性能开销巨大。
    - `GetBoundingBox()`: 遍历计算 X/Y 极值。
    - `GenerateGCode()`: 序列化回字符串。

## 3. GCodeLine 结构
- **属性**:
    - `Command`, `Type`: 核心指令信息。
    - `X, Y, Z, I, J, F, S`: 几何与工艺参数。使用 `double?` (Nullable) 是正确的设计，区分“未设置”和“0”。
    - **树状结构**:
        - `IsPolyline`: 标记位。
        - `Children`: 子行列表。
        - `Parent`: 父行引用。
    - **UI 状态**: `IsCollapsed`。

## 4. 功能实现 (Functionality)
模型设计能够很好地表达 G 代码的线性特性以及 DXF 导入带来的层级特性（多段线）。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **被引用**: 几乎所有 Service 和 Component。

## 6. 代码书写与风格 (Code Style)
- 使用 `INotifyPropertyChanged` 支持 WPF/Blazor 绑定。
- `DisplayCommand`, `DisplayParameters` 等计算属性方便了 UI 绑定。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **层级设计**: `GCodeLine` 支持嵌套，完美解决了 DXF Polyline 转换为 G 代码时的逻辑分组问题（一个 Polyline 对应多个 G1/G2 指令，且可以折叠显示）。

### 风险点 (Cons/Risks)
- **SelectedLine 性能**: `GCodeDocument.SelectedLine` 的 Getter 调用了 `GetFlatLines()`。
    ```csharp
    public GCodeLine? SelectedLine
    {
        get
        {
            if (_selectedIndex < 0) return null;
            var flatLines = GetFlatLines(); // <--- 昂贵的分配
            return _selectedIndex < flatLines.Count ? flatLines[_selectedIndex] : null;
        }
    }
    ```
    如果在渲染循环或 UI 刷新中多次访问 `SelectedLine`，会造成大量的内存分配和 CPU 浪费。

- **扁平化缓存**: 文档被多次读取（仿真、渲染、列表显示），每次都重新扁平化。

### 建议 (Suggestions)
1.  **[High] 缓存扁平列表**: 在 `GCodeDocument` 中维护一个 `List<GCodeLine> _cachedFlatLines`。仅在 `Lines` 集合发生变化（增删改）时标记 dirty 并重新生成。`SelectedLine` 直接访问缓存。
2.  **[Medium] 集合通知**: 使用 `ObservableCollection` 替代 `List`，或者在 Service 层统一管理增删改操作以触发缓存更新。
