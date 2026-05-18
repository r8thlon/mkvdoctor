namespace MkvDoctor.Core.Models;

public class OperationResult
{
    public bool Success { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }

    public static OperationResult Ok(string outputPath, TimeSpan duration) =>
        new() { Success = true, OutputPath = outputPath, Duration = duration };

    public static OperationResult Fail(string error) =>
        new() { Success = false, ErrorMessage = error };
}
