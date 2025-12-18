# 项目分析报告 02: 数据模型 (Data Models)

本报告详细分析 `GCodeWorkbench.UI/Models` 命名空间下的核心数据结构。这些模型定义了应用程序的数据基础。

## 1. GCodeDocument.cs (文档根对象)
`GCodeDocument` 类是整个编辑器的核心数据容器，实现了 `INotifyPropertyChanged` 接口以支持 UI 数据绑定。

*   **主要属性**:
    *   `List<GCodeLine> Lines`: 存储 G 代码行的列表。这是一个树状结构的根列表（对于多段线，子行存储在 `GCodeLine` 内部）。
    *   `string FileName`: 当前文件名。
    *   `bool IsDirty`: 文档是否被修改过。
    *   `int SelectedIndex`: 当前选中行的扁平化索引。
    *   `GCodeLine? SelectedLine`: 这是一个计算属性，通过 `GetFlatLines()` 和索引获取当前选中的行对象。

*   **核心方法**:
    *   `GetFlatLines()`: 这是一个关键方法。由于 `GCodeLine` 可能包含子行（如 DXF 导入的多段线 `Polyline`），此方法将树状结构展平为一个 `List<GCodeLine>`，用于渲染列表和计算仿真路径。
    *   `GetVisibleLines()`: 类似 `GetFlatLines`，但考虑了 `IsCollapsed` 属性，用于 UI 列表的虚拟化渲染。
    *   `GetBoundingBox()`: 遍历所有行，计算 X/Y 坐标的最大最小值，并增加 10% 的 Padding，用于设置 Canvas 的初始视图范围。
    *   `GenerateGCode()`: 逆向操作，遍历所有行并将 `RawText` 或重组的命令拼接成字符串，用于保存文件。

## 2. GCodeLine.cs (代码行实体)
`GCodeLine` 表示单独的一行 G 代码或一个几何图元。

*   **基础属性**:
    *   `int LineNumber`: 行号。
    *   `string RawText`: 原始文本内容。
    *   `string Command`: 解析后的指令（如 "G01", "M03"）。
    *   `GCodeType Type`: 枚举类型，标识指令类别（见下文）。

*   **几何与参数属性**:
    *   `X, Y, Z`: 目标坐标。
    *   `I, J, K`: 圆弧圆心偏移量（通常用于 G02/G03）。
    *   `R`: 圆弧半径（备用格式）。
    *   `F`: 进给速度 (Feed Rate)。
    *   `S`: 主轴转速 (Spindle Speed)。

*   **层级结构 (用于 DXF 多段线)**:
    *   `bool IsPolyline`: 标识是否为多段线父节点。
    *   `List<GCodeLine> Children`: 存储多段线的各个线段（直线或圆弧）。
    *   `GCodeLine? Parent`: 指向父节点的引用。
    *   `bool IsCollapsed`: UI 折叠状态。

*   **UI 辅助属性**:
    *   `DisplayCommand`: UI 显示用的指令文本。
    *   `DisplayParameters`: 将 X,Y,Z 等参数格式化为字符串。
    *   `Tags`: 标签列表（如 "Rapid", "Arc CW"），由解析器自动生成或用户添加。

## 3. GCodeType (指令类型枚举)
定义了系统能够识别和处理的所有指令类型：
*   **运动类**: `Rapid` (G00), `Linear` (G01), `ArcCW` (G02), `ArcCCW` (G03)。
*   **控制类**: `Spindle` (M03-M05), `Coolant` (M07-M09), `ToolChange` (M06)。
*   **程序类**: `Setup` (G20/G21/G90), `Program` (M30/M02), `Dwell` (G04)。
*   **特殊类**: `Polyline` (DXF 导入的复合图元), `Comment`.
*   扩展方法 `GetColor()` 为不同类型的指令提供了默认的 TailwindCSS 颜色类名（如 Rapid 为 yellow, Linear 为 green）。

## 4. DxfImportOptions.cs (导入配置)
用于配置 `DxfParserService` 的行为。

*   **枚举**:
    *   `ControllerType`: `Pmac` (使用 RAPID/LINEAR 指令) 或 `Beckhoff` (标准 G 代码)。
    *   `CommandStyle`: `GCode` 或 `PmacNative`。
    *   `OriginType`: 决定原点 (0,0) 的位置。
        *   `DxfOriginal`: 保持 CAD 原始坐标。
        *   `FirstEntityStart`: 第一条线的起点设为 (0,0)。
        *   `BoundingBoxCenter`: 几何中心设为 (0,0)。
        *   `BoundingBoxTopLeft`: 左上角设为 (0,0)（常用）。

*   **参数**:
    *   `FeedRate`, `PlungeFeedRate`: 进给速度设置。
    *   `HeaderCode`, `FooterCode`: 用户自定义的程序头尾代码。
    *   `StartTrigger`, `StopTrigger`: 用于激光/切割头的开关指令（如 M03/M05）。

## 5. ProjectData.cs (序列化模型)
用于 `.gcmproj` 项目文件的 JSON 序列化。
*   `ProjectData`: 包含版本号、文件名、路径和 `List<GCodeLineData>`。
*   `GCodeLineData`: `GCodeLine` 的简化版（POCO），去除了运行时状态（如 Parent 引用），仅保留数据字段以便 JSON 存储。
*   提供扩展方法 `ToProjectData()` 和 `ToDocument()` 实现模型与 DTO 之间的转换。

---
*总结：数据模型设计清晰，特别是 `GCodeLine` 的层级设计巧妙地解决了 G 代码线性结构与 DXF 复杂几何（多段线）之间的矛盾。*
