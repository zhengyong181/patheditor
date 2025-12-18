namespace GCodeWorkbench.UI.Models;

/// <summary>
/// 表示一行 G 代码指令
/// </summary>
public class GCodeLine
{
    public int LineNumber { get; set; }
    public string RawText { get; set; } = "";
    public string Command { get; set; } = "";
    public GCodeType Type { get; set; } = GCodeType.Unknown;
    
    // 坐标参数
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Z { get; set; }
    public double? I { get; set; }
    public double? J { get; set; }
    public double? K { get; set; }
    public double? R { get; set; }
    public double? F { get; set; }  // 进给速度
    public double? S { get; set; }  // 主轴转速
    
    // 标签
    public List<string> Tags { get; set; } = new();
    public string Label { get; set; } = "";
    
    // 层级关系
    public bool IsPolyline { get; set; } = false;
    public bool IsCollapsed { get; set; } = true;
    public List<GCodeLine> Children { get; set; } = new();
    public GCodeLine? Parent { get; set; }
    
    // 用于 UI 显示
    public string DisplayCommand => !string.IsNullOrEmpty(Command) ? Command : (IsPolyline ? "[Polyline]" : "");
    public string DisplayParameters => GetParametersString();
    public string TypeLabel => Type.GetLabel();
    public string TypeColor => Type.GetColor();
    
    private string GetParametersString()
    {
        var parts = new List<string>();
        if (X.HasValue) parts.Add($"X{X.Value}");
        if (Y.HasValue) parts.Add($"Y{Y.Value}");
        if (Z.HasValue) parts.Add($"Z{Z.Value}");
        if (I.HasValue) parts.Add($"I{I.Value}");
        if (J.HasValue) parts.Add($"J{J.Value}");
        if (K.HasValue) parts.Add($"K{K.Value}");
        if (R.HasValue) parts.Add($"R{R.Value}");
        // Skip F for Setup type (F is already in Command like "F10.0")
        if (F.HasValue && Type != GCodeType.Setup) parts.Add($"F{F.Value}");
        if (S.HasValue) parts.Add($"S{S.Value}");
        return string.Join(" ", parts);
    }
    
    /// <summary>
    /// 获取包括子节点在内的所有行（扁平化）
    /// </summary>
    public IEnumerable<GCodeLine> GetAllLines()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var line in child.GetAllLines())
            {
                yield return line;
            }
        }
    }
}

/// <summary>
/// G 代码指令类型
/// </summary>
public enum GCodeType
{
    Unknown,
    Setup,      // 设置指令 (G20/G21/G90/G91)
    Rapid,      // 快速定位 (G00)
    Linear,     // 直线插补 (G01)
    ArcCW,      // 顺时针圆弧 (G02)
    ArcCCW,     // 逆时针圆弧 (G03)
    Dwell,      // 暂停 (G04)
    Spindle,    // 主轴指令 (M03/M04/M05)
    Coolant,    // 冷却液 (M07/M08/M09)
    ToolChange, // 换刀 (M06/T)
    Program,    // 程序控制 (M00/M01/M02/M30)
    Comment,    // 注释
    Polyline,   // 多段线（DXF 导入）
}

public static class GCodeTypeExtensions
{
    public static string GetLabel(this GCodeType type) => type switch
    {
        GCodeType.Setup => "Setup",
        GCodeType.Rapid => "Rapid",
        GCodeType.Linear => "Linear",
        GCodeType.ArcCW => "Arc CW",
        GCodeType.ArcCCW => "Arc CCW",
        GCodeType.Dwell => "Dwell",
        GCodeType.Spindle => "Spindle",
        GCodeType.Coolant => "Coolant",
        GCodeType.ToolChange => "Tool",
        GCodeType.Program => "Program",
        GCodeType.Comment => "Comment",
        GCodeType.Polyline => "Poly",
        _ => "Unknown"
    };
    
    public static string GetColor(this GCodeType type) => type switch
    {
        GCodeType.Setup => "slate",
        GCodeType.Rapid => "yellow",
        GCodeType.Linear => "green",
        GCodeType.ArcCW or GCodeType.ArcCCW => "cyan",
        GCodeType.Spindle => "primary",
        GCodeType.Coolant => "blue",
        GCodeType.ToolChange => "orange",
        GCodeType.Program => "purple",
        GCodeType.Comment => "gray",
        GCodeType.Polyline => "purple",
        _ => "slate"
    };
}
