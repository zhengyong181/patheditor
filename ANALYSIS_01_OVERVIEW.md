# 项目分析报告 01: 概览与架构 (Overview & Architecture)

## 1. 项目简介 (Project Introduction)
`GCodeWorkbench` 是一个工业级 G 代码编辑器与仿真工具，旨在为 CNC 加工和激光切割等应用场景提供高效的代码查看、编辑、仿真以及 CAD (DXF) 文件导入功能。

## 2. 技术栈 (Technology Stack)
本项目采用了微软最新的混合桌面应用开发模式：
*   **开发框架**: .NET 8.0
*   **桌面宿主**: WPF (Windows Presentation Foundation)
*   **UI 框架**: Blazor Hybrid (通过 `Microsoft.AspNetCore.Components.WebView.Wpf`)
*   **前端技术**: Razor Components, HTML5, TailwindCSS
*   **核心依赖**:
    *   `netDxf` (2023.11.10): 用于解析 DXF CAD 文件。
    *   `Microsoft.Extensions.DependencyInjection`: 依赖注入容器。

## 3. 解决方案结构 (Solution Structure)
解决方案包含两个主要项目：

### 3.1. GCodeWorkbench (WPF Host)
这是应用程序的入口点和宿主容器。
*   **职责**:
    *   创建 Windows 窗口 (`MainWindow`).
    *   配置 Blazor WebView 环境。
    *   注册系统级服务（如文件读写对话框）。
    *   加载 `index.html` 作为 Web UI 的入口。
*   **关键文件**:
    *   `App.xaml` / `App.xaml.cs`: 应用程序生命周期管理。
    *   `MainWindow.xaml`: 包含 `<BlazorWebView>` 控件的主窗口。
    *   `MainWindow.xaml.cs`: 配置依赖注入 (DI) 容器，注册 `WpfProjectService`, `SvgRenderService`, `CommandManager` 等服务。
    *   `Services/WpfProjectService.cs`: 实现了 `IProjectService` 接口，利用 WPF 的 `OpenFileDialog` 和 `SaveFileDialog` 提供文件操作能力。

### 3.2. GCodeWorkbench.UI (Blazor Class Library)
这是核心业务逻辑和用户界面的所在库。它可以被视为一个独立的 Web 前端应用，但运行在本地上下文中。
*   **职责**:
    *   提供所有的 UI 组件 (Pages, Components)。
    *   实现 G 代码解析、DXF 转换、几何计算、仿真逻辑。
    *   管理应用状态。
*   **目录结构**:
    *   `Components/`: 所有 Razor UI 组件（如画布、代码列表、对话框）。
    *   `Models/`: 数据传输对象 (DTO) 和业务实体（如 `GCodeLine`, `GCodeDocument`）。
    *   `Services/`: 纯业务逻辑服务（不依赖 UI 框架）。
        *   `GCodeParser.cs`: 文本解析。
        *   `DxfParserService.cs`: DXF 到 G 代码的转换核心。
        *   `SimulationService.cs`: 时间轴与插值计算。
        *   `SvgRenderService.cs`: 可视化路径生成。
    *   `Commands/`: 命令模式实现 (Undo/Redo)。
    *   `Main.razor`: 根布局组件。

## 4. 核心工作流 (Core Workflows)
1.  **启动**: WPF 启动 `MainWindow` -> 初始化 Blazor WebView -> 加载 `wwwroot/index.html` -> 渲染 `Main.razor`。
2.  **文件导入**:
    *   用户点击导入 -> `Main.razor` 调用 `IProjectService`。
    *   `WpfProjectService` 打开原生文件对话框。
    *   读取文件内容 -> 调用 `DxfParserService` 或 `GCodeParser` 转换为 `GCodeDocument`。
    *   UI 更新并加载仿真数据。
3.  **仿真与渲染**:
    *   `SvgRenderService` 将 `GCodeDocument` 转换为 SVG `<path>` 字符串。
    *   `CanvasPanel.razor` 渲染 SVG。
    *   `SimulationService` 驱动时间轴，计算当前刀头位置 (X,Y)。
    *   JS Interop (`canvasHelper.v2.js`) 更新刀头在 Canvas 上的视觉位置（绕过 Blazor 渲染树以提高 60FPS 下的性能）。

## 5. UI 架构 (UI Architecture)
采用 **Blazor Hybrid** 允许直接使用 Web 生态系统的布局能力 (Flexbox/Grid, TailwindCSS) 来构建复杂的 IDE 界面，同时通过 .NET 直接调用本地逻辑，没有 HTTP 开销。

*   **布局**: Flexbox 布局，包含 Header, Left Panel (Code List), Main Content (Canvas), Bottom Panel (Editor)。
*   **交互**:
    *   JS 负责高性能的 Canvas 缩放/平移 (Pan/Zoom)。
    *   C# 负责业务逻辑和 DOM 结构生成。
*   **主题**: 支持深色/浅色模式切换（Tailwind `dark` 类）。

---
*本文件详细分析了 GCodeWorkbench 的整体架构设计。具体模块分析请参考后续文档。*
