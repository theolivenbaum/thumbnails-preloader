using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThumbsPreloader.Core;

public enum PreloadJobState
{
    Idle,
    Running,
    Canceled,
    Done,
    Failed,
}

public sealed class PreloadJob
{
    private readonly string _rootPath;
    private readonly bool _recursive;
    private readonly CancellationTokenSource _cts = new();
    private readonly Action<Action> _dispatch;
    private ThumbnailPreloader? _preloader;

    public PreloadJob(string rootPath, bool recursive, Action<Action> dispatch)
    {
        _rootPath = rootPath;
        _recursive = recursive;
        _dispatch = dispatch;
    }

    public PreloadJobState State { get; private set; } = PreloadJobState.Idle;
    public string? Error { get; private set; }

    public event Action<DirectoryProgress>? DirectoryEntered;
    public event Action<DirectoryProgress>? DirectoryExited;
    public event Action<PreloadJobState>? StateChanged;

    public void Cancel() => _cts.Cancel();

    public Task RunAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                _preloader = new ThumbnailPreloader();
                SetState(PreloadJobState.Running);
                Process(_rootPath, depth: 0);
                if (_cts.IsCancellationRequested)
                {
                    SetState(PreloadJobState.Canceled);
                }
                else
                {
                    SetState(PreloadJobState.Done);
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                SetState(PreloadJobState.Failed);
            }
        });
    }

    private void Process(string directory, int depth)
    {
        if (_cts.IsCancellationRequested) return;

        string[] subdirs;
        string[] files;
        try
        {
            subdirs = _recursive ? Directory.GetDirectories(directory) : Array.Empty<string>();
            files = Directory.GetFiles(directory);
        }
        catch
        {
            return;
        }

        // The non-recursive top-level pass should still touch its subdirectories as items
        // (matching original behavior where they appeared via GetFileSystemEntries).
        string[] topLevelDirs = depth == 0 && !_recursive ? Directory.GetDirectories(directory) : Array.Empty<string>();

        var total = subdirs.Length + files.Length + topLevelDirs.Length;
        if (total == 0) return;

        var node = new DirectoryProgress(directory, depth) { Total = total };
        _dispatch(() => DirectoryEntered?.Invoke(node));

        try
        {
            foreach (var sub in subdirs)
            {
                if (_cts.IsCancellationRequested) return;
                _dispatch(() => node.CurrentItem = Path.GetFileName(sub));
                _preloader?.PreloadThumbnail(sub);
                Process(sub, depth + 1);
                _dispatch(() => node.Processed += 1);
            }

            foreach (var dir in topLevelDirs)
            {
                if (_cts.IsCancellationRequested) return;
                _dispatch(() => node.CurrentItem = Path.GetFileName(dir));
                _preloader?.PreloadThumbnail(dir);
                _dispatch(() => node.Processed += 1);
            }

            foreach (var file in files)
            {
                if (_cts.IsCancellationRequested) return;
                _dispatch(() => node.CurrentItem = Path.GetFileName(file));
                _preloader?.PreloadThumbnail(file);
                _dispatch(() => node.Processed += 1);
            }
        }
        finally
        {
            _dispatch(() => DirectoryExited?.Invoke(node));
        }
    }

    private void SetState(PreloadJobState state)
    {
        State = state;
        _dispatch(() => StateChanged?.Invoke(state));
    }
}
