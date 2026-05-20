using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ThumbsPreloader.Core;

public sealed class DirectoryProgress : INotifyPropertyChanged
{
    private int _total;
    private int _processed;
    private int? _subtreeTotal;
    private int _subtreeProcessed;
    private string? _currentItem;
    private readonly DateTime _startedAt = DateTime.UtcNow;

    public DirectoryProgress(string path, int depth)
    {
        Path = path;
        Depth = depth;
    }

    public string Path { get; }
    public int Depth { get; }

    public int Total
    {
        get => _total;
        set => SetField(ref _total, value);
    }

    public int Processed
    {
        get => _processed;
        set
        {
            if (SetField(ref _processed, value))
            {
                OnPropertyChanged(nameof(Percent));
                OnPropertyChanged(nameof(Caption));
            }
        }
    }

    public int? SubtreeTotal
    {
        get => _subtreeTotal;
        set
        {
            if (_subtreeTotal != value)
            {
                _subtreeTotal = value;
                OnPropertyChanged(nameof(SubtreeTotal));
                OnPropertyChanged(nameof(EtaText));
            }
        }
    }

    public int SubtreeProcessed
    {
        get => _subtreeProcessed;
        set
        {
            if (SetField(ref _subtreeProcessed, value))
            {
                OnPropertyChanged(nameof(EtaText));
            }
        }
    }

    public string? CurrentItem
    {
        get => _currentItem;
        set => SetField(ref _currentItem, value);
    }

    public double Percent => _total <= 0 ? 0 : (100.0 * _processed) / _total;
    public string Caption => $"{_processed:N0} / {_total:N0}";

    public TimeSpan? Eta
    {
        get
        {
            if (_subtreeTotal is not { } total) return null;
            if (_subtreeProcessed <= 0 || total <= 0) return null;
            var remaining = total - _subtreeProcessed;
            if (remaining <= 0) return TimeSpan.Zero;
            var elapsed = DateTime.UtcNow - _startedAt;
            if (elapsed <= TimeSpan.Zero) return null;
            var ticksPerItem = elapsed.Ticks / (double)_subtreeProcessed;
            return TimeSpan.FromTicks((long)(ticksPerItem * remaining));
        }
    }

    public string EtaText
    {
        get
        {
            if (_subtreeTotal is null) return "ETA --";
            var eta = Eta;
            return eta is null ? "ETA --" : $"ETA {FormatEta(eta.Value)}";
        }
    }

    private static string FormatEta(TimeSpan span)
    {
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1) return $"{span.Minutes}m {span.Seconds:00}s";
        return $"{Math.Max(0, span.Seconds)}s";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
