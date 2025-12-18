namespace GCodeWorkbench.UI;

public class CodeLineModel
{
    public int LineNumber { get; set; }
    public string Command { get; set; } = "";
    public string Parameters { get; set; } = "";
    public string Description { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "";
    public string TypeColor { get; set; } = "slate";
    public bool IsSpindle { get; set; } = false;
    public bool IsPolyline { get; set; } = false;
    public int PolylinePointCount { get; set; } = 0;
    public string PolylineName { get; set; } = "";
    public bool IsCollapsed { get; set; } = true;
    public bool IsChild { get; set; } = false;
    public int ParentIndex { get; set; } = -1;
}
