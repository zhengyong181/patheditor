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
        string message = $"Error: {ex?.Message}\n\nStack Trace:\n{ex?.StackTrace}";
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
