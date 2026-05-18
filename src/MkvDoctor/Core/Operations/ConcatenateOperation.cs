using MkvDoctor.Core.Interfaces;
using MkvDoctor.Core.Models;

namespace MkvDoctor.Core.Operations;

public class ConcatenateOperation : IVideoOperation
{
    private readonly IFFmpegService _ffmpeg;

    public ConcatenateOperation(IFFmpegService ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    public string DisplayName => "Concatenate";
    public string OutputExtension => ".mkv";
    public bool CanCombineInputs => true;

    public async Task<OperationResult> ExecuteAsync(
        IReadOnlyList<string> inputPaths,
        string outputPath,
        IProgress<double> progress,
        CancellationToken ct = default)
    {
        var fileList = Path.GetTempFileName();
        try
        {
            await File.WriteAllLinesAsync(fileList,
                inputPaths.Select(f => $"file '{f.Replace("'", "'\\''")}'"), ct);

            var args = $"-f concat -safe 0 -i \"{fileList}\" -c copy -y \"{outputPath}\"";

            var sw = System.Diagnostics.Stopwatch.StartNew();
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
        finally
        {
            if (File.Exists(fileList))
                File.Delete(fileList);
        }
    }
}
