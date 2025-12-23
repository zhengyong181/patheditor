namespace GCodeWorkbench.UI.Commands;

/// <summary>
/// 可撤销命令接口
/// </summary>
public interface ICommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

/// <summary>
/// 修改属性值的通用命令
/// </summary>
public class SetPropertyCommand<T> : ICommand
{
    private readonly Action<T> _setter;
    private readonly T _newValue;
    private readonly T _oldValue;
    private readonly string _description;
    
    public string Description => _description;
    
    public SetPropertyCommand(Action<T> setter, T oldValue, T newValue, string description = "Set Property")
    {
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
        _description = description;
    }
    
    public void Execute() => _setter(_newValue);
    public void Undo() => _setter(_oldValue);
}

/// <summary>
/// 移动列表项的命令
/// </summary>
public class MoveItemCommand<T> : ICommand
{
    private readonly IList<T> _list;
    private readonly int _fromIndex;
    private readonly int _toIndex;
    
    public string Description => $"Move item from {_fromIndex} to {_toIndex}";
    
    public MoveItemCommand(IList<T> list, int fromIndex, int toIndex)
    {
        _list = list;
        _fromIndex = fromIndex;
        _toIndex = toIndex;
    }
    
    public void Execute()
    {
        var item = _list[_fromIndex];
        _list.RemoveAt(_fromIndex);
        _list.Insert(_toIndex, item);
    }
    
    public void Undo()
    {
        var item = _list[_toIndex];
        _list.RemoveAt(_toIndex);
        _list.Insert(_fromIndex, item);
    }
}

/// <summary>
/// 复合命令（批量操作）
/// </summary>
public class CompositeCommand : ICommand
{
    private readonly List<ICommand> _commands = new();
    private readonly string _description;
    
    public string Description => _description;
    
    public CompositeCommand(string description = "Batch Operation")
    {
        _description = description;
    }
    
    public void Add(ICommand command) => _commands.Add(command);
    
    public void Execute()
    {
        foreach (var cmd in _commands)
            cmd.Execute();
    }
    
    public void Undo()
    {
        // Undo in reverse order
        for (int i = _commands.Count - 1; i >= 0; i--)
            _commands[i].Undo();
    }
}

/// <summary>
/// 插入列表项的命令
/// </summary>
public class InsertItemCommand<T> : ICommand
{
    private readonly IList<T> _list;
    private readonly T _item;
    private readonly int _index;
    private readonly string _description;
    
    public string Description => _description;
    
    public InsertItemCommand(IList<T> list, T item, int index, string description = "Insert Item")
    {
        _list = list;
        _item = item;
        _index = index;
        _description = description;
    }
    
    public void Execute() => _list.Insert(_index, _item);
    public void Undo() => _list.RemoveAt(_index);
}

/// <summary>
/// 删除列表项的命令
/// </summary>
public class DeleteItemCommand<T> : ICommand
{
    private readonly IList<T> _list;
    private readonly T _item;
    private readonly int _index;
    private readonly string _description;
    
    public string Description => _description;
    
    public DeleteItemCommand(IList<T> list, T item, int index, string description = "Delete Item")
    {
        _list = list;
        _item = item;
        _index = index;
        _description = description;
    }
    
    public void Execute() => _list.RemoveAt(_index);
    public void Undo() => _list.Insert(_index, _item);
}
