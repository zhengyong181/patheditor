using GCodeWorkbench.UI.Models;

namespace GCodeWorkbench.UI.Services;

/// <summary>
/// 项目服务接口，用于保存和加载项目
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// 打开文件选择对话框并加载项目
    /// </summary>
    Task<GCodeDocument?> OpenProjectAsync();
    
    /// <summary>
    /// 打开文件选择对话框并加载 G代码 文件
    /// </summary>
    Task<GCodeDocument?> ImportGCodeAsync();
    
    /// <summary>
    /// 打开文件选择对话框并返回 DXF 文件路径（不加载）
    /// </summary>
    Task<string?> PickDxfFileAsync();

    /// <summary>
    /// 打开文件选择对话框并加载 DXF 文件
    /// </summary>
    Task<GCodeDocument?> ImportDxfAsync();
    
    /// <summary>
    /// 保存当前项目（如果有路径则直接保存，否则打开保存对话框）
    /// </summary>
    Task<bool> SaveProjectAsync(GCodeDocument document, string? existingPath = null);
    
    /// <summary>
    /// 另存为项目
    /// </summary>
    Task<bool> SaveProjectAsAsync(GCodeDocument document);
    
    /// <summary>
    /// 导出 G代码 文件
    /// </summary>
    Task<bool> ExportGCodeAsync(GCodeDocument document);
}
