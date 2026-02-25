using System;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.UI.Logging;

public sealed class LogBufferLoggerProvider : ILoggerProvider
{
    private readonly LogBuffer _logBuffer;

    public LogBufferLoggerProvider(LogBuffer logBuffer)
    {
        _logBuffer = logBuffer;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new BufferLogger(categoryName, _logBuffer);
    }

    public void Dispose()
    {
    }

    private sealed class BufferLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogBuffer _logBuffer;
        private readonly bool _verbose;

        public BufferLogger(string categoryName, LogBuffer logBuffer, bool verbose = false)
        {
            _categoryName = categoryName;
            _logBuffer = logBuffer;
            _verbose = verbose;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(msg) && exception is null)
                return;
            
            var ts = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{ts}] [{logLevel}]{(_verbose ? " " + _categoryName : "")}: {msg}";
            if (exception is not null)
                line += $" | {exception.GetType().Name}: {exception.Message}";
            
            _logBuffer.AppendLine(line);
        }
    }
}