using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using GCodeWorkbench.UI.Models;
using GCodeWorkbench.UI.Services;

namespace GCodeWorkbench.Services;

/// <summary>
/// WPF 平台的项目服务实现
/// </summary>
public class WpfProjectService : IProjectService
{
    private readonly GCodeParser _parser = new();
    private readonly DxfParserService _dxfParser = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    
    public async Task<GCodeDocument?> OpenProjectAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "G-Code Workbench Project (*.gcmproj)|*.gcmproj|All Files (*.*)|*.*",
            Title = "Open Project"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var data = JsonSerializer.Deserialize<ProjectData>(json, _jsonOptions);
                if (data != null)
                {
                    var doc = data.ToDocument();
                    doc.ProjectPath = dialog.FileName;
                    return doc;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading project: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        return null;
    }
    
    public async Task<GCodeDocument?> ImportGCodeAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "G-Code Files (*.nc;*.gcode;*.tap)|*.nc;*.gcode;*.tap|All Files (*.*)|*.*",
            Title = "Import G-Code"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var content = await File.ReadAllTextAsync(dialog.FileName);
                var doc = _parser.Parse(content);
                doc.FileName = Path.GetFileName(dialog.FileName);
                return doc;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing file: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        return null;
    }
    
    public async Task<string?> PickDxfFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
            Title = "Select DXF File"
        };
        
        if (dialog.ShowDialog() == true)
        {
            return await Task.FromResult(dialog.FileName);
        }
        return null;
    }

    public async Task<GCodeDocument?> ImportDxfAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
            Title = "Import DXF"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var doc = _dxfParser.LoadDxf(dialog.FileName);
                return await Task.FromResult(doc);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing DXF: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        return null;
    }
    
    public async Task<bool> SaveProjectAsync(GCodeDocument document, string? existingPath = null)
    {
        var path = existingPath ?? document.ProjectPath;
        
        if (string.IsNullOrEmpty(path))
        {
            return await SaveProjectAsAsync(document);
        }
        
        try
        {
            var data = document.ToProjectData();
            data.ProjectPath = path;
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(path, json);
            document.ProjectPath = path;
            document.IsDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving project: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return false;
        }
    }
    
    public async Task<bool> SaveProjectAsAsync(GCodeDocument document)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "G-Code Workbench Project (*.gcmproj)|*.gcmproj",
            Title = "Save Project As",
            FileName = Path.GetFileNameWithoutExtension(document.FileName) + ".gcmproj"
        };
        
        if (dialog.ShowDialog() == true)
        {
            return await SaveProjectAsync(document, dialog.FileName);
        }
        return false;
    }
    
    public async Task<bool> ExportGCodeAsync(GCodeDocument document)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "G-Code Files (*.nc)|*.nc|G-Code Files (*.gcode)|*.gcode|All Files (*.*)|*.*",
            Title = "Export G-Code",
            FileName = document.FileName
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var gcode = document.GetGCodeText();
                await File.WriteAllTextAsync(dialog.FileName, gcode);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting G-Code: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }
        return false;
    }
}
