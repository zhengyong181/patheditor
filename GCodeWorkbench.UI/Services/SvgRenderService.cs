using System.Text;
using GCodeWorkbench.UI.Models;

namespace GCodeWorkbench.UI.Services;

public class SvgRenderService
{
    public (string RapidPath, string FeedPath) GeneratePaths(GCodeDocument doc)
    {
        var sbRapid = new StringBuilder();
        var sbFeed = new StringBuilder();
        
        double curX = 0;
        double curY = 0;
        double curZ = 0;
        
        // Track the current end position of the SVG Feed Path
        // If curX/Y deviates from this, we must emit an 'M' command before drawing.
        double feedPenX = double.NaN;
        double feedPenY = double.NaN;
        
        foreach (var line in doc.GetFlatLines())
        {
            // Update current position from line
            // Only update axes that are specified
            double nextX = line.X ?? curX;
            double nextY = line.Y ?? curY;
            double nextZ = line.Z ?? curZ;
            
            // Check if there is actual movement in 2D
            bool moved = Math.Abs(nextX - curX) > 0.001 || Math.Abs(nextY - curY) > 0.001;
            
            if (line.Type == GCodeType.Rapid)
            {
                if (moved)
                {
                    // Rapid path is distinct, just line segments
                    sbRapid.Append($"M{curX:F3},{curY:F3}L{nextX:F3},{nextY:F3} ");
                }
                // When we Rapid, the Feed path is broken. Reset pen.
                feedPenX = double.NaN;
                feedPenY = double.NaN;
            }
            else if (line.Type == GCodeType.Linear || line.Type == GCodeType.Polyline)
            {
                if (moved)
                {
                    // If pen is undefined or not at start, Move
                    if (double.IsNaN(feedPenX) || Math.Abs(curX - feedPenX) > 0.001 || Math.Abs(curY - feedPenY) > 0.001)
                    {
                        sbFeed.Append($"M{curX:F3},{curY:F3} ");
                    }
                    
                    sbFeed.Append($"L{nextX:F3},{nextY:F3} ");
                    
                    feedPenX = nextX;
                    feedPenY = nextY;
                }
            }
            else if (line.Type == GCodeType.ArcCW || line.Type == GCodeType.ArcCCW)
            {
                // Arc Math
                double i = line.I ?? 0;
                double j = line.J ?? 0;
                double r = Math.Sqrt(i * i + j * j);
                
                // Check for full circle (start == end)
                bool isFullCircle = Math.Abs(curX - nextX) < 0.001 && Math.Abs(curY - nextY) < 0.001 && r > 0.001;
                
                if (moved || isFullCircle)
                {
                    // Gap check
                    if (double.IsNaN(feedPenX) || Math.Abs(curX - feedPenX) > 0.001 || Math.Abs(curY - feedPenY) > 0.001)
                    {
                        sbFeed.Append($"M{curX:F3},{curY:F3} ");
                    }
                    
                    if (r < 0.001)
                    {
                        sbFeed.Append($"L{nextX:F3},{nextY:F3} ");
                    }
                    else
                    {
                        // Calculate flags
                        int sweepFlag = (line.Type == GCodeType.ArcCCW) ? 1 : 0;
                        double cx = curX + i;
                        double cy = curY + j;
                        double startAngle = Math.Atan2(curY - cy, curX - cx);
                        double endAngle = Math.Atan2(nextY - cy, nextX - cx);
                        double angleDiff = endAngle - startAngle;
                        
                        if (sweepFlag == 1) // CCW
                            while (angleDiff <= 0) angleDiff += 2 * Math.PI;
                        else // CW
                            while (angleDiff >= 0) angleDiff -= 2 * Math.PI;
                            
                        int largeArcFlag = Math.Abs(angleDiff) > Math.PI ? 1 : 0;
                        
                        // Full circle: draw two half-arcs
                        if (isFullCircle)
                        {
                            double midX = cx - (curX - cx);
                            double midY = cy - (curY - cy);
                            sbFeed.Append($"A{r:F3},{r:F3} 0 1 {sweepFlag} {midX:F3},{midY:F3} ");
                            sbFeed.Append($"A{r:F3},{r:F3} 0 1 {sweepFlag} {nextX:F3},{nextY:F3} ");
                        }
                        else
                        {
                            sbFeed.Append($"A{r:F3},{r:F3} 0 {largeArcFlag} {sweepFlag} {nextX:F3},{nextY:F3} ");
                        }
                    }
                    
                    feedPenX = nextX;
                    feedPenY = nextY;
                }
            }
            
            curX = nextX;
            curY = nextY;
            curZ = nextZ;
        }
        
        return (sbRapid.ToString(), sbFeed.ToString());
    }
    
