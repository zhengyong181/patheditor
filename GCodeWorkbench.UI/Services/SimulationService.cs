using GCodeWorkbench.UI.Models;

namespace GCodeWorkbench.UI.Services;

public class SimulationService
{
    private List<MotionSegment> _segments = new();
    private TimeSpan _totalDuration;
    private TimeSpan _currentTime;
    private bool _isPlaying;
    private double _speedMultiplier = 1.0;
    
    // Performance optimization: track last accessed index
    private int _lastSegmentIndex = 0;

    // Default speeds if not specified (mm/sec)
    private const double DefaultRapidFeed = 100; // Fallback: 100 mm/sec
    private const double DefaultCutFeed = 10;    // Fallback: 10 mm/sec
    
    // Configurable rapid feed rate (default 100mm/sec)
    public double RapidFeedRate { get; set; } = 100;
    
    public event Action<SimulationState>? OnStateChanged;
    
    public bool IsPlaying => _isPlaying;
    public double Progress => _totalDuration.TotalSeconds > 0 ? _currentTime.TotalSeconds / _totalDuration.TotalSeconds : 0;
    public double CurrentTimeSeconds => _currentTime.TotalSeconds;
    public double TotalTimeSeconds => _totalDuration.TotalSeconds;
    
    public void Load(GCodeDocument doc)
    {
        _segments.Clear();
        _currentTime = TimeSpan.Zero;
        _totalDuration = TimeSpan.Zero;
        _isPlaying = false;
        _lastSegmentIndex = 0;
        
        double curX = 0, curY = 0, curZ = 0;
        double currentFeed = DefaultCutFeed;
        
        var flatLines = doc.GetFlatLines();
        
        foreach (var line in flatLines)
        {
            double nextX = line.X ?? curX;
            double nextY = line.Y ?? curY;
            double nextZ = line.Z ?? curZ;
            
            if (line.F.HasValue) currentFeed = line.F.Value;
            
            double dist = 0;
            bool isArc = (line.Type == GCodeType.ArcCW || line.Type == GCodeType.ArcCCW);
            double centerX = 0, centerY = 0, radius = 0, startAngle = 0, endAngle = 0;
            bool isCCW = (line.Type == GCodeType.ArcCCW);
            
            if (isArc)
            {
                // 计算圆弧参数
                double i = line.I ?? 0;
                double j = line.J ?? 0;
                centerX = curX + i;
                centerY = curY + j;
                radius = Math.Sqrt(i * i + j * j);
                
                if (radius > 0.0001)
                {
                    startAngle = Math.Atan2(curY - centerY, curX - centerX);
                    endAngle = Math.Atan2(nextY - centerY, nextX - centerX);
                    
                    // 计算角度差
                    double angleDiff = endAngle - startAngle;
                    if (isCCW)
                    {
                        while (angleDiff <= 0) angleDiff += 2 * Math.PI;
                    }
                    else
                    {
                        while (angleDiff >= 0) angleDiff -= 2 * Math.PI;
                    }
                    
                    // 弧长 = |角度差| × 半径
                    dist = Math.Abs(angleDiff) * radius;
                }
            }
            else
            {
                // 直线距离
                dist = Math.Sqrt(Math.Pow(nextX - curX, 2) + Math.Pow(nextY - curY, 2) + Math.Pow(nextZ - curZ, 2));
            }
            
            if (dist > 0.0001)
            {
                // Use configurable rapid feed rate for G0, current feed for G1/G2/G3
                double feed = (line.Type == GCodeType.Rapid) ? RapidFeedRate : currentFeed;
                if (feed <= 0) feed = DefaultCutFeed;
                // Speed is in mm/sec, so duration = dist / feed (seconds)
                double durationSec = dist / feed;
                var duration = TimeSpan.FromSeconds(durationSec);
                
                var segment = new MotionSegment
                {
                    StartPoint = new(curX, curY, curZ),
                    EndPoint = new(nextX, nextY, nextZ),
                    GCodeLine = line,
                    Duration = duration,
                    StartTime = _totalDuration,
                    IsArc = isArc,
                    CenterX = centerX,
                    CenterY = centerY,
                    Radius = radius,
                    StartAngle = startAngle,
                    EndAngle = endAngle,
                    IsCCW = isCCW
                };
                
                _segments.Add(segment);
                _totalDuration += duration;
            }
            
            curX = nextX;
            curY = nextY;
            curZ = nextZ;
        }
    }
    
