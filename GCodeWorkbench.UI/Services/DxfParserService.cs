using netDxf;
using netDxf.Entities;
using GCodeWorkbench.UI.Models;

namespace GCodeWorkbench.UI.Services;

/// <summary>
/// DXF 文件解析服务，将 DXF 实体转换为 G 代码
/// </summary>
public class DxfParserService
{
    private const double Tolerance = 0.001;
    
    // Event for logging to UI
    public event Action<string>? OnLog;
    
    private void Log(string msg)
    {
         OnLog?.Invoke(msg);
         Console.WriteLine(msg); // Keep Console backup
    }
    /// 解析 DXF 文件转换为 G 代码文档
    /// </summary>
    /// <summary>
    /// 解析 DXF 文件转换为 G 代码文档
    /// </summary>
    public GCodeDocument LoadDxf(string filePath, DxfImportOptions? options = null)
    {
        var doc = DxfDocument.Load(filePath);
        if (doc == null)
            throw new Exception("Failed to load DXF file");
            
        var gcodeDoc = new GCodeDocument
        {
            FileName = Path.GetFileName(filePath)
        };
        
        options ??= new DxfImportOptions();
        
        // Reset state
        _lastX = 0;
        _lastY = 0;
        _hasLastPos = false;
        
        // Calculate Offset (NEW)
        CalculateOriginOffset(doc, options);
        
        int lineNumber = 1;
        
        // 1. 初始化设置 (User Configured Header)
        if (!string.IsNullOrWhiteSpace(options.HeaderCode))
        {
            foreach (var line in options.HeaderCode.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                AddLine(gcodeDoc, ref lineNumber, GCodeType.Setup, line.Trim(), "", "Header");
            }
        }
        
        // 1.5 添加 Feed Rate 命令 (F 在程序头生成，单位 mm/sec)
        // 同时设置 F 属性，以便 SimulationService 可以读取
        AddLine(gcodeDoc, ref lineNumber, GCodeType.Setup, "F", "", "Feed Rate (mm/sec)", "", null, f: options.FeedRate, customRawText: $"F{options.FeedRate:F1}");
        
        // 2. 将实体转换为 G 代码
        // 处理 LINE
        foreach (var line in doc.Entities.Lines)
        {
            ConvertLine(line, gcodeDoc, ref lineNumber, options);
        }
        
        // 处理 POLYLINE (Polyline2D)
        foreach (var poly in doc.Entities.Polylines2D)
        {
            ConvertPolyline(poly, gcodeDoc, ref lineNumber, options);
        }
        
        // 注意: netDxf 2023 中 LWPOLYLINE 已合并到 Polyline2D
        // doc.Entities.Polylines2D 包含了两种类型
        
        // 处理 CIRCLE
        foreach (var circle in doc.Entities.Circles)
        {
            ConvertCircle(circle, gcodeDoc, ref lineNumber, options);
        }
        
        // 处理 ARC
        foreach (var arc in doc.Entities.Arcs)
        {
            ConvertArc(arc, gcodeDoc, ref lineNumber, options);
        }
        
        // 3. 结束程序 (User Configured Footer)
        if (!string.IsNullOrWhiteSpace(options.FooterCode))
        {
            foreach (var line in options.FooterCode.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                AddLine(gcodeDoc, ref lineNumber, GCodeType.Program, line.Trim(), "", "Footer");
            }
        }
        
        return gcodeDoc;
    }
    
    private double _offsetX = 0;
    private double _offsetY = 0;

