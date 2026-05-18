using MkvDoctor.Core.Interfaces;

namespace MkvDoctor.Core.Operations;

public enum OperationType
{
    FixDuration,
    Concatenate,
    ConvertToMkv
}

public static class OperationFactory
{
    public static IVideoOperation Create(OperationType type, IFFmpegService ffmpeg) => type switch
    {
        OperationType.FixDuration => new FixDurationOperation(ffmpeg),
        OperationType.Concatenate => new ConcatenateOperation(ffmpeg),
        OperationType.ConvertToMkv => new ConvertToMkvOperation(ffmpeg),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