    public void Play() { _isPlaying = true; }
    public void Pause() { _isPlaying = false; }
    public void Stop() { _isPlaying = false; _currentTime = TimeSpan.Zero; Notify(); }
    
    public void Seek(double progress)
    {
        _currentTime = _totalDuration * Math.Clamp(progress, 0, 1);
        // Reset index hint on seek to ensure correctness
        _lastSegmentIndex = 0;
        Notify();
    }
    
    public void SetSpeed(double multiplier)
    {
        _speedMultiplier = Math.Max(0.1, multiplier);
    }
    
    public void Tick(TimeSpan realTimeDelta)
    {
        if (!_isPlaying) return;
        
        var simDelta = realTimeDelta * _speedMultiplier;
        _currentTime += simDelta;
        
        if (_currentTime >= _totalDuration)
        {
            _currentTime = _totalDuration;
            Pause();
        }
        
        Notify();
    }
    
    private void Notify()
    {
        MotionSegment? currentSeg = null;
        
        if (_segments.Count > 0)
        {
            // Optimization: Start search from _lastSegmentIndex
            // Since time usually moves forward, this makes lookup O(1) in most cases
            int count = _segments.Count;
            if (_lastSegmentIndex >= count) _lastSegmentIndex = count - 1;

            // Check if we need to rewind (rare, only if manual time set backwards without Seek)
            if (_lastSegmentIndex > 0 && _segments[_lastSegmentIndex].StartTime > _currentTime)
            {
                _lastSegmentIndex = 0;
            }

            for (int i = _lastSegmentIndex; i < count; i++)
            {
                var s = _segments[i];
                if (s.StartTime <= _currentTime && (s.StartTime + s.Duration) >= _currentTime)
                {
                    currentSeg = s;
                    _lastSegmentIndex = i;
                    break;
                }

                // If we passed the current time (segments are ordered), then we won't find it later
                if (s.StartTime > _currentTime)
                    break;
            }

            // If still null (e.g. finished), use last
            if (currentSeg == null)
            {
                if (_currentTime >= _totalDuration) currentSeg = _segments.Last();
                else if (_currentTime <= TimeSpan.Zero) currentSeg = _segments.First();
            }
        }
        
        var state = new SimulationState
        {
            IsPlaying = _isPlaying,
            Progress = Progress,
            CurrentLine = currentSeg?.GCodeLine
        };
        
        if (currentSeg != null)
        {
            // Interpolate position
            double segProgress = (_currentTime - currentSeg.StartTime).TotalSeconds / currentSeg.Duration.TotalSeconds;
            segProgress = Math.Clamp(segProgress, 0, 1);
            
            if (currentSeg.IsArc && currentSeg.Radius > 0.0001)
            {
                // 圆弧插值：使用角度
                double angleDiff = currentSeg.EndAngle - currentSeg.StartAngle;
                if (currentSeg.IsCCW)
                {
                    while (angleDiff <= 0) angleDiff += 2 * Math.PI;
                }
                else
                {
                    while (angleDiff >= 0) angleDiff -= 2 * Math.PI;
                }
                
                double currentAngle = currentSeg.StartAngle + angleDiff * segProgress;
                state.X = currentSeg.CenterX + currentSeg.Radius * Math.Cos(currentAngle);
                state.Y = currentSeg.CenterY + currentSeg.Radius * Math.Sin(currentAngle);
                state.Z = Lerp(currentSeg.StartPoint.Z, currentSeg.EndPoint.Z, segProgress);
            }
            else
            {
                // 直线插值
                state.X = Lerp(currentSeg.StartPoint.X, currentSeg.EndPoint.X, segProgress);
                state.Y = Lerp(currentSeg.StartPoint.Y, currentSeg.EndPoint.Y, segProgress);
                state.Z = Lerp(currentSeg.StartPoint.Z, currentSeg.EndPoint.Z, segProgress);
            }
        }
        
        OnStateChanged?.Invoke(state);
    }
    
    private double Lerp(double a, double b, double t) => a + (b - a) * t;
}

public class MotionSegment
{
    public Point3D StartPoint { get; set; }
    public Point3D EndPoint { get; set; }
    public GCodeLine GCodeLine { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public TimeSpan StartTime { get; set; }
    
    // 圆弧参数
    public bool IsArc { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
    public bool IsCCW { get; set; }
}

public struct Point3D 
{
    public double X, Y, Z;
    public Point3D(double x, double y, double z) { X=x; Y=y; Z=z; }
}

public class SimulationState
{
    public bool IsPlaying { get; set; }
    public double Progress { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public GCodeLine? CurrentLine { get; set; }
}
