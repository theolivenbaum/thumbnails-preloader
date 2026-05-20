using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ThumbsPreloader.Core;

namespace ThumbsPreloader.Views;

public partial class ProgressWindow : Window
{
    private readonly string _path;
    private readonly bool _recursive;
    private readonly bool _silent;
    private readonly ObservableCollection<DirectoryProgress> _stack = new();
    private PreloadJob? _job;

    public ProgressWindow() : this("", false, false) { }

    public ProgressWindow(string path, bool recursive, bool silent)
    {
        _path = path;
        _recursive = recursive;
        _silent = silent;
        InitializeComponent();

        var rootText = this.FindControl<TextBlock>("RootText");
        if (rootText != null) rootText.Text = path;
        var hint = this.FindControl<TextBlock>("HintText");
        if (hint != null) hint.Text = recursive
            ? "Recursing into subdirectories. Each nested bar tracks one level of depth."
            : "Top-level directory only.";

        var host = this.FindControl<ItemsControl>("StackHost");
        if (host != null) host.ItemsSource = _stack;

        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        StartJob();
    }

    public void RunHeadless()
    {
        // Silent mode: no UI; just run and exit.
        StartJob();
    }

    private void StartJob()
    {
        _job = new PreloadJob(_path, _recursive, action => Dispatcher.UIThread.Post(action));
        _job.DirectoryEntered += OnDirectoryEntered;
        _job.DirectoryExited += OnDirectoryExited;
        _job.StateChanged += OnStateChanged;

        _ = _job.RunAsync();
    }

    private void OnDirectoryEntered(DirectoryProgress node) => _stack.Add(node);

    private void OnDirectoryExited(DirectoryProgress node) => _stack.Remove(node);

    private void OnStateChanged(PreloadJobState state)
    {
        var status = this.FindControl<TextBlock>("StatusText");
        switch (state)
        {
            case PreloadJobState.Running:
                if (status != null) status.Text = "Preloading thumbnails...";
                break;
            case PreloadJobState.Canceled:
                if (status != null) status.Text = "Canceled.";
                Shutdown();
                break;
            case PreloadJobState.Done:
                if (status != null) status.Text = "Done.";
                Shutdown();
                break;
            case PreloadJobState.Failed:
                if (status != null) status.Text = $"Failed: {_job?.Error}";
                Shutdown();
                break;
        }
    }

    private void Shutdown()
    {
        if (_silent || !IsVisible)
        {
            ExitApp();
        }
        else
        {
            var cancel = this.FindControl<Button>("CancelButton");
            if (cancel != null) cancel.Content = "Close";
            Dispatcher.UIThread.Post(() =>
            {
                // Give the user a moment to see "Done" before auto-closing.
            });
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (_job is { State: PreloadJobState.Done or PreloadJobState.Canceled or PreloadJobState.Failed } || _job == null)
        {
            ExitApp();
            return;
        }
        _job.Cancel();
    }

    private void ExitApp()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            Close();
        }
    }
}