    private void CalculateOriginOffset(DxfDocument doc, DxfImportOptions options)
    {
        _offsetX = 0;
        _offsetY = 0;
        
        Log($"[DxfParser] Calculating Origin. Option: {options.Origin}");
        
        if (options.Origin == OriginType.DxfOriginal) 
        {
             Log("[DxfParser] Origin is Default (DXF 0,0). No offset applied.");
             return;
        }

        if (options.Origin == OriginType.FirstEntityStart)
        {
            // Try to find the first entity
            if (doc.Entities.Lines.Any())
            {
                var first = doc.Entities.Lines.First();
                if (first != null)
                {
                    _offsetX = -first.StartPoint.X;
                    _offsetY = -first.StartPoint.Y;
                    Log($"[DxfParser] First Entity (Line) found. Offset: {_offsetX:F3}, {_offsetY:F3}");
                }
            }
            else if (doc.Entities.Polylines2D.Any())
            {
                var first = doc.Entities.Polylines2D.First();
                if (first != null && first.Vertexes.Count > 0)
                {
                    _offsetX = -first.Vertexes[0].Position.X;
                    _offsetY = -first.Vertexes[0].Position.Y;
                     Log($"[DxfParser] First Entity (Polyline) found. Offset: {_offsetX:F3}, {_offsetY:F3}");
                }
            }
             else if (doc.Entities.Circles.Any())
            {
                var first = doc.Entities.Circles.First();
                if (first != null)
                {
                    // Circle start is typically (Center.X + Radius, Center.Y)
                    _offsetX = -(first.Center.X + first.Radius);
                    _offsetY = -first.Center.Y;
                     Log($"[DxfParser] First Entity (Circle) found. Offset: {_offsetX:F3}, {_offsetY:F3}");
                }
            }
            else if (doc.Entities.Arcs.Any())
            {
                 var first = doc.Entities.Arcs.First();
                if (first != null)
                {
                     double startRad = first.StartAngle * Math.PI / 180.0;
                     double sx = first.Center.X + first.Radius * Math.Cos(startRad);
                     double sy = first.Center.Y + first.Radius * Math.Sin(startRad);
                    _offsetX = -sx;
                    _offsetY = -sy;
                     Log($"[DxfParser] First Entity (Arc) found. Offset: {_offsetX:F3}, {_offsetY:F3}");
                }
            }
            return;
        }

        // Precise Bounding Box Logic
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        bool hasBounds = false;
        
        void UpdateBounds(double x, double y)
        {
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
            hasBounds = true;
        }

        // Helper for Arc Bounds
        void UpdateArcBounds(double cx, double cy, double r, double startAngleRad, double endAngleRad, bool isCcw)
        {
             // 1. Add Endpoints
             UpdateBounds(cx + r * Math.Cos(startAngleRad), cy + r * Math.Sin(startAngleRad));
             UpdateBounds(cx + r * Math.Cos(endAngleRad), cy + r * Math.Sin(endAngleRad));
             
             // 2. Add Cardinal Points (0, 90, 180, 270 deg) if included
             // Normalize angles to 0-2PI
             double Normalize(double angle) 
             {
                 angle = angle % (2 * Math.PI);
                 if (angle < 0) angle += 2 * Math.PI;
                 return angle;
             }
             
             double start = Normalize(startAngleRad);
             double end = Normalize(endAngleRad);
             
             double[] cardinals = { 0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 };
             foreach (var card in cardinals)
             {
                 bool included = false;
                 if (isCcw)
                 {
                     if (start < end) included = card >= start && card <= end;
                     else included = card >= start || card <= end; // Wraps around 0
                 }
                 else // CW
                 {
                     if (start > end) included = card <= start && card >= end;
                     else included = card <= start || card >= end; // Wraps around 0
                 }
                 
                 if (included)
                 {
                     UpdateBounds(cx + r * Math.Cos(card), cy + r * Math.Sin(card));
                 }
             }
        }
        
        foreach (var l in doc.Entities.Lines)
        {
            UpdateBounds(l.StartPoint.X, l.StartPoint.Y);
            UpdateBounds(l.EndPoint.X, l.EndPoint.Y);
        }
        
        foreach (var p in doc.Entities.Polylines2D)
        {
            if (p.Vertexes.Count == 0) continue;
            
            // Add First Point
            UpdateBounds(p.Vertexes[0].Position.X, p.Vertexes[0].Position.Y);
            
            int vCount = p.Vertexes.Count;
            int segCount = p.IsClosed ? vCount : vCount - 1;
            
            for(int i=0; i<segCount; i++)
            {
                var cur = p.Vertexes[i];
                var next = p.Vertexes[(i + 1) % vCount];
                
                // Add Next Point (simplest way to ensure all vertices are covered)
                UpdateBounds(next.Position.X, next.Position.Y);
                
                if (Math.Abs(cur.Bulge) > 0.0001)
                {
                    // Calculate Arc Params
                    double dx = next.Position.X - cur.Position.X;
                    double dy = next.Position.Y - cur.Position.Y;
                    double chord = Math.Sqrt(dx*dx + dy*dy);
                    if (chord > 0.0001)
                    {
                        double sagitta = Math.Abs(cur.Bulge) * chord / 2;
                        double radius = ((chord/2)*(chord/2) + sagitta*sagitta) / (2*sagitta);
                        double dist = radius - sagitta;
                        
                        double midX = (cur.Position.X + next.Position.X) / 2;
                        double midY = (cur.Position.Y + next.Position.Y) / 2;
                        
                        // Normal vector
                        double nx = -dy / chord;
                        double ny = dx / chord;
                        
                        // Sign determines side
                        double sign = cur.Bulge > 0 ? 1 : -1; 
                        
                        double cx = midX + sign * dist * nx;
                        double cy = midY + sign * dist * ny;
                        
                        double startAngle = Math.Atan2(cur.Position.Y - cy, cur.Position.X - cx);
                        double endAngle = Math.Atan2(next.Position.Y - cy, next.Position.X - cx);
                        
                        // Bulge > 0 is CCW, < 0 is CW
                        UpdateArcBounds(cx, cy, radius, startAngle, endAngle, cur.Bulge > 0);
                    }
                }
            }
        }
        
        foreach (var c in doc.Entities.Circles)
        {
            UpdateBounds(c.Center.X - c.Radius, c.Center.Y - c.Radius);
            UpdateBounds(c.Center.X + c.Radius, c.Center.Y + c.Radius);
        }
         
        foreach (var a in doc.Entities.Arcs)
        {
             // netDxf Arcs are CCW by default
             double startRad = a.StartAngle * Math.PI / 180.0;
             double endRad = a.EndAngle * Math.PI / 180.0;
             UpdateArcBounds(a.Center.X, a.Center.Y, a.Radius, startRad, endRad, true);
        }
        
        if (!hasBounds) 
        {
            Log("[DxfParser] No bounds found for BoundingBox origin.");
            return;
        }
        
        Log($"[DxfParser] Precise Bounds found. Min: {minX:F3},{minY:F3} Max: {maxX:F3},{maxY:F3}");
        
        if (options.Origin == OriginType.BoundingBoxCenter)
        {
            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;
            _offsetX = -centerX;
            _offsetY = -centerY;
             Log($"[DxfParser] Box Center. Offset: {_offsetX:F3}, {_offsetY:F3}");
        }
        else if (options.Origin == OriginType.BoundingBoxTopLeft)
        {
             // To set Top-Left (MinX, MaxY) to (0,0):
             // NewX = OldX - MinX  => OffsetX = -MinX
             // NewY = OldY - MaxY  => OffsetY = -MaxY
            _offsetX = -minX;
            _offsetY = -maxY;
             Log($"[DxfParser] Box TopLeft. Offset: {_offsetX:F3}, {_offsetY:F3}");
        }
    }
    
