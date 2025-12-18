# 代码分析报告: WpfProjectService.cs

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench/Services/WpfProjectService.cs`
- **命名空间**: `GCodeWorkbench.Services`
- **类名**: `WpfProjectService`
- **作用**: 实现了 `IProjectService` 接口，提供特定于 Windows 桌面环境的文件 I/O 功能。

## 2. 变量与状态 (Variables & State)
- **依赖**:
    - `GCodeParser`: 用于导入 G 代码。
    - `DxfParserService`: 用于导入 DXF。
- **配置**:
    - `JsonSerializerOptions`: 配置了 JSON 格式化（缩进、大小写不敏感）。

## 3. 函数与逻辑 (Functions & Logic)
- **`OpenProjectAsync`, `ImportGCodeAsync`, `PickDxfFileAsync`, `SaveProjectAsAsync`**:
    - **逻辑**:
        1. 实例化 `Microsoft.Win32.OpenFileDialog` 或 `SaveFileDialog`。
        2. 设置 Filter（文件扩展名过滤）。
        3. 调用 `ShowDialog()` 阻塞等待用户操作。
        4. 如果返回 `true`，使用 `File.ReadAllTextAsync` 或 `WriteAllTextAsync` 进行 IO 操作。
        5. 调用解析器转换数据。
    - **异常处理**: 使用 `MessageBox.Show` 显示错误。

## 4. 功能实现 (Functionality)
通过 WPF 的原生对话框，提供了符合用户习惯的文件操作体验。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **依赖**: `GCodeWorkbench.UI.Services.IProjectService`
- **被引用**: `MainWindow.xaml.cs` (DI 注册)。

## 6. 代码书写与风格 (Code Style)
- 使用 `async/await` 处理文件 IO，避免阻塞 UI 线程。
- 典型的 Adapter 模式实现。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **原生体验**: 使用系统对话框比 Web 模拟的对话框体验更好。

### 风险点 (Cons/Risks)
- **UI 耦合**: 直接调用 `System.Windows.MessageBox.Show` 使得该服务与 UI 线程耦合。如果在非 UI 线程调用可能会有问题（虽然目前都在 UI 上下文）。
- **硬编码 Filter**: 文件扩展名过滤器硬编码在方法中。

### 建议 (Suggestions)
1.  **[Low] 抽象对话框**: 将 `MessageBox` 封装为 `IDialogService`，以便单元测试。
2.  **[Low] 配置化**: 将支持的文件扩展名列表提取到常量或配置文件中。
