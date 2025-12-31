using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GCodeWorkbench.UI.Services;
using GCodeWorkbench.Services;

namespace GCodeWorkbench;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddSingleton<IProjectService, WpfProjectService>();
        services.AddSingleton<SvgRenderService>();
        services.AddSingleton<CommandManager>();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        Resources.Add("services", services.BuildServiceProvider());
        
        InitializeComponent();
    }
}
