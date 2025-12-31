using GCodeWorkbench.UI.Commands;

namespace GCodeWorkbench.UI.Services;

/// <summary>
/// 管理 Undo/Redo 操作的服务
/// </summary>
public class CommandManager
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private readonly int _maxHistory;
    
    public event Action? OnStateChanged;
    
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;
    
    public CommandManager(int maxHistory = 200)
    {
        _maxHistory = maxHistory;
    }
    
    /// <summary>
    /// 执行命令并记录到历史
    /// </summary>
    public void Execute(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // 执行新命令后清空 redo
        
        // 限制历史记录数量
        TrimHistory();
        
        OnStateChanged?.Invoke();
    }
    
    /// <summary>
    /// 撤销上一个命令
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;
        
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        
        OnStateChanged?.Invoke();
    }
    
    /// <summary>
    /// 重做上一个撤销的命令
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;
        
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        
        OnStateChanged?.Invoke();
    }
    
    /// <summary>
    /// 清空所有历史
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnStateChanged?.Invoke();
    }
    
    /// <summary>
    /// 获取 Undo 历史描述（用于 UI 显示）
    /// </summary>
    public IEnumerable<string> GetUndoHistory()
    {
        return _undoStack.Select(c => c.Description);
    }
    
    /// <summary>
    /// 获取 Redo 历史描述
    /// </summary>
    public IEnumerable<string> GetRedoHistory()
    {
        return _redoStack.Select(c => c.Description);
    }
    
    private void TrimHistory()
    {
        // 保持 undo 栈不超过 maxHistory
        if (_undoStack.Count > _maxHistory)
        {
            var tempList = _undoStack.ToList();
            tempList.RemoveRange(_maxHistory, tempList.Count - _maxHistory);
            _undoStack.Clear();
            foreach (var cmd in tempList.AsEnumerable().Reverse())
                _undoStack.Push(cmd);
        }
    }
}
