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
    private int _selectedTabIndex;
    private bool _isProcessing;
    private bool _updatingSelectAll;
    private string _globalLog = string.Empty;
    private double _overallProgress;
    private string? _mixOutputPath;
    private CancellationTokenSource? _cts;

    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".avi", ".mov", ".mkv", ".webm", ".flv", ".wmv", ".mts", ".m2ts" };

    // ── Process tab ──────────────────────────────────────────────

    public ObservableCollection<VideoFileModel> Files { get; } = new();

    public int SelectedOperation
    {
        get => _selectedOperation;
        set { _selectedOperation = value; OnPropertyChanged(); }
    }

    public bool SelectAll => Files.Count > 0 && Files.All(f => f.IsSelected);

    public bool CanProcess => !IsProcessing && Files.Any(f => f.IsSelected);

    public void ToggleSelectAll()
    {
        var newVal = !SelectAll;
        _updatingSelectAll = true;
        foreach (var file in Files)
            file.IsSelected = newVal;
        _updatingSelectAll = false;
        OnPropertyChanged(nameof(SelectAll));
    }

    public void AddFiles(string[] paths)
    {
        foreach (var path in paths)
        {
            if (!VideoExts.Contains(Path.GetExtension(path)) || Files.Any(f => f.FilePath == path))
                continue;

            var model = new VideoFileModel(path);
            model.PropertyChanged += OnFilePropertyChanged;
            var idx = Files
                .Select((f, i) => new { f, i })
                .FirstOrDefault(x => string.Compare(x.f.FileName, model.FileName, StringComparison.OrdinalIgnoreCase) > 0)
                ?.i ?? Files.Count;
            Files.Insert(idx, model);
            _ = ProbeFileDurationAsync(model);
        }
        ProcessCommand.RaiseCanExecuteChanged();
        ClearFilesCommand.RaiseCanExecuteChanged();
    }

    public void RemoveFileAt(int index)
    {
        if (index < 0 || index >= Files.Count) return;
        Files.RemoveAt(index);
        ProcessCommand.RaiseCanExecuteChanged();
        ClearFilesCommand.RaiseCanExecuteChanged();
    }

    public void ClearFiles()
    {
        Files.Clear();
        ProcessCommand.RaiseCanExecuteChanged();
        ClearFilesCommand.RaiseCanExecuteChanged();
    }

    public async void Process()
    {
        var selectedFiles = Files.Where(f => f.IsSelected).ToList();
        if (IsProcessing || selectedFiles.Count == 0) return;

        IsProcessing = true;
        OverallProgress = 0;
        _cts = new CancellationTokenSource();
        GlobalLog = string.Empty;

        var operationType = (OperationType)SelectedOperation;
        var operation = OperationFactory.Create(operationType, new FFmpegService());
        var inputFiles = selectedFiles.Select(f => f.FilePath).ToList();

        try
        {
            var total = selectedFiles.Count;
            var completed = 0;

            await foreach (var result in _runner.RunAsync(operation, inputFiles, _cts.Token))
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

    // ── Mix tab ──────────────────────────────────────────────────

    public ObservableCollection<VideoFileModel> MixFiles { get; } = new();

    public bool CanMix => !IsProcessing && MixFiles.Count > 0;

    public string? MixOutputPath
    {
        get => _mixOutputPath;
        set
        {
            _mixOutputPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMixOutput));
        }
    }

    public bool HasMixOutput => MixOutputPath != null;

    public void AddMixFiles(string[] paths)
    {
        foreach (var path in paths)
        {
            if (!VideoExts.Contains(Path.GetExtension(path)) || MixFiles.Any(f => f.FilePath == path))
                continue;

            var model = new VideoFileModel(path);
            var idx = MixFiles
                .Select((f, i) => new { f, i })
                .FirstOrDefault(x => string.Compare(x.f.FileName, model.FileName, StringComparison.OrdinalIgnoreCase) > 0)
                ?.i ?? MixFiles.Count;
            MixFiles.Insert(idx, model);
            _ = ProbeFileDurationAsync(model);
        }
        MixCommand.RaiseCanExecuteChanged();
        MixClearCommand.RaiseCanExecuteChanged();
    }

    public void MixRemoveFileAt(int index)
    {
        if (index < 0 || index >= MixFiles.Count) return;
        MixFiles.RemoveAt(index);
        MixCommand.RaiseCanExecuteChanged();
        MixClearCommand.RaiseCanExecuteChanged();
    }

    public void MixClearFiles()
    {
        MixFiles.Clear();
        MixOutputPath = null;
        MixCommand.RaiseCanExecuteChanged();
        MixClearCommand.RaiseCanExecuteChanged();
    }

    public async void Mix()
    {
        if (IsProcessing || MixFiles.Count == 0) return;

        IsProcessing = true;
        OverallProgress = 0;
        _cts = new CancellationTokenSource();
        GlobalLog = string.Empty;
        MixOutputPath = null;

        var operation = new ConcatenateOperation(new FFmpegService());
        var inputFiles = MixFiles.Select(f => f.FilePath).ToList();

        try
        {
            await foreach (var result in _runner.RunAsync(operation, inputFiles, _cts.Token))
            {
                OverallProgress = 100;
                var line = result.Success
                    ? $"OK: {Path.GetFileName(result.OutputPath)} ({result.Duration.TotalSeconds:F1}s)"
                    : $"FAIL: {result.ErrorMessage}";
                GlobalLog += line + "\n";
                if (result.Success)
                    MixOutputPath = result.OutputPath;
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

    public void MixMoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= MixFiles.Count) return;
        if (toIndex < 0 || toIndex >= MixFiles.Count) return;
        if (fromIndex == toIndex) return;
        MixFiles.Move(fromIndex, toIndex);
    }

    // ── Shared ───────────────────────────────────────────────────

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
            ProcessCommand.RaiseCanExecuteChanged();
            MixCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
            ClearFilesCommand.RaiseCanExecuteChanged();
            MixClearCommand.RaiseCanExecuteChanged();
        }
    }

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

    public void Cancel() => _cts?.Cancel();

    // ── Commands ─────────────────────────────────────────────────

    public RelayCommand AddFilesCommand { get; }
    public RelayCommand ProcessCommand { get; }
    public RelayCommand MixCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearFilesCommand { get; }
    public RelayCommand RemoveFileCommand { get; }
    public RelayCommand MixClearCommand { get; }
    public RelayCommand MixRemoveFileCommand { get; }

    public MainViewModel(OperationRunner runner)
    {
        _runner = runner;
        _runner.FileProgress += OnFileProgress;
        _runner.FileCompleted += OnFileCompleted;

        AddFilesCommand = new RelayCommand(_ => { }, _ => !IsProcessing);
        ProcessCommand = new RelayCommand(_ => Process(), _ => CanProcess);
        MixCommand = new RelayCommand(_ => Mix(), _ => CanMix);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => IsProcessing);
        ClearFilesCommand = new RelayCommand(_ => ClearFiles(), _ => !IsProcessing && Files.Count > 0);
        RemoveFileCommand = new RelayCommand(p =>
        {
            if (p is VideoFileModel file)
            {
                Files.Remove(file);
                ProcessCommand.RaiseCanExecuteChanged();
                ClearFilesCommand.RaiseCanExecuteChanged();
            }
        });
        MixClearCommand = new RelayCommand(_ => MixClearFiles(), _ => !IsProcessing && MixFiles.Count > 0);
        MixRemoveFileCommand = new RelayCommand(p =>
        {
            if (p is VideoFileModel file)
            {
                MixFiles.Remove(file);
                MixCommand.RaiseCanExecuteChanged();
                MixClearCommand.RaiseCanExecuteChanged();
            }
        });
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static async Task ProbeFileDurationAsync(VideoFileModel model)
    {
        var ffmpeg = new FFmpegService();
        var duration = await ffmpeg.ProbeDurationAsync(model.FilePath);
        model.Duration = duration.HasValue
            ? $"{(int)duration.Value.TotalHours:D2}:{duration.Value.Minutes:D2}:{duration.Value.Seconds:D2}"
            : "N/A";
    }

    private void OnFilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoFileModel.IsSelected))
        {
            ProcessCommand.RaiseCanExecuteChanged();
            if (!_updatingSelectAll)
                OnPropertyChanged(nameof(SelectAll));
        }
    }

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
                file.OutputPath = e.Result.Success ? e.Result.OutputPath : null;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
