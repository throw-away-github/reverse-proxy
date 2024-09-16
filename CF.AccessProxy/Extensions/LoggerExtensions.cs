namespace CF.AccessProxy.Extensions;

public static partial class LoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Error cleaning up state for key {Key}")]
    public static partial void CouldNotCleanupState(this ILogger logger, Exception ex, string? key);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Error processing task for key {Key}")]
    public static partial void ErrorProcessingTask(this ILogger logger, Exception ex, string? key);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Uncaught exception while processing task")]
    public static partial void UncaughtExceptionWhileProcessingTask(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Failed to revalidate cache entry for key {CacheKey}: {Reason}")]
    public static partial void FailedToRevalidateCacheEntry(this ILogger logger, string cacheKey, string? reason);
}