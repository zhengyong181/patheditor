using System.Text.Json.Serialization;

namespace GCodeWorkbench.UI.Models;

/// <summary>
/// 项目文件数据模型
/// </summary>
public class ProjectData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
    
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = "";
    
    [JsonPropertyName("projectPath")]
    public string? ProjectPath { get; set; }
    
    [JsonPropertyName("lines")]
    public List<GCodeLineData> Lines { get; set; } = new();
}

/// <summary>
/// G代码 行数据模型（用于序列化）
/// </summary>
public class GCodeLineData
{
    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }
    
    [JsonPropertyName("rawText")]
    public string RawText { get; set; } = "";
    
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Unknown";
    
    [JsonPropertyName("x")]
    public double? X { get; set; }
    
    [JsonPropertyName("y")]
    public double? Y { get; set; }
    
    [JsonPropertyName("z")]
    public double? Z { get; set; }
    
    [JsonPropertyName("i")]
    public double? I { get; set; }
    
    [JsonPropertyName("j")]
    public double? J { get; set; }
    
    [JsonPropertyName("f")]
    public double? F { get; set; }
    
    [JsonPropertyName("s")]
    public double? S { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
    
    [JsonPropertyName("isPolyline")]
    public bool IsPolyline { get; set; }
    
    [JsonPropertyName("children")]
    public List<GCodeLineData> Children { get; set; } = new();
}

public static class ProjectDataExtensions
{
    public static ProjectData ToProjectData(this GCodeDocument doc)
    {
        return new ProjectData
        {
            Version = "1.0",
            FileName = doc.FileName,
            Lines = doc.Lines.Select(ToLineData).ToList()
        };
    }
    
    public static GCodeDocument ToDocument(this ProjectData data)
    {
        var doc = new GCodeDocument
        {
            FileName = data.FileName
        };
        doc.Lines.AddRange(data.Lines.Select(ToGCodeLine));
        return doc;
    }
    
    private static GCodeLineData ToLineData(GCodeLine line)
    {
        return new GCodeLineData
        {
            LineNumber = line.LineNumber,
            RawText = line.RawText,
            Command = line.Command,
            Type = line.Type.ToString(),
            X = line.X, Y = line.Y, Z = line.Z, I = line.I, J = line.J, F = line.F, S = line.S,
            Tags = line.Tags.ToList(),
            Label = line.Label,
            IsPolyline = line.IsPolyline,
            Children = line.Children.Select(ToLineData).ToList()
        };
    }
    
    private static GCodeLine ToGCodeLine(GCodeLineData data)
    {
        var line = new GCodeLine
        {
            LineNumber = data.LineNumber,
            RawText = data.RawText,
            Command = data.Command,
            Type = Enum.TryParse<GCodeType>(data.Type, out var type) ? type : GCodeType.Unknown,
            X = data.X, Y = data.Y, Z = data.Z, I = data.I, J = data.J, F = data.F, S = data.S,
            Tags = data.Tags.ToList(),
            Label = data.Label,
            IsPolyline = data.IsPolyline
        };
        
        foreach (var childData in data.Children)
        {
            var child = ToGCodeLine(childData);
            child.Parent = line;
            line.Children.Add(child);
        }
        
        return line;
    }
}
