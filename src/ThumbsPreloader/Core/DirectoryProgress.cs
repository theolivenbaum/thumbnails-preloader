using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ThumbsPreloader.Core;

public sealed class DirectoryProgress : INotifyPropertyChanged
{
    private int _total;
    private int _processed;
    private string? _currentItem;

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

    public string? CurrentItem
    {
        get => _currentItem;
        set => SetField(ref _currentItem, value);
    }

    public double Percent => _total <= 0 ? 0 : (100.0 * _processed) / _total;
    public string Caption => $"{_processed:N0} / {_total:N0}";

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
