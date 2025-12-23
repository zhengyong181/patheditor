using System.Text.RegularExpressions;
using GCodeWorkbench.UI.Models;

namespace GCodeWorkbench.UI.Services;

/// <summary>
/// G 代码解析服务
/// </summary>
public class GCodeParser
{
    // 匹配 G/M 指令 或 PMAC 文本指令 (RAPID, LINEAR, ARC1, ARC2)
    private static readonly Regex CommandRegex = new(@"([GM]\d+|RAPID|LINEAR|ARC[12]|OPEN|CLOSE)", RegexOptions.IgnoreCase);
    
    // 匹配参数 (X, Y, Z, I, J, K, R, F, S, T, P, N)
    private static readonly Regex ParamRegex = new(@"([XYZIJKRFSTP])\s*(=?\s*-?\d+\.?\d*)", RegexOptions.IgnoreCase);
    
    // 匹配注释
    private static readonly Regex CommentRegex = new(@"[;(].*$");
    
    /// <summary>
    /// 解析 G 代码文本
    /// </summary>
    public GCodeDocument Parse(string gcode)
    {
        var document = new GCodeDocument();
        var lines = gcode.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        int lineNumber = 1;
        foreach (var rawLine in lines)
        {
            var line = ParseLine(rawLine.Trim(), lineNumber++);
            if (line != null)
            {
                document.Lines.Add(line);
            }
        }
        
        // 自动分配标签
        AutoAssignLabels(document);
        
        return document;
    }
    
    /// <summary>
    /// 解析单行 G 代码
    /// </summary>
    private GCodeLine? ParseLine(string rawText, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return null;
        
        var line = new GCodeLine
        {
            LineNumber = lineNumber,
            RawText = rawText
        };
        
        // 移除注释部分进行解析
        var codeText = CommentRegex.Replace(rawText, "").Trim();
        
        // 检查是否是纯注释行
        if (string.IsNullOrEmpty(codeText) && rawText.Contains(';') || rawText.Contains('('))
        {
            line.Type = GCodeType.Comment;
            line.Command = ";";
            line.Label = rawText.TrimStart(';', '(', ' ').TrimEnd(')', ' ');
            return line;
        }
        
        // 提取指令
        var commandMatch = CommandRegex.Match(codeText);
        if (commandMatch.Success)
        {
            line.Command = commandMatch.Value.ToUpper();
            line.Type = DetermineType(line.Command);
        }
        
        // 提取参数
        var paramMatches = ParamRegex.Matches(codeText);
        foreach (Match match in paramMatches)
        {
            var param = match.Groups[1].Value.ToUpper();
            var valStr = match.Groups[2].Value.Replace("=", "").Trim();
            
            if (double.TryParse(valStr, out var value))
            {
                switch (param)
                {
                    case "X": line.X = value; break;
                    case "Y": line.Y = value; break;
                    case "Z": line.Z = value; break;
                    case "I": line.I = value; break;
                    case "J": line.J = value; break;
                    case "K": line.K = value; break;
                    case "R": line.R = value; break;
                    case "F": line.F = value; break;
                    case "S": line.S = value; break;
                }
            }
        }
        
        // 处理纯 F/S 命令行 (如 "F10.0" 或 "S1000")
        if (string.IsNullOrEmpty(line.Command) && (line.F.HasValue || line.S.HasValue))
        {
            // 使用原始文本作为 Command（去除空格）
            line.Command = codeText.Trim();
            line.Type = GCodeType.Setup;
        }
        
        return line;
    }
    
    /// <summary>
    /// 根据指令确定类型
    /// </summary>
    private GCodeType DetermineType(string command)
    {
        return command switch
        {
            "G00" or "G0" or "RAPID" => GCodeType.Rapid,
            "G01" or "G1" or "LINEAR" => GCodeType.Linear,
            "G02" or "G2" or "ARC1" => GCodeType.ArcCW,
            "G03" or "G3" or "ARC2" => GCodeType.ArcCCW,
            "G04" or "G4" => GCodeType.Dwell,
            "G20" or "G21" or "G90" or "G91" or "G17" or "G18" or "G19" => GCodeType.Setup,
            "M03" or "M3" or "M04" or "M4" or "M05" or "M5" => GCodeType.Spindle,
            "M06" or "M6" => GCodeType.ToolChange,
            "M07" or "M7" or "M08" or "M8" or "M09" or "M9" => GCodeType.Coolant,
            "M00" or "M0" or "M01" or "M1" or "M02" or "M2" or "M30" => GCodeType.Program,
            _ => GCodeType.Unknown
        };
    }
    
    /// <summary>
    /// 自动分配标签
    /// </summary>
    private void AutoAssignLabels(GCodeDocument document)
    {
        foreach (var line in document.Lines)
        {
            // 根据类型自动打标签
            line.Tags.Add(line.Type.GetLabel());
            
            // 生成可读标签
            line.Label = line.Type switch
            {
                GCodeType.Setup => GetSetupLabel(line.Command),
                GCodeType.Rapid => "Rapid Move",
                GCodeType.Linear => "Linear Cut",
                GCodeType.ArcCW => "Arc CW",
                GCodeType.ArcCCW => "Arc CCW",
                GCodeType.Spindle => GetSpindleLabel(line.Command),
                GCodeType.Coolant => "Coolant",
                GCodeType.ToolChange => "Tool Change",
                GCodeType.Program => GetProgramLabel(line.Command),
                _ => ""
            };
        }
    }
    
    private string GetSetupLabel(string command) => command switch
    {
        "G20" => "Inch Units",
        "G21" => "Metric Units",
        "G90" => "Absolute Mode",
        "G91" => "Relative Mode",
        "G17" => "XY Plane",
        "G18" => "XZ Plane",
        "G19" => "YZ Plane",
        _ => "Setup"
    };
    
    private string GetSpindleLabel(string command) => command switch
    {
        "M03" or "M3" => "Spindle CW",
        "M04" or "M4" => "Spindle CCW",
        "M05" or "M5" => "Spindle Stop",
        _ => "Spindle"
    };
    
    private string GetProgramLabel(string command) => command switch
    {
        "M00" or "M0" => "Program Stop",
        "M01" or "M1" => "Optional Stop",
        "M02" or "M2" or "M30" => "Program End",
        _ => "Program"
    };
}
