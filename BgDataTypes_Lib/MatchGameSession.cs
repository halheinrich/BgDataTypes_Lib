using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// The session a game of a match is played in, as the game begins: the
/// match's terms — its length (<see cref="MatchTerms"/>) — and the game's
/// standing, what player 1 and player 2 each still need and whether this is
/// the Crawford game (<see cref="MatchStanding"/>), as the header states them.
/// One of the two kinds of <see cref="GameSession"/>; seen from the player on
/// roll at a decision, the same facts are a <see cref="MatchSession"/>.
/// </summary>
/// <remarks>
/// <b>Composed, not restated, and well-formed by construction.</b> The terms
/// and the standing are held as the header states them and hold their own
/// rules (the internal <see cref="SessionRules"/>); held to each other, each
/// away score is at most the length — the rule neither states alone, since
/// the standing knows no length. Built by <see cref="GameSession.Create"/>
/// alone.
/// </remarks>
public sealed class MatchGameSession : GameSession
{
    /// <summary>
    /// The constructor <see cref="GameSession.Create"/> builds a match game's
    /// session through, once the kinds agree: it holds the standing's away
    /// scores to the terms' length.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An away score of <paramref name="standing"/> is above the <paramref name="terms"/>' length.</exception>
    internal MatchGameSession(MatchTerms terms, MatchStanding standing) : base(SessionKind.Match)
    {
        if (!SessionRules.NeedsHolds(standing.Away1, terms.Length) || !SessionRules.NeedsHolds(standing.Away2, terms.Length))
            throw new ArgumentOutOfRangeException(nameof(standing), standing, SessionRules.NeedsMessage);
        Terms = terms;
        Standing = standing;
    }

    /// <summary>The match's terms: its length, as its header states it.</summary>
    public MatchTerms Terms { get; }

    /// <summary>
    /// The game's standing: what player 1 and player 2 each still need, each
    /// at most the terms' <see cref="MatchTerms.Length"/>, and whether this is
    /// the Crawford game.
    /// </summary>
    public MatchStanding Standing { get; }

    /// <inheritdoc/>
    public override bool Equals(GameSession? other) =>
        other is MatchGameSession match && match.Terms == Terms && match.Standing == Standing;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Terms, Standing);

    /// <summary>
    /// The session for a reader — <c>"match to 7, 3-away/5-away"</c>, player
    /// 1's first, with <c>", Crawford"</c> in the Crawford game — for a test
    /// failure or a log; not a wire form.
    /// </summary>
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Terms}, {Standing.Away1}-away/{Standing.Away2}-away{(Standing.IsCrawford ? ", Crawford" : "")}");

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyGameSession, TResult> money, Func<MatchGameSession, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return match(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyGameSession> money, Action<MatchGameSession> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        match(this);
    }
}
