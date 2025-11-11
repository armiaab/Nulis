using Microsoft.Extensions.Logging;

namespace Nulis.Services;

public static class LoggerService
{
    private static ILoggerFactory? _loggerFactory;

    public static void Initialize()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddDebug()
                .SetMinimumLevel(LogLevel.Trace);
        });
    }

    public static ILogger<T> GetLogger<T>()
    {
        _loggerFactory ??= LoggerFactory.Create(builder =>
        {
            builder
                .AddDebug()
                .SetMinimumLevel(LogLevel.Trace);
        });

        return _loggerFactory.CreateLogger<T>();
    }
}
