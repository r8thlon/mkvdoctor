using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MkvDoctor.Core.Operations;
using MkvDoctor.Core.Services;

namespace MkvDoctor.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly OperationRunner _runner;
    private int _selectedOperation;
    private string _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private bool _isProcessing;
    private string _globalLog = string.Empty;
    private double _overallProgress;
    private CancellationTokenSource? _cts;

    public ObservableCollection<VideoFileModel> Files { get; } = new();

    public int SelectedOperation
    {
        get => _selectedOperation;
        set { _selectedOperation = value; OnPropertyChanged(); }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set { _outputDirectory = value; OnPropertyChanged(); }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
            ProcessAllCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
            ClearFilesCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanProcess => !IsProcessing && Files.Count > 0;

    public string GlobalLog
    {
        get => _globalLog;
        set { _globalLog = value; OnPropertyChanged(); }
    }

    public double OverallProgress
    {
        get => _overallProgress;
        set { _overallProgress = value; OnPropertyChanged(); }
    }

    public RelayCommand AddFilesCommand { get; }
    public RelayCommand ProcessAllCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearFilesCommand { get; }
    public RelayCommand RemoveFileCommand { get; }

    public MainViewModel(OperationRunner runner)
    {
        _runner = runner;
        _runner.FileProgress += OnFileProgress;
        _runner.FileCompleted += OnFileCompleted;

        AddFilesCommand = new RelayCommand(_ => { }, _ => !IsProcessing);
        ProcessAllCommand = new RelayCommand(_ => ProcessAll(), _ => CanProcess);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsProcessing);
        ClearFilesCommand = new RelayCommand(_ => ClearFiles(), _ => !IsProcessing && Files.Count > 0);
        RemoveFileCommand = new RelayCommand(p =>
        {
            if (p is VideoFileModel file)
            {
                Files.Remove(file);
                ProcessAllCommand.RaiseCanExecuteChanged();
                ClearFilesCommand.RaiseCanExecuteChanged();
            }
        });
    }

    public void AddFiles(string[] paths)
    {
        var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".avi", ".mov", ".mkv", ".webm", ".flv", ".wmv", ".mts", ".m2ts" };

        foreach (var path in paths)
        {
            if (exts.Contains(Path.GetExtension(path)) && Files.All(f => f.FilePath != path))
            {
                var model = new VideoFileModel { FilePath = path };
                Files.Add(model);
                _ = ProbeFileDurationAsync(model);
            }
        }
        ProcessAllCommand.RaiseCanExecuteChanged();
        ClearFilesCommand.RaiseCanExecuteChanged();
    }

    private static async Task ProbeFileDurationAsync(VideoFileModel model)
    {
        var ffmpeg = new FFmpegService();
        var duration = await ffmpeg.ProbeDurationAsync(model.FilePath);
        model.Duration = duration.HasValue
            ? $"{(int)duration.Value.TotalHours:D2}:{duration.Value.Minutes:D2}:{duration.Value.Seconds:D2}"
            : "N/A";
    }

    public void RemoveFileAt(int index)
    {
        if (index >= 0 && index < Files.Count)
        {
            Files.RemoveAt(index);
            ProcessAllCommand.RaiseCanExecuteChanged();
            ClearFilesCommand.RaiseCanExecuteChanged();
        }
    }

    public void ClearFiles()
    {
        Files.Clear();
        ProcessAllCommand.RaiseCanExecuteChanged();
        ClearFilesCommand.RaiseCanExecuteChanged();
    }

    public async void ProcessAll()
    {
        if (IsProcessing || Files.Count == 0) return;

        IsProcessing = true;
        OverallProgress = 0;
        _cts = new CancellationTokenSource();
        GlobalLog = string.Empty;

        var operationType = (OperationType)SelectedOperation;
        var operation = OperationFactory.Create(operationType, new FFmpegService());
        var inputFiles = Files.Select(f => f.FilePath).ToList();
        var outputDir = OutputDirectory;

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        try
        {
            var total = Files.Count;
            var completed = 0;

            await foreach (var result in _runner.RunAsync(operation, inputFiles, outputDir, _cts.Token))
            {
                completed++;
                OverallProgress = (double)completed / total * 100;
                var line = result.Success
                    ? $"OK [{completed}/{total}]: {Path.GetFileName(result.OutputPath)} ({result.Duration.TotalSeconds:F1}s)"
                    : $"FAIL [{completed}/{total}]: {result.ErrorMessage}";
                GlobalLog += line + "\n";
            }
        }
        catch (OperationCanceledException)
        {
            GlobalLog += "Cancelled by user.\n";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public void Cancel() => _cts?.Cancel();

    private void OnFileProgress(object? sender, FileProgressEventArgs e)
    {
        foreach (var file in Files)
        {
            if (e.InputFiles.Contains(file.FilePath))
            {
                file.Progress = e.Progress;
                file.Status = $"{(int)e.Progress}%";
            }
        }
    }

    private void OnFileCompleted(object? sender, FileCompletedEventArgs e)
    {
        foreach (var file in Files)
        {
            if (e.InputFiles.Contains(file.FilePath))
            {
                file.Progress = e.Result.Success ? 100 : 0;
                file.Status = e.Result.Success ? "Done" : "Error";
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
