using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// The session a game of a money session is played in, as the game begins:
/// its terms — the Jacoby and beaver rules and the cube limit
/// (<see cref="MoneyTerms"/>) — and the game's standing, the points player 1
/// and player 2 have each won in the session (<see cref="MoneyStanding"/>),
/// as the header states them. One of the two kinds of
/// <see cref="GameSession"/>; seen from the player on roll at a decision, the
/// same facts are a <see cref="MoneySession"/>.
/// </summary>
/// <remarks>
/// <b>Composed, not restated.</b> The terms and the standing are held as the
/// header states them and hold their own rules; a money standing knows
/// nothing of the terms, so nothing binds the two beyond their kind. Built by
/// <see cref="GameSession.Create"/> alone.
/// </remarks>
public sealed class MoneyGameSession : GameSession
{
    /// <summary>The constructor <see cref="GameSession.Create"/> builds a money game's session through, once the kinds agree.</summary>
    internal MoneyGameSession(MoneyTerms terms, MoneyStanding standing) : base(SessionKind.Money)
    {
        Terms = terms;
        Standing = standing;
    }

    /// <summary>The session's terms: the Jacoby and beaver rules and the cube limit, as its header states them.</summary>
    public MoneyTerms Terms { get; }

    /// <summary>The game's standing: the points player 1 and player 2 have each won in the session before this game.</summary>
    public MoneyStanding Standing { get; }

    /// <inheritdoc/>
    public override bool Equals(GameSession? other) =>
        other is MoneyGameSession money && money.Terms == Terms && money.Standing == Standing;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Terms, Standing);

    /// <summary>
    /// The session for a reader — <c>"money, Jacoby, no beaver, cube limit
    /// 1024, 3-0"</c>, player 1's score first — for a test failure or a log;
    /// not a wire form.
    /// </summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Terms}, {Standing.Score1}-{Standing.Score2}");

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyGameSession, TResult> money, Func<MatchGameSession, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return money(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyGameSession> money, Action<MatchGameSession> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        money(this);
    }
}
