using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly int _parallelism = Math.Max(1, Environment.ProcessorCount / 2);
    private ThreadLocal<ThumbnailPreloader>? _preloaders;

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
                _preloaders = new ThreadLocal<ThumbnailPreloader>(() => new ThumbnailPreloader(), trackAllValues: true);
                SetState(PreloadJobState.Running);
                Process(_rootPath, depth: 0, ancestors: new List<DirectoryProgress>());
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
            finally
            {
                _preloaders?.Dispose();
                _preloaders = null;
            }
        });
    }

    private void Process(string directory, int depth, List<DirectoryProgress> ancestors)
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

        if (_recursive)
        {
            var capturedNode = node;
            var capturedDir = directory;
            Task.Run(() =>
            {
                var subtreeTotal = CountSubtree(capturedDir);
                if (_cts.IsCancellationRequested) return;
                _dispatch(() => capturedNode.SubtreeTotal = subtreeTotal);
            }, _cts.Token);
        }
        else
        {
            // Non-recursive: subtree == immediate, so ETA is available right away.
            var capturedNode = node;
            _dispatch(() => capturedNode.SubtreeTotal = total);
        }

        var chain = new List<DirectoryProgress>(ancestors.Count + 1);
        chain.AddRange(ancestors);
        chain.Add(node);

        try
        {
            foreach (var sub in subdirs)
            {
                if (_cts.IsCancellationRequested) return;
                _dispatch(() => node.CurrentItem = Path.GetFileName(sub));
                _preloaders?.Value?.PreloadThumbnail(sub);
                Process(sub, depth + 1, chain);
                BumpProcessed(chain);
            }

            var parallelItems = topLevelDirs.Concat(files).ToArray();
            if (parallelItems.Length > 0)
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _parallelism,
                    CancellationToken = _cts.Token,
                };

                try
                {
                    Parallel.ForEach(parallelItems, parallelOptions, item =>
                    {
                        if (_cts.IsCancellationRequested) return;
                        _dispatch(() => node.CurrentItem = Path.GetFileName(item));
                        _preloaders?.Value?.PreloadThumbnail(item);
                        BumpProcessed(chain);
                    });
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
        finally
        {
            _dispatch(() => DirectoryExited?.Invoke(node));
        }
    }

    private void BumpProcessed(List<DirectoryProgress> chain)
    {
        _dispatch(() =>
        {
            if (chain.Count > 0) chain[^1].Processed += 1;
            foreach (var ancestor in chain) ancestor.SubtreeProcessed += 1;
        });
    }

    private int CountSubtree(string directory)
    {
        if (_cts.IsCancellationRequested) return 0;
        int count = 0;
        try
        {
            var subdirs = Directory.GetDirectories(directory);
            var files = Directory.GetFiles(directory);
            count = subdirs.Length + files.Length;
            foreach (var sub in subdirs)
            {
                if (_cts.IsCancellationRequested) return count;
                count += CountSubtree(sub);
            }
        }
        catch
        {
        }
        return count;
    }

    private void SetState(PreloadJobState state)
    {
        State = state;
        _dispatch(() => StateChanged?.Invoke(state));
    }
}
