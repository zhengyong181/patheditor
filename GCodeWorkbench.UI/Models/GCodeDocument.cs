using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GCodeWorkbench.UI.Models;

/// <summary>
/// G 代码文档模型，包含所有代码行和状态
/// </summary>
public class GCodeDocument : INotifyPropertyChanged
{
    private List<GCodeLine> _lines = new();
    private List<GCodeLine>? _cachedFlatLines;
    private int _selectedIndex = -1;
    private string _fileName = "untitled.nc";
    private bool _isDirty = false;
    
    public List<GCodeLine> Lines
    {
        get => _lines;
        set
        {
            _lines = value;
            InvalidateCache();
            OnPropertyChanged();
        }
    }
    
    public int SelectedIndex
    {
        get => _selectedIndex;
        set 
        { 
            if (_selectedIndex != value)
            {
                _selectedIndex = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedLine));
            }
        }
    }
    
    public GCodeLine? SelectedLine
    {
        get
        {
            if (_selectedIndex < 0) return null;
            var flatLines = GetFlatLines();
            return _selectedIndex < flatLines.Count ? flatLines[_selectedIndex] : null;
        }
    }
    
    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }
    
    public bool IsDirty
    {
        get => _isDirty;
        set { _isDirty = value; OnPropertyChanged(); }
    }
    
    private string? _projectPath;
    public string? ProjectPath
    {
        get => _projectPath;
        set { _projectPath = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// 获取 G 代码文本
    /// </summary>
    public string GetGCodeText() => GenerateGCode();

    /// <summary>
    /// 获取所有可见行（考虑折叠状态）
    /// </summary>
    public IEnumerable<(GCodeLine Line, int FlatIndex)> GetVisibleLines()
    {
        int index = 0;
        foreach (var line in _lines)
        {
            yield return (line, index++);
            
            if (line.IsPolyline && !line.IsCollapsed)
            {
                foreach (var child in line.Children)
                {
                    yield return (child, index++);
                }
            }
        }
    }
    
    /// <summary>
    /// 获取扁平化的所有行（用于渲染）
    /// </summary>
    public List<GCodeLine> GetFlatLines()
    {
        if (_cachedFlatLines != null)
        {
            return _cachedFlatLines;
        }

        var result = new List<GCodeLine>();
        foreach (var line in _lines)
        {
            result.AddRange(line.GetAllLines());
        }

        _cachedFlatLines = result;
        return result;
    }
    
    /// <summary>
    /// Invalidate the flat line cache.
    /// Should be called whenever the structure of Lines (add/remove/reorder) changes.
    /// Property changes (X/Y/Z) do not require invalidation unless they affect structure.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedFlatLines = null;
    }

    /// <summary>
    /// 生成 G 代码文本
    /// </summary>
    public string GenerateGCode()
    {
        var lines = new List<string>();
        foreach (var line in GetFlatLines())
        {
            if (!string.IsNullOrEmpty(line.RawText))
            {
                lines.Add(line.RawText);
            }
            else if (!line.IsPolyline)
            {
                lines.Add($"{line.Command} {line.DisplayParameters}".Trim());
            }
        }
        return string.Join(Environment.NewLine, lines);
    }
    
    public (double MinX, double MinY, double MaxX, double MaxY) GetBoundingBox()
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        bool hasPoints = false;
        
        var flatLines = GetFlatLines();
        
        // 初始位置
        double curX = 0, curY = 0;
        
        foreach (var line in flatLines)
        {
            if (line.X.HasValue) curX = line.X.Value;
            if (line.Y.HasValue) curY = line.Y.Value;
            
            if (line.Type == GCodeType.Linear || line.Type == GCodeType.Rapid || line.Type == GCodeType.Polyline)
            {
                if (curX < minX) minX = curX;
                if (curY < minY) minY = curY;
                if (curX > maxX) maxX = curX;
                if (curY > maxY) maxY = curY;
                hasPoints = true;
            }
        }
        
        if (!hasPoints) return (0, 0, 100, 100);
        
        // padding
        double padX = (maxX - minX) * 0.1;
        double padY = (maxY - minY) * 0.1;
        if (padX == 0) padX = 10;
        if (padY == 0) padY = 10;
        
        return (minX - padX, minY - padY, maxX + padX, maxY + padY);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
