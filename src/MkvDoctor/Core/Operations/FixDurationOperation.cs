using MkvDoctor.Core.Interfaces;
using MkvDoctor.Core.Models;

namespace MkvDoctor.Core.Operations;

public class FixDurationOperation : IVideoOperation
{
    private readonly IFFmpegService _ffmpeg;

    public FixDurationOperation(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public string DisplayName => "Fix duration";
    public string OutputExtension => ".mkv";

    public async Task<OperationResult> ExecuteAsync(
        IReadOnlyList<string> inputPaths,
        string outputPath,
        IProgress<double> progress,
        CancellationToken ct = default)
    {
        var input = inputPaths[0];
        var args = $"-i \"{input}\" -c copy -map 0 -fflags +genpts -correct_ts_overflow 1 -y \"{outputPath}\"";

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await foreach (var p in _ffmpeg.RunAsync(args, ct))
            {
                if (p.Progress.HasValue)
                    progress.Report(p.Progress.Value);
            }
            sw.Stop();
            return OperationResult.Ok(outputPath, sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }
}
