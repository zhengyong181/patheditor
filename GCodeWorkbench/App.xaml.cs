using System.Windows;

namespace GCodeWorkbench;

public partial class App : Application
{
    public App()
    {
        // Catch non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        ShowExceptionDialog(e.Exception, "UI Exception");
        e.Handled = true; // Prevent crash if possible
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ShowExceptionDialog(e.ExceptionObject as Exception, "System Exception");
    }

    private void ShowExceptionDialog(Exception? ex, string title)
    {
        string message = GetFullExceptionMessage(ex);
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    private string GetFullExceptionMessage(Exception? ex, int depth = 0)
    {
        if (ex == null) return "Unknown error";
        
        var separator = new string('=', 20);
        var prefix = depth > 0 ? $"\n{separator} Inner Exception ({depth}) {separator}\n" : "";
        var result = $"{prefix}Type: {ex.GetType().FullName}\nMessage: {ex.Message}\n";
        
        if (depth == 0)
        {
            result += $"\nStack Trace:\n{ex.StackTrace}\n";
        }
        
        if (ex.InnerException != null)
        {
            result += GetFullExceptionMessage(ex.InnerException, depth + 1);
        }
        
        return result;
    }
}
