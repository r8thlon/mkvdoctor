using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MkvDoctor.UI.ViewModels;

public class VideoFileModel : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private string _displayName;
    private string _status = "Pending";
    private string _statusBackground = "Transparent";
    private string _duration = "Probando...";
    private double _progress;
    private string? _outputPath;

    public VideoFileModel(string filePath)
    {
        FilePath = filePath;
        _displayName = Path.GetFileNameWithoutExtension(filePath);
    }

    public string FilePath { get; private set; }

    public string Extension => Path.GetExtension(FilePath);

    public string FileName
    {
        get => _displayName;
        set
        {
            if (value == _displayName) return;
            var dir = Path.GetDirectoryName(FilePath)!;
            var ext = Path.GetExtension(FilePath);
            var newPath = Path.Combine(dir, value + ext);
            if (!string.Equals(newPath, FilePath, StringComparison.OrdinalIgnoreCase))
            {
                File.Move(FilePath, newPath);
                FilePath = newPath;
                _cachedSize = null;
                OnPropertyChanged(nameof(FilePath));
                OnPropertyChanged(nameof(FileSize));
            }
            _displayName = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public string? OutputPath
    {
        get => _outputPath;
        internal set
        {
            _outputPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowOpenLocation));
        }
    }

    public bool ShowOpenLocation => Status == "Done" && OutputPath != null;

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

    public string Duration
    {
        get => _duration;
        set { _duration = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            StatusBackground = value == "Done" ? "#e8f5e9" : "Transparent";
            OnPropertyChanged(nameof(ShowOpenLocation));
        }
    }

    public string StatusBackground
    {
        get => _statusBackground;
        private set { _statusBackground = value; OnPropertyChanged(); }
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