    // State for continuous path generation
    private double _lastX = 0;
    private double _lastY = 0;
    private bool _hasLastPos = false;

    private void MoveTo(GCodeDocument doc, ref int ln, double x, double y, DxfImportOptions options)
    {
        double finalX = x + _offsetX;
        double finalY = y + _offsetY;
        
        // Check if we are already there (within tolerance)
        // We compare against the last GENERATED position (which already includes offset)
        if (_hasLastPos && Math.Abs(finalX - _lastX) < 0.001 && Math.Abs(finalY - _lastY) < 0.001)
        {
            return;
        }

        // Rapid to position
        AddLine(doc, ref ln, GCodeType.Rapid, GetRapidCmd(options), $"X{finalX:F3} Y{finalY:F3}", "Rapid", "", options, x: finalX, y: finalY);
        _lastX = finalX;
        _lastY = finalY;
        _hasLastPos = true;
    }
    
    private void ConvertLine(Line line, GCodeDocument doc, ref int ln, DxfImportOptions options)
    {
        // 1. Move to start (Smart Rapid)
        MoveTo(doc, ref ln, line.StartPoint.X, line.StartPoint.Y, options);
        
        // 2. Cut to end
        double endX = line.EndPoint.X + _offsetX;
        double endY = line.EndPoint.Y + _offsetY;
        
        AddLine(doc, ref ln, GCodeType.Linear, GetLinearCmd(options), $"X{endX:F3} Y{endY:F3}", "Line", "", options, x: endX, y: endY);
        
        _lastX = endX;
        _lastY = endY;
        _hasLastPos = true;
    }
    
