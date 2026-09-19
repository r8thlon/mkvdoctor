using MkvDoctor.Core.Interfaces;

namespace MkvDoctor.Core.Operations;

public enum OperationType
{
    FixDuration,
    ConvertToMkv,
    Concatenate,
    MuteVideo
}

public static class OperationFactory
{
    public static IVideoOperation Create(OperationType type, IFFmpegService ffmpeg) => type switch
    {
        OperationType.FixDuration => new FixDurationOperation(ffmpeg),
        OperationType.Concatenate => new ConcatenateOperation(ffmpeg),
        OperationType.ConvertToMkv => new ConvertToMkvOperation(ffmpeg),
        OperationType.MuteVideo => new MuteVideoOperation(ffmpeg),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
