using MkvDoctor.Core.Interfaces;
using MkvDoctor.Core.Models;

namespace MkvDoctor.Core.Operations;

public class ConvertToMkvOperation : IVideoOperation
{
    private readonly IFFmpegService _ffmpeg;

    public ConvertToMkvOperation(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public string DisplayName => "Convert to MKV";
    public string OutputExtension => ".mkv";

    public async Task<OperationResult> ExecuteAsync(
        IReadOnlyList<string> inputPaths,
        string outputPath,
        IProgress<double> progress,
        CancellationToken ct = default)
    {
        var input = inputPaths[0];
        var args = $"-i \"{input}\" -c copy -map 0 -y \"{outputPath}\"";

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
