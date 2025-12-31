using System;
using System.IO;
using System.Windows;

namespace GCodeWorkbench;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 写日志文件到程序目录
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_log.txt");
        
        try
        {
            File.AppendAllText(logPath, $"\n\n========== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========\n");
            File.AppendAllText(logPath, $"Starting application...\n");
            File.AppendAllText(logPath, $".NET Version: {Environment.Version}\n");
            File.AppendAllText(logPath, $"OS: {Environment.OSVersion}\n");
            File.AppendAllText(logPath, $"64-bit OS: {Environment.Is64BitOperatingSystem}\n");
            File.AppendAllText(logPath, $"64-bit Process: {Environment.Is64BitProcess}\n");
            File.AppendAllText(logPath, $"Current Directory: {Environment.CurrentDirectory}\n");
            File.AppendAllText(logPath, $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}\n");
            
            // 检查 WebView2 是否可用
            try
            {
                var webView2Version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                File.AppendAllText(logPath, $"WebView2 Version: {webView2Version}\n");
            }
            catch (Exception webViewEx)
            {
                File.AppendAllText(logPath, $"WebView2 Check Failed: {webViewEx.Message}\n");
            }
            
            File.AppendAllText(logPath, "Creating App instance...\n");
            
            var app = new App();
            app.InitializeComponent();
            
            File.AppendAllText(logPath, "App initialized, running...\n");
            
            app.Run();
        }
        catch (Exception ex)
        {
            var errorMsg = GetFullException(ex);
            File.AppendAllText(logPath, $"\n!!! FATAL ERROR !!!\n{errorMsg}\n");
            
            MessageBox.Show(
                $"程序启动失败！\n\n{errorMsg}\n\n详细日志已保存到:\n{logPath}",
                "启动错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
    
    private static string GetFullException(Exception ex, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var result = $"{indent}Type: {ex.GetType().FullName}\n{indent}Message: {ex.Message}\n";
        
        if (depth == 0)
        {
            result += $"{indent}StackTrace:\n{ex.StackTrace}\n";
        }
        
        if (ex.InnerException != null)
        {
            result += $"\n{indent}--- Inner Exception ---\n";
            result += GetFullException(ex.InnerException, depth + 1);
        }
        
        return result;
    }
}
