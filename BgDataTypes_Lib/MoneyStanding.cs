namespace BgDataTypes_Lib;

/// <summary>
/// Where the players of a money session stand as a game begins: the points
/// each has won in the session (the game header's scores). One of the two
/// kinds of <see cref="GameStanding"/>; no away scores and no Crawford game,
/// which belong to a match. Seen from the player on roll, the same facts are a
/// record's <see cref="MoneySession.OnRollScore"/> and
/// <see cref="MoneySession.OpponentScore"/>.
/// </summary>
public sealed class MoneyStanding : GameStanding
{
    private readonly int _score1;
    private readonly int _score2;

    /// <summary>Creates a money standing; the members are set by the initializer.</summary>
    public MoneyStanding() : base(SessionKind.Money)
    {
    }

    /// <summary>The points player 1 has won in the session before this game; at least 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    public required int Score1
    {
        get => _score1;
        init => _score1 = Checked(value, nameof(Score1));
    }

    /// <summary>The points player 2 has won in the session before this game; at least 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    public required int Score2
    {
        get => _score2;
        init => _score2 = Checked(value, nameof(Score2));
    }

    /// <inheritdoc/>
    public override bool Equals(GameStanding? other) =>
        other is MoneyStanding money && money.Score1 == Score1 && money.Score2 == Score2;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, Score1, Score2);

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyStanding, TResult> money, Func<MatchStanding, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return money(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyStanding> money, Action<MatchStanding> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        money(this);
    }

    private static int Checked(int score, string member) => SessionRules.ScoreHolds(score)
        ? score
        : throw new ArgumentOutOfRangeException(member, score, SessionRules.ScoreMessage);
}
