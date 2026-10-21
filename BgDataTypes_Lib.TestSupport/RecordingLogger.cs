using Microsoft.Extensions.Logging;

namespace BgDataTypes_Lib.TestSupport;

/// <summary>
/// A minimal <see cref="ILogger{T}"/> that keeps each entry's level, fully
/// formatted message and exception — enough to assert what a component
/// logged, or that it logged nothing of a kind. Every level is enabled and a
/// scope does nothing.
/// </summary>
/// <typeparam name="T">The category the logger stands in for.</typeparam>
public sealed class RecordingLogger<T> : ILogger<T>
{
    /// <summary>Every entry logged, in the order it was logged.</summary>
    public List<LogEntry> Entries { get; } = [];

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}

/// <summary>One entry kept by <see cref="RecordingLogger{T}"/>.</summary>
/// <param name="Level">The level the entry was logged at.</param>
/// <param name="Message">The message, fully formatted.</param>
/// <param name="Exception">The exception logged with the entry, if any.</param>
public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
