namespace MkvDoctor.Core.Interfaces;

public interface IFFmpegService
{
    IAsyncEnumerable<FFmpegProgress> RunAsync(
        string arguments,
        CancellationToken ct = default);
}

public record FFmpegProgress(
    double? Progress,
    string? LogLine,
    bool IsCompleted,
    int ExitCode);
