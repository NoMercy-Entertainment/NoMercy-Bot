using Microsoft.Extensions.Logging;
using NoMercyBot.Globals.SystemCalls;
using Serilog.Events;

namespace NoMercyBot.Services;

public class CustomLogger<T> : ILogger<T>
{
    private readonly string _categoryName;

    public CustomLogger()
    {
        _categoryName = typeof(T).Name;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        LogEventLevel level = ConvertLogLevel(logLevel);
        
        // Route logs to appropriate category based on the class name
        if (_categoryName.Contains("Discord"))
            Logger.Discord($"{message}", level);
        else if (_categoryName.Contains("Twitch"))
            Logger.Twitch($"{message}", level);
        else if (_categoryName.Contains("Spotify"))
            Logger.Spotify($"{message}", level);
        else if (_categoryName.Contains("Http"))
            Logger.Http($"{message}", level);
        else if (_categoryName.Contains("Service"))
            Logger.Service($"{message}", level);
        // else
        //     Logger.System($"{message}", level);
            // Logger.System($"[{_categoryName}] {message}", level);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    
    private LogEventLevel ConvertLogLevel(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
}