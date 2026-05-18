using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MkvDoctor.Core.Interfaces;

namespace MkvDoctor.Core.Services;

public partial class FFmpegService : IFFmpegService
{
    private static readonly Regex TimeRegex = TimePattern();

    public async IAsyncEnumerable<FFmpegProgress> RunAsync(
        string arguments,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            }
        };

        process.Start();

        var duration = TimeSpan.Zero;
        var totalDurationParsed = false;

        await foreach (var line in ReadLinesAsync(process.StandardError, ct))
        {
            if (!totalDurationParsed && line.StartsWith("  Duration: "))
            {
                var match = TimeRegex.Match(line);
                if (match.Success)
                    duration = TimeSpan.Parse(match.Groups[1].Value);
                totalDurationParsed = true;
            }

            var progress = ParseProgress(line, duration);
            yield return new FFmpegProgress(progress, line, false, 0);

            if (ct.IsCancellationRequested)
            {
                process.Kill();
                yield break;
            }
        }

        await process.WaitForExitAsync(ct);
        yield return new FFmpegProgress(null, null, true, process.ExitCode);
    }

    private static double? ParseProgress(string line, TimeSpan duration)
    {
        if (duration == TimeSpan.Zero) return null;

        var match = TimeRegex.Match(line);
        if (!match.Success) return null;

        if (TimeSpan.TryParse(match.Groups[1].Value, out var current))
        {
            if (duration.TotalSeconds > 0)
                return Math.Clamp(current.TotalSeconds / duration.TotalSeconds * 100, 0, 100);
        }

        return null;
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(
        StreamReader reader,
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (await reader.ReadLineAsync(ct) is { } line)
            yield return line;
    }

    [GeneratedRegex(@"(\d{2}):(\d{2}):(\d{2})\.(\d{2})")]
    private static partial Regex TimePattern();
}
