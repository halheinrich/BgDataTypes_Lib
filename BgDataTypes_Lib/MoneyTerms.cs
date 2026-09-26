namespace BgDataTypes_Lib;

/// <summary>
/// A money session's terms, as its header states them: the Jacoby and beaver
/// rules and the cube limit. One of the two kinds of <see cref="SessionTerms"/>;
/// no length, which belongs to a match. The same rules a record's
/// <see cref="MoneySession"/> carries, before any game is played.
/// </summary>
public sealed class MoneyTerms : SessionTerms
{
    private readonly int _cubeLimit;

    /// <summary>Creates money terms; the members are set by the initializer.</summary>
    public MoneyTerms() : base(SessionKind.Money)
    {
    }

    /// <summary>Whether the Jacoby rule is in force (<see cref="MoneySession.IsJacoby"/>).</summary>
    public required bool IsJacoby { get; init; }

    /// <summary>Whether the beaver rule is in force (<see cref="MoneySession.IsBeaver"/>).</summary>
    public required bool IsBeaver { get; init; }

    /// <summary>The highest value the cube may reach — a positive power of two (<see cref="MoneySession.CubeLimit"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a positive power of two.</exception>
    public required int CubeLimit
    {
        get => _cubeLimit;
        init
        {
            if (!SessionRules.CubeValueHolds(value))
                throw new ArgumentOutOfRangeException(nameof(CubeLimit), value, SessionRules.CubeLimitMessage);
            _cubeLimit = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(SessionTerms? other) =>
        other is MoneyTerms money
        && money.IsJacoby == IsJacoby
        && money.IsBeaver == IsBeaver
        && money.CubeLimit == CubeLimit;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, IsJacoby, IsBeaver, CubeLimit);

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyTerms, TResult> money, Func<MatchTerms, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return money(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyTerms> money, Action<MatchTerms> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        money(this);
    }
}
