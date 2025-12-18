# 代码分析报告: DxfParserService.cs

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Services/DxfParserService.cs`
- **命名空间**: `GCodeWorkbench.UI.Services`
- **类名**: `DxfParserService`
- **作用**: 核心业务服务。利用 `netDxf` 库读取 DXF 文件，运用几何算法将其转换为 G 代码路径。

## 2. 变量与状态 (Variables & State)
- **私有状态**:
    - `_lastX`, `_lastY`: 记录上一次生成的 G 代码坐标，用于判断是否需要插入 RAPID 移动（Smart Move）。
    - `_hasLastPos`: 标记 `_lastX/Y` 是否已初始化。
    - `_offsetX`, `_offsetY`: 坐标系原点偏移量。

## 3. 函数与逻辑 (Functions & Logic)
- **`LoadDxf(...)`**:
    - **流程**:
        1. 加载 DXF。
        2. 计算原点偏移 (`CalculateOriginOffset`)。
        3. 插入 Header 代码。
        4. 依次处理 `Lines`, `Polylines2D`, `Circles`, `Arcs`。
        5. 插入 Footer 代码。
    - **逻辑**: 这种分层处理确保了生成的 G 代码结构完整。

- **`CalculateOriginOffset(...)`**:
    - **核心算法**: 根据 `DxfImportOptions.Origin` 计算偏移。
    - **亮点**: 实现了**精确包围盒**算法。特别是对于圆弧（Arc）和带凸度多段线（Polyline Bulge），不仅计算起点终点，还计算了是否包含象限点（0, 90, 180, 270度），这是很多简易 CAM 软件容易忽略的细节。

- **`ConvertPolyline(...)`**:
    - **难点**: DXF 多段线由顶点 (`Vertex`) 和凸度 (`Bulge`) 组成。
    - **算法**:
        - `Bulge = 0`: 直线段，生成 `G01`。
        - `Bulge != 0`: 圆弧段。利用公式 `Bulge = tan(theta/4)` 反推圆弧参数（半径、圆心）。
    - **逻辑**: 计算出圆心后，生成 `G02/G03` 指令。注意这里 `I/J` 参数是相对起点的偏移，不受全局 `Offset` 影响，这在逻辑上是正确的。

- **`MoveTo(...)`**:
    - **Smart Move**: 比较当前图元起点与上一图元终点。如果距离 > Tolerance，插入 `G00` (Rapid)。否则直接接续 `G01`。这大大优化了加工路径。

## 4. 功能实现 (Functionality)
- 支持多种 DXF 实体 (Line, Polyline2D, Circle, Arc)。
- 支持两种控制器格式 (G-Code vs PMAC)。
- 提供灵活的坐标系对齐功能。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **依赖**:
    - `netDxf` (第三方库)
    - `GCodeWorkbench.UI.Models.DxfImportOptions`
    - `GCodeWorkbench.UI.Models.GCodeDocument`
- **被引用**:
    - `WpfProjectService.ImportDxfAsync`

## 6. 代码书写与风格 (Code Style)
- **几何计算**: 包含大量数学公式（三角函数、向量计算）。代码中虽有变量名（如 `chord`, `sagitta`），但缺乏详细的几何图示注释，维护难度较高。
- **日志**: 使用 `OnLog` 事件回调 UI，这是一个很好的解耦设计。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **算法严谨**: 包围盒和 Bulge 计算逻辑非常专业，考虑了边缘情况。
- **解耦**: 通过 `DxfImportOptions` 将配置分离，便于扩展。

### 风险点 (Cons/Risks)
- **实体支持有限**: 目前仅支持 2D 实体。如果 DXF 包含 `Spline` (样条曲线) 或 `Ellipse` (椭圆)，会被忽略。netDxf 虽支持读取，但本服务未实现对应的离散化算法（将其转为短线段）。
- **Z轴缺失**: 目前完全是 2D (XY) 转换，忽略了 DXF 中的 Elevation (Z) 或 3D 线段。
- **排序优化**: 代码按实体类型列表（Line list -> Poly list -> Arc list）顺序处理。这会导致极其低效的路径（跳来跳去）。没有实现 TSP (旅行商问题) 或最近邻路径优化。

### 建议 (Suggestions)
1.  **[High] 路径排序**: 实现一个简单的“最近邻”排序算法。将所有实体转换为统一的“路径段”对象，然后贪婪地连接最近的端点，减少空跑 (G00)。
2.  **[Medium] Spline 支持**: 增加对 `Spline` 的支持，通过插值将其转换为 `Polyline` 近似。
3.  **[Medium] 图层过滤**: DXF 通常包含很多杂项图层（标注、边框）。应增加按 Layer 过滤的功能。
