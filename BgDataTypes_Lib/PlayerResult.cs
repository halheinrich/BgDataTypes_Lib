using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// The player's result on a decision: which case it is
/// (<see cref="Kind"/>), and the error of the two cases that have one. What
/// the ranking's <see cref="RankedPlays.PlayerResult"/> and the filter view's
/// <see cref="IDecisionFilterData.PlayerResult"/> answer, so that "not
/// scored" and "not recorded" are told apart by type, never by one
/// <see langword="null"/> (the umbrella's third-round ruling on the records
/// leg of halheinrich/backgammon#273).
/// </summary>
/// <remarks>
/// <para>
/// <b>The error is read only where it exists.</b> <see cref="TryGetError"/>
/// yields it for <see cref="PlayerResultKind.Scored"/> and
/// <see cref="PlayerResultKind.Unlisted"/> and nothing for the other two, and
/// <see cref="Match{TResult}"/> hands it to exactly those branches; no member
/// reads a case without an error as one. The filter's "erred by more than
/// x" is <c>result.TryGetError(out var error) &amp;&amp; error &gt; x</c>:
/// scored and unlisted moves above x, never a move the ranking does not
/// score, never a decision with no move recorded.
/// </para>
/// <para>
/// <b>Built by case.</b> <see cref="NotRecorded"/> and
/// <see cref="NotScored"/> carry nothing; <see cref="Scored"/> refuses an
/// error that is negative or not finite — a scored error never is;
/// <see cref="Unlisted"/> takes the analyser's number as stated. The
/// <c>default</c> value is <see cref="NotRecorded"/>. A value type: reading a
/// result allocates nothing.
/// </para>
/// </remarks>
public readonly struct PlayerResult : IEquatable<PlayerResult>
{
    private readonly double _error;

    private PlayerResult(PlayerResultKind kind, double error)
    {
        Kind = kind;
        _error = error;
    }

    /// <summary>Which case the result is.</summary>
    public PlayerResultKind Kind { get; }

    /// <summary>No player move is recorded; no error.</summary>
    public static PlayerResult NotRecorded => default;

    /// <summary>A listed move the ranking does not score; no error.</summary>
    public static PlayerResult NotScored => new(PlayerResultKind.NotScored, 0.0);

    /// <summary>A move this library scores, and its error.</summary>
    /// <param name="error">The move's error: finite, never negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="error"/> is negative or not finite.</exception>
    public static PlayerResult Scored(double error)
    {
        if (!double.IsFinite(error) || error < 0.0)
            throw new ArgumentOutOfRangeException(nameof(error), error, "A scored move's error is a finite number, never negative.");
        return new(PlayerResultKind.Scored, error);
    }

    /// <summary>A move the record does not state, and the analyser's error for it, as stated.</summary>
    /// <param name="error">The analyser's error.</param>
    public static PlayerResult Unlisted(double error) => new(PlayerResultKind.Unlisted, error);

    /// <summary>
    /// The error, for a <see cref="PlayerResultKind.Scored"/> or
    /// <see cref="PlayerResultKind.Unlisted"/> result.
    /// </summary>
    /// <param name="error">The error when the result has one; otherwise 0, which means nothing.</param>
    /// <returns><see langword="true"/> exactly when the result has an error.</returns>
    public bool TryGetError(out double error)
    {
        bool has = Kind is PlayerResultKind.Scored or PlayerResultKind.Unlisted;
        error = has ? _error : 0.0;
        return has;
    }

    /// <summary>Runs the branch of the result's case, handing the error to the two cases that have one.</summary>
    /// <exception cref="ArgumentNullException">A branch is <see langword="null"/>.</exception>
    public TResult Match<TResult>(
        Func<TResult> notRecorded, Func<TResult> notScored, Func<double, TResult> scored, Func<double, TResult> unlisted)
    {
        ArgumentNullException.ThrowIfNull(notRecorded);
        ArgumentNullException.ThrowIfNull(notScored);
        ArgumentNullException.ThrowIfNull(scored);
        ArgumentNullException.ThrowIfNull(unlisted);
        return Kind switch
        {
            PlayerResultKind.NotScored => notScored(),
            PlayerResultKind.Scored => scored(_error),
            PlayerResultKind.Unlisted => unlisted(_error),
            _ => notRecorded(),
        };
    }

    /// <inheritdoc/>
    public bool Equals(PlayerResult other) => Kind == other.Kind && _error.Equals(other._error);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PlayerResult other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, _error);

    /// <summary>Whether two results are the same case with the same error.</summary>
    public static bool operator ==(PlayerResult left, PlayerResult right) => left.Equals(right);

    /// <summary>Whether two results differ in case or error.</summary>
    public static bool operator !=(PlayerResult left, PlayerResult right) => !left.Equals(right);

    /// <summary>The case, and the error where there is one, e.g. <c>"Scored 0.032"</c>, <c>"NotScored"</c>.</summary>
    public override string ToString() =>
        TryGetError(out double error)
            ? string.Create(CultureInfo.InvariantCulture, $"{Kind} {error}")
            : Kind.ToString();
}