    private void ConvertPolyline(netDxf.Entities.Polyline2D poly, GCodeDocument doc, ref int ln, DxfImportOptions options)
    {
        if (poly.Vertexes.Count < 2) return;
        
        // 计算起点坐标
        var start = poly.Vertexes[0];
        var firstX = start.Position.X + _offsetX;
        var firstY = start.Position.Y + _offsetY;
        
        // 1. 先 Smart Move 到起点（这样 RAPID 在多段线之前）
        MoveTo(doc, ref ln, start.Position.X, start.Position.Y, options);
        
        // 2. 然后创建多段线父节点
        var parentLine = new GCodeLine
        {
            LineNumber = ln++,
            IsPolyline = true,
            IsCollapsed = true,
            Label = $"Polyline ({poly.Vertexes.Count} pts)",
            Type = GCodeType.Polyline,
            X = firstX,
            Y = firstY
        };
        doc.Lines.Add(parentLine);
        
        // 遍历各段
        int vertexCount = poly.Vertexes.Count;
        int segmentCount = poly.IsClosed ? vertexCount : vertexCount - 1;
        
        for (int i = 0; i < segmentCount; i++)
        {
            var currentVertex = poly.Vertexes[i];
            int nextIndex = (i + 1) % vertexCount;
            var nextVertex = poly.Vertexes[nextIndex];
            double bulge = currentVertex.Bulge;
            
            double nextX = nextVertex.Position.X + _offsetX;
            double nextY = nextVertex.Position.Y + _offsetY;

            if (Math.Abs(bulge) < 0.0001)
            {
                // 直线段
                AddChildLine(parentLine, ref ln, GCodeType.Linear, GetLinearCmd(options), 
                    $"X{nextX:F3} Y{nextY:F3}", "", options, 
                    x: nextX, y: nextY);
            }
            else
            {
                // 圆弧段 logic
                // Original geometry needs raw coordinates for vector math
                double rawStartX = currentVertex.Position.X;
                double rawStartY = currentVertex.Position.Y;
                double rawEndX = nextVertex.Position.X;
                double rawEndY = nextVertex.Position.Y;
                
                // Geometry Calc
                double chordX = rawEndX - rawStartX;
                double chordY = rawEndY - rawStartY;
                double chordLen = Math.Sqrt(chordX * chordX + chordY * chordY);
                
                if (chordLen < 0.0001) continue;
                
                double midX = (rawStartX + rawEndX) / 2;
                double midY = (rawStartY + rawEndY) / 2;
                double arcHeight = Math.Abs(bulge) * chordLen / 2;
                double halfChord = chordLen / 2;
                double radius = (halfChord * halfChord + arcHeight * arcHeight) / (2 * arcHeight);
                double distToCenter = radius - arcHeight;
                double normX = -chordY / chordLen;
                double normY = chordX / chordLen;
                double sign = bulge > 0 ? 1 : -1;
                double rawCenterX = midX + sign * distToCenter * normX;
                double rawCenterY = midY + sign * distToCenter * normY;
                
                // I and J are relative vectors, so they are NOT affected by offset!
                double iVal = rawCenterX - rawStartX;
                double jVal = rawCenterY - rawStartY;
                
                string arcCmd = bulge > 0 ? GetArcCCWCmd(options) : GetArcCWCmd(options);
                GCodeType arcType = bulge > 0 ? GCodeType.ArcCCW : GCodeType.ArcCW;
                
                AddChildLine(parentLine, ref ln, arcType, arcCmd,
                    $"X{nextX:F3} Y{nextY:F3} I{iVal:F3} J{jVal:F3}", "", options,
                    x: nextX, y: nextY, i: iVal, j: jVal);
            }
        }
        
        // Update state to end of polyline
        if (poly.Vertexes.Count > 0)
        {
             var endV = poly.IsClosed ? poly.Vertexes[0] : poly.Vertexes[poly.Vertexes.Count - 1];
             _lastX = endV.Position.X + _offsetX;
             _lastY = endV.Position.Y + _offsetY;
             _hasLastPos = true;
        }
    }

    
    private void ConvertCircle(Circle circle, GCodeDocument doc, ref int ln, DxfImportOptions options)
    {
        double startX = circle.Center.X + circle.Radius;
        double startY = circle.Center.Y;
        double iVal = -circle.Radius;
        double jVal = 0;
        
        // Smart Move
        MoveTo(doc, ref ln, startX, startY, options);
        
        double finalStartX = startX + _offsetX;
        double finalStartY = startY + _offsetY;
        
        string arcCmd = GetArcCWCmd(options);
        AddLine(doc, ref ln, GCodeType.ArcCW, arcCmd, $"X{finalStartX:F3} Y{finalStartY:F3} I{iVal:F3} J{jVal:F3}", $"Circle R{circle.Radius:F2}", "", options, x: finalStartX, y: finalStartY, i: iVal, j: jVal);
        _lastX = finalStartX;
        _lastY = finalStartY;
        _hasLastPos = true;
    }
    
