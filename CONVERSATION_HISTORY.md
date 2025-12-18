# 项目对话历史记录 (Conversation History)

本文档总结了 `GCodeWorkbench` 项目开发过程中的关键对话、决策与变更。旨在帮助后续开发者快速理解项目演进脉络。

## 1. 早期阶段：基础架构搭建 (Phase 1-3)
**目标**: 建立一个基于 .NET 8 WPF + Blazor Hybrid 的 G代码编辑器与仿真器。

*   **架构选型**: 采用 WPF 作为宿主容器 (WebView2)，Blazor 处理前端 UI。这种架构结合了桌面的本地文件访问能力与 Web UI 的灵活性。
*   **核心功能实现**:
    *   **GCodeParser**: 实现了基础的 G代码文本解析（G0, G1, G2, G3）。
    *   **UI 组件**: 搭建了 `Main.razor`, `CodeListPanel` (代码列表), `CanvasPanel` (可视化画布), `EditorPanel` (文本编辑器)。
    *   **仿真引擎**: `SimulationService` 实现了基于时钟（Tick）的刀路插补仿真。

## 2. 中期阶段：项目持久化 (Phase 4)
**目标**: 支持项目的保存与加载。

*   **ProjectService**: 引入 `IProjectService` 接口与实现。
*   **数据结构**: 定义了 `ProjectState` 模型，使用 `System.Text.Json` 进行序列化。
*   **文件格式**: 确立了 `.dxfproj` 作为项目文件扩展名。
*   **功能**: 实现了新建、打开、保存、另存为以及导出 G代码的功能。UI 上增加了顶部菜单栏的响应逻辑。

## 3. 近期冲刺：DXF 解析与 PMAC 适配 (Phase 5)
**目标**: 解决工业现场实际遇到的 DXF 转换与特定控制器适配问题。

*   **PMAC 命令支持**:
    *   用户指出默认 G代码（G0/G1）不适用于 PMAC 控制器。
    *   **变更**: 引入 `CommandStyle` 枚举 (Standard/PmacNative)。
    *   **实现**: 适配了 `RAPID`, `LINEAR`, `ARC1` (顺时针), `ARC2` (逆时针) 指令格式。
    *   **配置**: 在 `DxfImportDialog` 中添加了 Header/Footer 的自定义模板支持。

*   **复杂几何处理 (netDxf)**:
    *   **多段线 (Polyline)**: 修复了 `LWPOLYLINE` 到 G代码的转换，特别是处理 **Bulge (凸度)** 属性，实现了多段线内的圆弧段解析。
    *   **圆弧精度**: 彻底重写了圆弧 (`ConvertArc`) 和圆 (`ConvertCircle`) 的转换逻辑，确保 I/J 圆心矢量计算正确。

*   **画布渲染修复**:
    *   **问题**: SVG 渲染器在接收到 `Setup` (G21/G90) 或 `RAPID` 指令时，路径出现“虚空连线”或圆弧半径归零错误。
    *   **修复**: 重构 `SvgRenderService`，引入“画笔位置追踪”机制。强制要求圆弧指令携带 `I, J` 参数传递给前端，解决了可视化变形问题。

## 4. 当前阶段：自定义原点与精准包围盒 (Phase 6)
**目标**: 允许用户自定义 DXF 导入后的坐标原点，并解决包围盒计算不准的问题。

*   **需求**: 用户需要将原点设置在：1. DXF 原始原点; 2. 首个图元起点; 3. 包围盒中心; 4. 包围盒左上角。
*   **初步实现**:
    *   在 `DxfImportOptions` 添加 `OriginType`。
    *   在 `DxfParserService` 中实现 `CalculateOriginOffset`。
    *   **Bug 修复 1**: 发现 UI 日志为空，排查发现 `DxfParserService` 中漏掉了 `CalculateOriginOffset` 的调用，已补全。
    *   **Bug 修复 2**: 解决了用户“找不到控制台”的问题，在 `Main.razor` 中集成了实时的 **LOG OUTPUT** UI 面板。

*   **算法优化**:
    *   **问题**: 用户反馈“包围盒中心”计算不准。
    *   **原因**: 原算法简单地使用 `Center +/- Radius` 作为圆弧包围盒，导致部分小圆弧段的包围盒被放大。
    *   **最终方案**: 实现了**精准包围盒算法**。
        *   对圆弧和多段线圆弧段，计算起点、终点。
        *   **关键**: 判断圆弧是否经过 0°, 90°, 180°, 270° 四个象限点（Cardinal Points），仅将经过的象限点纳入极值计算。
        *   验证通过：现在“包围盒左上角”和“形心”能精确对齐图形的几何边缘。

---

**总结**: 项目目前处于稳定可用状态。核心的 G代码解析与仿真功能完备，DXF 导入功能非常强大，支持复杂的几何变换与控制器方言适配。
