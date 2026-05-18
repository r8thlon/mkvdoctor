using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MkvDoctor.UI.ViewModels;

public class VideoFileModel : INotifyPropertyChanged
{
    private string _status = "Pending";
    private double _progress;

    public string FilePath { get; init; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);

    private long? _cachedSize;
    public string FileSize
    {
        get
        {
            _cachedSize ??= new FileInfo(FilePath).Length;
            return _cachedSize.Value switch
            {
                < 1024 => $"{_cachedSize.Value} B",
                < 1024 * 1024 => $"{_cachedSize.Value / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{_cachedSize.Value / (1024.0 * 1024):F1} MB",
                var b => $"{b / (1024.0 * 1024 * 1024):F2} GB"
            };
        }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