    /// <summary>
    /// 生成可点击的路径段列表，每个段包含路径数据和对应的 flatIndex
    /// </summary>
    public List<PathSegment> GenerateClickablePaths(GCodeDocument doc)
    {
        var result = new List<PathSegment>();
        var flatLines = doc.GetFlatLines();
        
        double curX = 0;
        double curY = 0;
        
        for (int i = 0; i < flatLines.Count; i++)
        {
            var line = flatLines[i];
            double nextX = line.X ?? curX;
            double nextY = line.Y ?? curY;
            
            bool moved = Math.Abs(nextX - curX) > 0.001 || Math.Abs(nextY - curY) > 0.001;
            
            string pathData = "";
            bool isRapid = false;
            
            if (line.Type == GCodeType.Rapid)
            {
                if (moved)
                {
                    pathData = $"M{curX:F3},{curY:F3}L{nextX:F3},{nextY:F3}";
                    isRapid = true;
                }
            }
            else if (line.Type == GCodeType.Linear)
            {
                if (moved)
                {
                    pathData = $"M{curX:F3},{curY:F3}L{nextX:F3},{nextY:F3}";
                }
            }
            else if (line.Type == GCodeType.ArcCW || line.Type == GCodeType.ArcCCW)
            {
                double ii = line.I ?? 0;
                double jj = line.J ?? 0;
                double r = Math.Sqrt(ii * ii + jj * jj);
                bool isFullCircle = Math.Abs(curX - nextX) < 0.001 && Math.Abs(curY - nextY) < 0.001 && r > 0.001;
                
                if (moved || isFullCircle)
                {
                    if (r < 0.001)
                    {
                        pathData = $"M{curX:F3},{curY:F3}L{nextX:F3},{nextY:F3}";
                    }
                    else
                    {
                        int sweepFlag = (line.Type == GCodeType.ArcCCW) ? 1 : 0;
                        double cx = curX + ii;
                        double cy = curY + jj;
                        double startAngle = Math.Atan2(curY - cy, curX - cx);
                        double endAngle = Math.Atan2(nextY - cy, nextX - cx);
                        double angleDiff = endAngle - startAngle;
                        
                        if (sweepFlag == 1) while (angleDiff <= 0) angleDiff += 2 * Math.PI;
                        else while (angleDiff >= 0) angleDiff -= 2 * Math.PI;
                        
                        int largeArcFlag = Math.Abs(angleDiff) > Math.PI ? 1 : 0;
                        
                        if (isFullCircle)
                        {
                            double midX = cx - (curX - cx);
                            double midY = cy - (curY - cy);
                            pathData = $"M{curX:F3},{curY:F3}A{r:F3},{r:F3} 0 1 {sweepFlag} {midX:F3},{midY:F3} A{r:F3},{r:F3} 0 1 {sweepFlag} {nextX:F3},{nextY:F3}";
                        }
                        else
                        {
                            pathData = $"M{curX:F3},{curY:F3}A{r:F3},{r:F3} 0 {largeArcFlag} {sweepFlag} {nextX:F3},{nextY:F3}";
                        }
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(pathData))
            {
                result.Add(new PathSegment
                {
                    Path = pathData,
                    FlatIndex = i,
                    IsRapid = isRapid
                });
            }
            
            curX = nextX;
            curY = nextY;
        }
        
        return result;
    }
}

public class PathSegment
{
    public string Path { get; set; } = "";
    public int FlatIndex { get; set; }
    public bool IsRapid { get; set; }
}
