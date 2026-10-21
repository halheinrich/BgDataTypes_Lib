using Microsoft.Extensions.Logging;

namespace BgDataTypes_Lib.TestSupport;

/// <summary>
/// An <see cref="ILogger{T}"/> whose every entry throws, as a failing logging
/// provider does — to pin that logging what happened stops nothing. It counts
/// the entries attempted, so a test can tell the logger was reached.
/// </summary>
/// <typeparam name="T">The category the logger stands in for.</typeparam>
public sealed class ThrowingLogger<T> : ILogger<T>
{
    /// <summary>The message every entry fails with.</summary>
    public const string Failure = "The logging provider failed.";

    /// <summary>How many entries were attempted.</summary>
    public int Attempts { get; private set; }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always, with <see cref="Failure"/>.</exception>
    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Attempts++;
        throw new InvalidOperationException(Failure);
    }
}
