namespace GCodeWorkbench.UI.Models;

public enum ControllerType
{
    Pmac,
    Beckhoff
}

/// <summary>
/// 命令风格：G-code (G00/G01) 或 PMAC 原生 (RAPID/LINEAR)
/// </summary>
public enum CommandStyle
{
    GCode,      // G00, G01, G02, G03
    PmacNative  // RAPID, LINEAR, ARC (CCW), ARC (CW)
}

public class DxfImportOptions
{
    public ControllerType Controller { get; set; } = ControllerType.Pmac;
    
    /// <summary>
    /// 命令风格：G-code 或 PMAC 原生
    /// </summary>
    public CommandStyle Style { get; set; } = CommandStyle.PmacNative;
    
    // Default Header (Standard G-Code setup)
    public string HeaderCode { get; set; } = "G21\nG90";
    
    // Default Footer (Program End)
    public string FooterCode { get; set; } = "M30";
    
    // Formatting Options
    public bool UseLineNumbers { get; set; } = false;
    public bool UseCompactCommands { get; set; } = false; // G1 vs G01
    
    // Feed Rates (mm/sec)
    public double FeedRate { get; set; } = 10.0;  // Default 10 mm/sec
    public double PlungeFeedRate { get; set; } = 5.0;  // Default 5 mm/sec
    public double RapidFeedRate { get; set; } = 100.0; // Used for simulation estimation (100 mm/sec)
    
    // Triggers for "Print" (Start/Stop printing)
    // PMAC/Beckhoff might use different defaults, user can override.
    // Example: M3/M5 or specific I/O codes.
    public string StartTrigger { get; set; } = "M03";
    public string StopTrigger { get; set; } = "M05";
    
    // Origin Selection
    public OriginType Origin { get; set; } = OriginType.DxfOriginal;
}

public enum OriginType
{
    DxfOriginal,        // 0,0 is Dxf 0,0
    FirstEntityStart,   // 0,0 involves shifting so (0,0) is first entity start
    BoundingBoxCenter,  // 0,0 is geometric center
    BoundingBoxTopLeft  // 0,0 is top-left
}