    private void ConvertArc(Arc arc, GCodeDocument doc, ref int ln, DxfImportOptions options)
    {
        double startAngleRad = arc.StartAngle * Math.PI / 180.0;
        double endAngleRad = arc.EndAngle * Math.PI / 180.0;
        
        double rawStartX = arc.Center.X + arc.Radius * Math.Cos(startAngleRad);
        double rawStartY = arc.Center.Y + arc.Radius * Math.Sin(startAngleRad);
        double rawEndX = arc.Center.X + arc.Radius * Math.Cos(endAngleRad);
        double rawEndY = arc.Center.Y + arc.Radius * Math.Sin(endAngleRad);
        
        // I and J are relative vectors, so they are NOT affected by offset!
        double iVal = arc.Center.X - rawStartX;
        double jVal = arc.Center.Y - rawStartY;
        
        // Smart Move
        MoveTo(doc, ref ln, rawStartX, rawStartY, options);
        
        double finalEndX = rawEndX + _offsetX;
        double finalEndY = rawEndY + _offsetY;
        
        string arcCmd = GetArcCCWCmd(options);
        AddLine(doc, ref ln, GCodeType.ArcCCW, arcCmd, $"X{finalEndX:F3} Y{finalEndY:F3} I{iVal:F3} J{jVal:F3}", $"Arc R{arc.Radius:F2}", "", options, x: finalEndX, y: finalEndY, i: iVal, j: jVal);
        
        _lastX = finalEndX;
        _lastY = finalEndY;
        _hasLastPos = true;
    }
    
    // 辅助方法：添加顶级行
    // 辅助方法：添加顶级行
    private void AddLine(GCodeDocument doc, ref int ln, GCodeType type, string cmd, string param = "", string label = "", string desc = "", DxfImportOptions? options = null, 
        double? x = null, double? y = null, double? z = null, double? f = null, double? i = null, double? j = null, string? customRawText = null)
    {
        string prefix = "";
        if (options != null && options.UseLineNumbers)
        {
            prefix = $"N{ln} ";
        }
        
        doc.Lines.Add(new GCodeLine
        {
            LineNumber = ln++,
            Type = type,
            Command = cmd,
            RawText = customRawText ?? $"{prefix}{cmd} {param}".Trim(), // Use custom raw text if provided
            Label = string.IsNullOrEmpty(label) ? type.ToString() : label,
            X = x.HasValue ? Math.Round(x.Value, 3) : null,
            Y = y.HasValue ? Math.Round(y.Value, 3) : null,
            Z = z.HasValue ? Math.Round(z.Value, 3) : null,
            F = f.HasValue ? Math.Round(f.Value, 3) : null,
            I = i.HasValue ? Math.Round(i.Value, 3) : null,
            J = j.HasValue ? Math.Round(j.Value, 3) : null
        });
        
        // 简单参数解析用于显示（实际应复用 GCodeParser 逻辑）
        if (type == GCodeType.Setup || type == GCodeType.Rapid || type == GCodeType.Linear)
        {
            // var last = doc.Lines.Last();
            // 这里为了演示简单处理，正式版应完整解析
        }
    }
    
    // 辅助方法：添加子行
    // 辅助方法：添加子行
    private void AddChildLine(GCodeLine parent, ref int ln, GCodeType type, string cmd, string param = "", string label = "", DxfImportOptions? options = null,
        double? x = null, double? y = null, double? z = null, double? f = null, double? i = null, double? j = null)
    {
        string prefix = "";
        if (options != null && options.UseLineNumbers)
        {
            prefix = $"N{ln} ";
        }
        
        var child = new GCodeLine
        {
            LineNumber = ln++,
            Type = type,
            Command = cmd,
            RawText = $"{prefix}{cmd} {param}".Trim(),
            Parent = parent,
            Label = label,
            X = x.HasValue ? Math.Round(x.Value, 3) : null,
            Y = y.HasValue ? Math.Round(y.Value, 3) : null,
            Z = z.HasValue ? Math.Round(z.Value, 3) : null,
            F = f.HasValue ? Math.Round(f.Value, 3) : null,
            I = i.HasValue ? Math.Round(i.Value, 3) : null,
            J = j.HasValue ? Math.Round(j.Value, 3) : null
        };
        parent.Children.Add(child);
    }
    
    private string GetRapidCmd(DxfImportOptions options) => 
        options.Style == CommandStyle.PmacNative ? "RAPID" : (options.UseCompactCommands ? "G0" : "G00");
    
    private string GetLinearCmd(DxfImportOptions options) => 
        options.Style == CommandStyle.PmacNative ? "LINEAR" : (options.UseCompactCommands ? "G1" : "G01");
    
    private string GetArcCWCmd(DxfImportOptions options) => 
        options.Style == CommandStyle.PmacNative ? "ARC1" : (options.UseCompactCommands ? "G2" : "G02");
    
    private string GetArcCCWCmd(DxfImportOptions options) => 
        options.Style == CommandStyle.PmacNative ? "ARC2" : (options.UseCompactCommands ? "G3" : "G03");
}
