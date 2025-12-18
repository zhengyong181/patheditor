# 项目分析报告 06: 基础设施与服务 (Infrastructure & Services)

本报告分析 WPF 宿主环境、依赖注入配置以及底层服务实现。

## 1. WPF 宿主 (WPF Host)

### 1.1. MainWindow.xaml
这是传统的 WPF 窗口定义。
*   引入了 `Microsoft.AspNetCore.Components.WebView.Wpf` 命名空间。
*   核心控件: `<blazor:BlazorWebView>`。
    *   `HostPage="wwwroot/index.html"`: 指定入口 HTML。
    *   `Services="{DynamicResource services}"`: 注入 DI 容器。
    *   `<blazor:RootComponent Selector="#app" ComponentType="{x:Type ui:Main}" />`: 将 Blazor 的 `Main` 组件挂载到 HTML 中的 `#app` 节点。

### 1.2. 依赖注入 (Dependency Injection)
在 `MainWindow.xaml.cs` 中配置：
```csharp
var services = new ServiceCollection();
services.AddWpfBlazorWebView(); // 注册 WebView 必需服务
services.AddSingleton<IProjectService, WpfProjectService>(); // 注册 WPF 专用实现
services.AddSingleton<SvgRenderService>();
services.AddSingleton<CommandManager>();
// ...
Resources.Add("services", services.BuildServiceProvider());
```
这种设计实现了 UI (Blazor) 与平台 (WPF) 的解耦。如果未来移植到 MAUI 或 WebAssembly，只需替换 `IProjectService` 的实现即可。

## 2. WpfProjectService.cs (文件服务)
实现了 `GCodeWorkbench.UI.Services.IProjectService` 接口。
*   **平台特性**: 直接使用了 `Microsoft.Win32.OpenFileDialog` 和 `SaveFileDialog`。这在纯 Web (WASM) 环境中是无法做到的，体现了 Hybrid 应用的优势。
*   **功能**:
    *   `ImportGCodeAsync`: 读取文本文件 -> 调用 `GCodeParser`。
    *   `ImportDxfAsync`: 读取 DXF 文件 -> 调用 `DxfParserService`。
    *   `SaveProjectAsync`: 使用 `System.Text.Json` 将 `ProjectData` 序列化保存。
    *   `ExportGCodeAsync`: 将文档导出为纯文本 G 代码。

## 3. Web 资源 (wwwroot)
虽然是桌面应用，但 UI 资源遵循 Web 标准。
*   `index.html`:
    *   引入了 Tailwind CSS (CDN/Script 方式，仅用于原型，生产环境应编译)。
    *   引入了 Google Fonts (Spline Sans, Fira Code)。
    *   定义了 CSS 变量 `--grid-color`。
*   `css/app.css` & `GCodeWorkbench.UI.styles.css`: 组件样式隔离。
*   `js/canvasHelper.v2.js`: 核心 JS 互操作库，封装了 Canvas 的所有 DOM 操作。

## 4. 跨项目引用
*   `GCodeWorkbench` (Exe) 引用了 `GCodeWorkbench.UI` (Lib)。
*   `GCodeWorkbench.UI` 引用了 `netDxf` (NuGet 包)。
*   这种分层结构清晰地分离了“启动/宿主”与“业务/UI”。

---
*总结：基础设施层搭建了一个稳固的桥梁，连接了 .NET 强大的本地能力和 Blazor 灵活的 UI 能力。依赖注入的使用使得系统模块化程度高，易于测试和扩展。*
