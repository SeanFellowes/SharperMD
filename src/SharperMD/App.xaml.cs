using System.Windows;
using SharperMD.Models;
using SharperMD.Services;

namespace SharperMD;

public partial class App : Application
{
    public static string? StartupFilePath { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Handle command line arguments
        if (e.Args.Length > 0 && !string.IsNullOrEmpty(e.Args[0]))
        {
            StartupFilePath = e.Args[0];
        }

        // Initialize theme based on settings
        var settings = AppSettings.Load();
        var themeService = new ThemeService();
        themeService.Initialize(settings.Theme);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
