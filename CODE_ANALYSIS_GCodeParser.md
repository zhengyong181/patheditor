# 代码分析报告: GCodeParser.cs

## 1. 文件定义 (Definition)
- **文件路径**: `GCodeWorkbench.UI/Services/GCodeParser.cs`
- **命名空间**: `GCodeWorkbench.UI.Services`
- **类名**: `GCodeParser`
- **作用**: 负责将原始的 G 代码字符串解析为结构化的 `GCodeDocument` 对象模型。它是文本到对象的转换入口。

## 2. 变量与状态 (Variables & State)
- **静态正则 (Static Regex)**:
    - `CommandRegex`: `@"([GM]\d+|RAPID|LINEAR|ARC[12]|OPEN|CLOSE)"`
        - 用于提取指令部分。
        - **特点**: 兼容了标准 G 代码 (G0/G1) 和 PMAC 风格指令 (RAPID/LINEAR)。
    - `ParamRegex`: `@"([XYZIJKRFSTP])\s*(=?\s*-?\d+\.?\d*)"`
        - 用于提取参数。
        - **特点**: 支持可选的 `=` 号，支持负数和小数。
    - `CommentRegex`: `@"([;(].*$)"`
        - 用于识别注释。
        - **缺陷**: 简单的正则可能无法处理行中间的括号注释（如 `G1 X10 (comment) Y10`），目前逻辑是先剔除匹配到的部分。

## 3. 函数与逻辑 (Functions & Logic)
- **`Parse(string gcode)`**:
    - **逻辑**:
        1. 初始化 `GCodeDocument`。
        2. 按行分割字符串 (`\r\n`).
        3. 遍历每一行调用 `ParseLine`。
        4. 调用 `AutoAssignLabels` 进行后处理。
    - **优点**: 结构清晰，分步处理。

- **`ParseLine(string rawText, int lineNumber)`**:
    - **逻辑**:
        1. 预处理：移除注释。
        2. 空行检查：如果是纯注释行，返回 `Type=Comment` 的行对象。
        3. **指令提取**: 使用 `CommandRegex` 查找第一个匹配项作为 `Command`。
        4. **类型推断**: 调用 `DetermineType`。
        5. **参数提取**: 使用 `ParamRegex` 查找所有匹配项，通过 `switch` 赋值给 `GCodeLine` 的对应属性 (X,Y,Z...)。
    - **细节**: 使用 `double.TryParse` 保证数字转换的安全性。

- **`DetermineType(string command)`**:
    - **逻辑**: 使用 C# 8.0 `switch expression` 将字符串映射到 `GCodeType` 枚举。
    - **覆盖**: 涵盖了 G0-G3, G4, G20/21/90/91, M0-M30 等常用指令。

- **`AutoAssignLabels(GCodeDocument document)`**:
    - **逻辑**: 遍历生成的文档，为每一行添加 `Tags` 和 `Label`。
    - **作用**: 增强 UI 的可读性（例如将 "G00" 显示为 "Rapid Move"）。

## 4. 功能实现 (Functionality)
该类实现了从非结构化文本到强类型对象的转换。它具有一定的容错性（通过正则），并且支持两种不同的 G 代码方言（标准 ISO 和 PMAC）。

## 5. 结构与交叉引用 (Structure & Cross-References)
- **依赖**:
    - `GCodeWorkbench.UI.Models.GCodeDocument`
    - `GCodeWorkbench.UI.Models.GCodeLine`
    - `GCodeWorkbench.UI.Models.GCodeType`
- **被引用**:
    - `WpfProjectService.ImportGCodeAsync`: 在文件导入时调用。
    - `GCodeWorkbench.UI.Main.razor`: 可能在某些调试或测试逻辑中直接调用（虽然主要通过 Service）。

## 6. 代码书写与风格 (Code Style)
- **风格**: 现代 C# 风格，使用了 `switch expression` 和正则预编译。
- **清晰度**: 高。变量命名规范，职责单一。
- **错误处理**: 较弱。解析失败的行会被跳过或部分解析，没有显式的错误报告机制（如抛出异常或返回错误列表）。

## 7. 优化建议 (Optimization)

### 积极点 (Pros)
- **扩展性**: 通过修改正则和 `DetermineType` 可以轻松支持更多指令。
- **性能**: 对于中等大小文件（几万行），正则解析速度尚可。

### 风险点 (Cons/Risks)
- **正则性能**: `Regex` 在处理极长行或极其巨大的文件时可能会有性能瓶颈（回溯）。建议显式设置 `RegexOptions.Compiled` 或超时时间。
- **语法局限**: 不支持复杂的宏变量（如 `#100=10`）、流程控制（`IF/WHILE`）或表达式参数（`X[#1+10]`）。这限制了其作为工业级解析器的能力。
- **注释处理**: 当前逻辑 `CommentRegex.Replace(rawText, "")` 可能会错误地处理行内注释，因为它直接替换了匹配项，如果正则不够严谨（例如没有锚点），可能会有误伤。

### 建议 (Suggestions)
1.  **[High] 增强正则健壮性**: 确保注释正则只匹配行尾分号或括号内容，并测试行中注释的情况。
2.  **[Medium] 错误报告**: `Parse` 方法应返回一个包含 `Errors/Warnings` 的结果对象，而不仅仅是文档，以便 UI 提示用户无法解析的行。
3.  **[Low] 性能优化**: 对于百万行级文件，考虑使用 `ReadOnlySpan<char>` 的手写词法分析器替代正则。
