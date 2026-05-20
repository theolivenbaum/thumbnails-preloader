using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ThumbsPreloader.Core;
using ThumbsPreloader.Views;

namespace ThumbsPreloader;

public partial class App : Application
{
    public Options? StartupOptions { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (StartupOptions is { BadArguments: false, NoArguments: false, Path: { } path })
            {
                if (StartupOptions.SilentMode)
                {
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    var window = new ProgressWindow(path, StartupOptions.IncludeNestedDirectories, silent: true);
                    window.RunHeadless();
                }
                else
                {
                    var window = new ProgressWindow(path, StartupOptions.IncludeNestedDirectories, silent: false);
                    desktop.MainWindow = window;
                }
            }
            else if (StartupOptions is { NoArguments: true })
            {
                desktop.MainWindow = new ProgressWindow(path: null, recursive: false, silent: false);
            }
            else
            {
                desktop.MainWindow = new AboutWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
