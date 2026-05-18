using System.Runtime.CompilerServices;
using MkvDoctor.Core.Interfaces;
using MkvDoctor.Core.Models;

namespace MkvDoctor.Core.Services;

public class OperationRunner
{
    private readonly IFFmpegService _ffmpeg;

    public event EventHandler<FileProgressEventArgs>? FileProgress;
    public event EventHandler<FileCompletedEventArgs>? FileCompleted;

    public OperationRunner(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public async IAsyncEnumerable<OperationResult> RunAsync(
        IVideoOperation operation,
        IReadOnlyList<string> inputFiles,
        string outputDirectory,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (operation.CanCombineInputs && inputFiles.Count > 1)
        {
            var output = Path.Combine(outputDirectory, $"concat_{DateTime.Now:yyyyMMdd_HHmmss}.mkv");
            var result = await RunSingleAsync(operation, inputFiles, output, ct);
            OnFileCompleted(inputFiles, result);
            yield return result;
            yield break;
        }

        foreach (var input in inputFiles)
        {
            var ext = operation.OutputExtension;
            var outputName = $"{Path.GetFileNameWithoutExtension(input)}{ext}";
            var output = Path.Combine(outputDirectory, outputName);
            var result = await RunSingleAsync(operation, [input], output, ct);
            OnFileCompleted([input], result);
            yield return result;
        }
    }

    private async Task<OperationResult> RunSingleAsync(
        IVideoOperation operation,
        IReadOnlyList<string> inputs,
        string output,
        CancellationToken ct)
    {
        var progress = new Progress<double>(p => OnFileProgress(inputs, p));
        return await operation.ExecuteAsync(inputs, output, progress, ct);
    }

    private void OnFileProgress(IReadOnlyList<string> inputs, double progress)
    {
        FileProgress?.Invoke(this, new FileProgressEventArgs(inputs, progress));
    }

    private void OnFileCompleted(IReadOnlyList<string> inputs, OperationResult result)
    {
        FileCompleted?.Invoke(this, new FileCompletedEventArgs(inputs, result));
    }
}

public class FileProgressEventArgs : EventArgs
{
    public IReadOnlyList<string> InputFiles { get; }
    public double Progress { get; }
    public FileProgressEventArgs(IReadOnlyList<string> inputs, double progress)
    {
        InputFiles = inputs;
        Progress = progress;
    }
}

public class FileCompletedEventArgs : EventArgs
{
    public IReadOnlyList<string> InputFiles { get; }
    public OperationResult Result { get; }
    public FileCompletedEventArgs(IReadOnlyList<string> inputs, OperationResult result)
    {
        InputFiles = inputs;
        Result = result;
    }
}
