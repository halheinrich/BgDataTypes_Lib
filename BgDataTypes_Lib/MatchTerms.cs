namespace BgDataTypes_Lib;

/// <summary>
/// A match's terms, as its header states them: its length. One of the two
/// kinds of <see cref="SessionTerms"/>; no money rule, which a match has not.
/// A producer states the real length — XG's raw sentinel for a money session
/// (99999) is a money session's <see cref="MoneyTerms"/>, never a match.
/// </summary>
public sealed class MatchTerms : SessionTerms
{
    private readonly int _length;

    /// <summary>Creates match terms; the member is set by the initializer.</summary>
    public MatchTerms() : base(SessionKind.Match)
    {
    }

    /// <summary>The match's length: the points a player needs to win it, at least 1 (<see cref="MatchSession.Length"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1.</exception>
    public required int Length
    {
        get => _length;
        init
        {
            if (!SessionRules.LengthHolds(value))
                throw new ArgumentOutOfRangeException(nameof(Length), value, SessionRules.LengthMessage);
            _length = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(SessionTerms? other) => other is MatchTerms match && match.Length == Length;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, Length);

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyTerms, TResult> money, Func<MatchTerms, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return match(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyTerms> money, Action<MatchTerms> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        match(this);
    }
}
