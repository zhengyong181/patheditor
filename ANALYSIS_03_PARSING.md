# 项目分析报告 03: 解析服务 (Parsing Services)

本报告深入分析 `GCodeWorkbench.UI/Services` 中负责数据导入与解析的核心服务。

## 1. GCodeParser.cs (G 代码文本解析)
负责将 `.nc`, `.gcode` 等文本文件解析为 `GCodeDocument`。

### 1.1. 解析逻辑
*   **正则表达式驱动**:
    *   `CommandRegex`: 匹配 `G01`, `M03`, `RAPID` 等主指令。
    *   `ParamRegex`: 匹配 `X100`, `Y-50.5`, `F2000` 等参数键值对。
    *   `CommentRegex`: 识别 `;` 或 `()` 格式的注释。
*   **流程**:
    1.  按行分割文本。
    2.  对每一行，先移除注释，提取纯代码。
    3.  匹配主指令 (`Command`)。
    4.  匹配所有参数，并赋值给 `GCodeLine` 的对应属性 (X, Y, Z, I, J, F, S)。
    5.  根据指令字符串判断 `GCodeType` (如 "G00" -> `GCodeType.Rapid`)。

### 1.2. 自动标签 (Auto Labeling)
*   `AutoAssignLabels` 方法会根据指令类型生成可读的描述。
*   例如：
    *   `G21` -> "Metric Units"
    *   `M03` -> "Spindle CW"
    *   `M30` -> "Program End"
*   这增强了 UI 列表中代码的可读性，即使不懂 G 代码的用户也能理解。

## 2. DxfParserService.cs (DXF 导入核心)
这是项目中逻辑最复杂的部分，负责将 CAD 几何图形转换为机器指令。使用 `netDxf` 库读取文件。

### 2.1. 原点偏移计算 (`CalculateOriginOffset`)
为了将 CAD 绘图放置在机器的工作区域内，必须计算偏移量 `_offsetX`, `_offsetY`。
*   **策略**:
    *   `DxfOriginal`: 偏移量为 (0,0)。
    *   `FirstEntityStart`: 找到第一个 Line/Polyline/Arc/Circle，取其起点/圆心取反作为偏移量。
    *   `BoundingBox...`: **难点**。需要精确计算所有图元的外接矩形。
*   **精确包围盒算法**:
    *   对于直线：直接比较起点和终点。
    *   对于 **圆弧 (Arc)** 和 **带凸度的多段线 (Polyline Bulge)**:
        *   不仅仅比较起点和终点。
        *   必须计算圆弧是否经过 0°, 90°, 180°, 270° (Cardinal Points) 象限点。如果经过，这些点可能是极值点，必须纳入包围盒计算。
        *   代码中 `UpdateArcBounds` 实现了这一逻辑，确保 `BoundingBoxTopLeft` 对齐绝对准确。

### 2.2. 几何转换与 Smart Move
*   **Smart Move (`MoveTo`)**:
    *   在绘制每一条线/圆弧之前，检查当前笔头位置 (`_lastX`, `_lastY`) 是否与目标起点一致。
    *   如果不一致，插入一条 `RAPID` (G00) 指令移动到起点。
    *   如果一致（误差 < 0.001），则省略移动指令，实现连续路径加工。

*   **Polyline (多段线) 处理**:
    *   DXF 的 `Polyline2D` 可能包含直线段和圆弧段（由 `Bulge` 凸度参数控制）。
    *   **Bulge 算法**:
        *   `Bulge = tan(angle/4)`。
        *   通过 Bulge 和弦长 (Chord) 计算圆弧半径、圆心位置、起始角、结束角。
        *   将 Bulge > 0 转换为 `GCodeType.ArcCCW` (G03)，Bulge < 0 转换为 `GCodeType.ArcCW` (G02)。
    *   生成的代码结构为一个 `Polyline` 父行，包含多个 `Linear`/`Arc` 子行。

*   **Circle/Arc 处理**:
    *   `ConvertCircle`: 转换为一个完整的 `G02` (CW) 圆。注意 DXF 圆心是绝对坐标，G 代码 I/J 也是相对圆心的增量（在此实现中 I=Radius, J=0 不完全准确，通常完整圆需要两段或特定 I/J 处理，代码中采用了 I=-R 的方式从右侧起刀）。
    *   `ConvertArc`: 转换为 `G03` (CCW)。netDxf 默认 Arc 都是 CCW。

### 2.3. 控制器适配
根据 `DxfImportOptions.Controller` 生成不同的指令风格：
*   **GCode 风格**: `G00`, `G01`, `G02`, `G03`。
*   **PMAC 风格**: `RAPID`, `LINEAR`, `ARC1` (CW), `ARC2` (CCW)。

---
*总结：解析模块不仅完成了基础转换，还处理了 CAD 导入中极具挑战性的坐标系对齐和复杂几何分解问题。*
