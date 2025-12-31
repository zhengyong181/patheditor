# 项目概览 (Project Overview)

`GCodeWorkbench` 是一个基于 .NET 8 技术的工业级 G代码编辑器与仿真工具。它专为 CNC/激光切割等场景设计，支持 G代码的编写、可视化仿真、以及从 DXF 文件的智能导入与转换。

## 1. 技术栈 (Tech Stack)

*   **框架**: .NET 8.0
*   **架构**: WPF (作为桌面宿主) + Blazor Hybrid (作为 UI 呈现层)
*   **前端**: Razor Components, HTML5 Canvas (用于路径渲染), TailwindCSS (样式)
*   **核心库**: 
    *   `netDxf` (2023.11.10): 用于解析 DXF CAD 文件。
    *   `Microsoft.AspNetCore.Components.WebView.Wpf`: 用于 Blazor Hybrid 宿主。

## 2. 项目结构 (Structure)

*   **GCodeWorkbench (WPF Host)**
    *   `MainWindow.xaml`: 主窗口容器，包含 `<BlazorWebView>`。
    *   `App.xaml`: 应用程序入口。
*   **GCodeWorkbench.UI (Blazor Library)**
    *   `Main.razor`: 核心业务组件，管理整个编辑器状态。
    *   **Services/**:
        *   `GCodeParser.cs`: 文本 -> G代码对象模型转换器。
        *   `DxfParserService.cs`: **核心业务**。负责将 DXF 实体转换为 G代码。包含复杂的几何算法（圆弧拟合、原点偏移计算、包围盒计算）。
        *   `SvgRenderService.cs`: 负责将 G代码转换为 SVG 路径字符串 (`d="..."`) 以供 Canvas 渲染。包含“断点续连”与智能路径优化逻辑。
        *   `SimulationService.cs`: 提供时间轴控制（播放/暂停/倍速），计算当前刀头位置 (Tool Position)。
        *   `ProjectService.cs`: 处理文件 I/O，支持 `.dxfproj` 项目文件的 JSON 序列化存储。
    *   **Components/**:
        *   `DxfImportDialog.razor`: DXF 导入配置弹窗（支持原点选择、缩放、控制器风格）。
        *   `CanvasPanel.razor`: 负责图形渲染与交互（缩放/平移）。
        *   `CodeListPanel.razor`: 显示 G代码行列表，支持高亮与滚动。
        *   `EditorPanel.razor`: 底部原始文本编辑器。
    *   **Models/**:
        *   `GCodeDocument/GCodeLine`: 核心数据模型。
        *   `DxfImportOptions`: 导入配置模型（含 `OriginType` 枚举）。

## 3. 核心功能特性 (Key Features)

### 3.1 G代码解析与仿真
*   支持标准 G代码 (G0, G1, G2, G3) 解析。
*   支持 `I/J` 圆心格式解析。
*   实时可视化仿真，显示当前执行行。

### 3.2 高级 DXF 导入 (Advanced DXF Import)
*   **控制器适配**: 支持标准 GCode 和 **PMAC** 控制器格式 (RAPID, LINEAR, ARC1, ARC2)。
*   **几何转换**:
    *   支持 `LINE`, `Polyline2D` (含 Bulge 圆弧段), `CIRCLE`, `ARC`。
    *   自动将 CW/CCW 圆弧转换为对应的 G2/G3 (或 ARC1/ARC2) 指令。
*   **原点自定义 (Origin Customization)**:
    *   `DxfOriginal`: 保持 CAD 原始坐标。
    *   `FirstEntityStart`: 对齐到首个图元的起点。
    *   `BoundingBoxCenter`: 对齐到几何形心。
    *   `BoundingBoxTopLeft`: 对齐到外接矩形左上角（**精准算法**：考虑圆弧象限点）。

### 3.3 调试与日志 (Debugging)
*   主界面集成 **LOG OUTPUT** 面板。
*   `DxfParserService` 暴露 `OnLog` 事件，实时输出导入过程中的计算细节（如包围盒范围、偏移量）。

## 4. 待办/未来优化方向 (Future Roadmap)

*   **路径优化**: 目前已实现基础的 Smart Move (G0)，未来可加入更复杂的“最短路径算法” (TSP)。
*   **3D 视图**: 目前仅支持 2D (X/Y) 视图，数据模型已预留 Z 轴字段，可扩展 3D 预览。
*   **编辑功能**: 目前以导入与查看为主，代码编辑功能较基础，可增强智能提示与语法检查。

---
*文档生成时间: 2025-12-18*
