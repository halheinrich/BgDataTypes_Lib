using Microsoft.Extensions.Logging;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The test loggers consumers' tests share (halheinrich/backgammon#424): what
/// the recording logger keeps and what the throwing logger does.
/// </summary>
public class TestLoggersTests
{
    [Fact]
    public void TheRecordingLogger_KeepsLevelMessageAndExceptionInOrder()
    {
        var logger = new RecordingLogger<TestLoggersTests>();
        var failure = new InvalidOperationException("boom");

        logger.LogInformation("read {Count} rows", 3);
        logger.LogError(failure, "failed on {Name}", "a.xg");

        Assert.Collection(
            logger.Entries,
            e => Assert.Equal(new LogEntry(LogLevel.Information, "read 3 rows", null), e),
            e => Assert.Equal(new LogEntry(LogLevel.Error, "failed on a.xg", failure), e));
    }

    [Fact]
    public void TheRecordingLogger_EnablesEveryLevel_AndKeepsNothingUntilLogged()
    {
        var logger = new RecordingLogger<TestLoggersTests>();

        Assert.All(Enum.GetValues<LogLevel>(), level => Assert.True(logger.IsEnabled(level)));
        Assert.Empty(logger.Entries);
        using (logger.BeginScope("scope"))
        {
        }

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void TheThrowingLogger_ThrowsOnEveryEntry_AndCountsTheAttempts()
    {
        var logger = new ThrowingLogger<TestLoggersTests>();

        var first = Assert.Throws<InvalidOperationException>(() => logger.LogWarning("one"));
        var second = Assert.Throws<InvalidOperationException>(() => logger.LogError("two"));

        Assert.Equal(ThrowingLogger<TestLoggersTests>.Failure, first.Message);
        Assert.Equal(ThrowingLogger<TestLoggersTests>.Failure, second.Message);
        Assert.Equal(2, logger.Attempts);
    }
}
