using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ThumbsPreloader.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.0.0";
        var appName = this.FindControl<TextBlock>("AppNameText");
        if (appName != null) appName.Text = $"ThumbsPreloader {version}";
        var updateText = this.FindControl<TextBlock>("UpdateText");
        if (updateText != null) updateText.Text = "Modern Avalonia .NET 10 build";
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnLicenseClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/theolivenbaum/thumbnails-preloader/blob/master/LICENSE")
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // Ignored.
        }
    }
}
