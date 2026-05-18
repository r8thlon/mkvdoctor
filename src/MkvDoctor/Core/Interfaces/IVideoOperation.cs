using MkvDoctor.Core.Models;

namespace MkvDoctor.Core.Interfaces;

public interface IVideoOperation
{
    string DisplayName { get; }
    string OutputExtension { get; }
    bool CanCombineInputs => false;
    Task<OperationResult> ExecuteAsync(
        IReadOnlyList<string> inputPaths,
        string outputPath,
        IProgress<double> progress,
        CancellationToken ct = default);
}
