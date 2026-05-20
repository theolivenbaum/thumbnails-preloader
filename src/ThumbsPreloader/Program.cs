using System;
using Avalonia;
using ThumbsPreloader.Core;

namespace ThumbsPreloader;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var options = new Options(args);

        return BuildAvaloniaApp()
            .AfterSetup(app =>
            {
                if (app.Instance is App a)
                {
                    a.StartupOptions = options;
                }
            })
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
