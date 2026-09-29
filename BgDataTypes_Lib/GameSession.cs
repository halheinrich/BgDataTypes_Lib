using System.Numerics;

namespace BgDataTypes_Lib;

/// <summary>
/// The session a game is played in, as the game begins: its terms, and the
/// game's standing, player 1's and player 2's, as its header states them
/// (halheinrich/backgammon#273, Hal's ruling of 2026-09-29). A game's session
/// is one of two types, <see cref="MoneyGameSession"/> and
/// <see cref="MatchGameSession"/>, each composing its kind's terms and its
/// kind's standing — so a caller holding a header's terms and a game's
/// standing tells money from a match, and reads a match's length, away scores
/// and Crawford flag, through one match on the kind, with no kind check of its
/// own and no branch that cannot run.
/// </summary>
/// <remarks>
/// <para>
/// <b>Paired by one operation, with no seat.</b> A header states the terms
/// and the standing apart (<see cref="IMatchInfo.Terms"/>,
/// <see cref="IGameInfo.Standing"/>); <see cref="Create"/> holds the one to
/// the other, and is the one statement of that rule. Both players roll within
/// a game, so a game has no player on roll: a game's session takes no seat and
/// claims none, and its standing stays seat-anchored, as the header states
/// it. A decision's session, the player on roll's view, is
/// <see cref="Session"/>: <see cref="Session.Create"/> pairs through this
/// operation and then turns the standing to the seat on roll, so the two
/// accept exactly the same pairs and refuse the rest alike.
/// </para>
/// <para>
/// <b>A closed pair.</b> The constructors are not reachable outside this
/// library, so these two kinds are the only ones there are, and
/// <see cref="Match{TResult}"/> and <see cref="Switch"/> take one branch per
/// kind: a consumer's match over the kind has no silent fall-through, and a
/// third kind would break every match at compile time.
/// </para>
/// <para>
/// <b>A value.</b> A game's session is immutable, and two are equal exactly
/// when they are the same kind with equal terms and an equal standing, the
/// hash consistent with that equality.
/// </para>
/// <para>
/// <b>Not a wire type.</b> No document embeds a game's session, so it bundles
/// no converter and is not among <see cref="BgDataTypesJsonContext"/>'s
/// roots: its wire debut, and its wire shape, belong to the first document
/// that embeds it. Its terms and its standing are wire types, and are what a
/// producer serializes.
/// </para>
/// </remarks>
public abstract class GameSession : IEquatable<GameSession>, IEqualityOperators<GameSession, GameSession, bool>
{
    /// <summary>Fixes the kind — reachable only from the two kinds in this library.</summary>
    private protected GameSession(SessionKind kind) => Kind = kind;

    /// <summary>
    /// The session's kind, which is its terms' and its standing's — every
    /// <see cref="MoneyGameSession"/> is <see cref="SessionKind.Money"/> and
    /// every <see cref="MatchGameSession"/> is <see cref="SessionKind.Match"/>.
    /// </summary>
    public SessionKind Kind { get; }

    /// <summary>
    /// The session a game is played in: the session's <paramref name="terms"/>
    /// and the game's <paramref name="standing"/>, held to each other — the
    /// standing is of the terms' kind, and a match standing's away scores are
    /// at most the match's length, which the standing does not know. The one
    /// statement of that rule, and the one way code outside this library
    /// builds a game's session; <see cref="Session.Create"/> pairs through it.
    /// No seat is taken, and none is claimed.
    /// </summary>
    /// <param name="terms">The session's terms, as its header states them.</param>
    /// <param name="standing">The game's standing, player 1's and player 2's, as its header states it; of the terms' kind.</param>
    /// <returns>
    /// A <see cref="MoneyGameSession"/> for money terms, a
    /// <see cref="MatchGameSession"/> for a match's, holding the
    /// <paramref name="terms"/> and the <paramref name="standing"/> as given.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="terms"/> or <paramref name="standing"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A match <paramref name="standing"/> has an away score above the terms' length.</exception>
    /// <exception cref="ArgumentException"><paramref name="standing"/> is not of <paramref name="terms"/>' kind.</exception>
    public static GameSession Create(SessionTerms terms, GameStanding standing)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(standing);

        return (terms, standing) switch
        {
            (MoneyTerms money, MoneyStanding scores) => new MoneyGameSession(money, scores),
            (MatchTerms match, MatchStanding aways) => new MatchGameSession(match, aways),
            _ => throw new ArgumentException(SessionRules.StandingKindMessage, nameof(standing)),
        };
    }

    /// <summary>
    /// The result of the branch for this session's kind — one branch per kind,
    /// so a match has no fall-through and a new kind breaks every call at
    /// compile time.
    /// </summary>
    /// <typeparam name="TResult">The type both branches return.</typeparam>
    /// <param name="money">The branch for a <see cref="MoneyGameSession"/>.</param>
    /// <param name="match">The branch for a <see cref="MatchGameSession"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract TResult Match<TResult>(Func<MoneyGameSession, TResult> money, Func<MatchGameSession, TResult> match);

    /// <summary>
    /// Runs the branch for this session's kind — the statement form of
    /// <see cref="Match{TResult}"/>, with the same guarantee.
    /// </summary>
    /// <param name="money">The branch for a <see cref="MoneyGameSession"/>.</param>
    /// <param name="match">The branch for a <see cref="MatchGameSession"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract void Switch(Action<MoneyGameSession> money, Action<MatchGameSession> match);

    /// <summary>
    /// Whether <paramref name="other"/> is a game's session of this kind with
    /// equal terms and an equal standing.
    /// </summary>
    /// <param name="other">The session to compare with; <see langword="null"/> is never equal.</param>
    public abstract bool Equals(GameSession? other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is GameSession other && Equals(other);

    /// <summary>A hash of the terms and the standing, consistent with <see cref="Equals(GameSession?)"/>.</summary>
    public abstract override int GetHashCode();

    /// <summary>Whether the two are equal (<see cref="Equals(GameSession?)"/>); two nulls are equal.</summary>
    public static bool operator ==(GameSession? left, GameSession? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the two are not equal (<see cref="Equals(GameSession?)"/>).</summary>
    public static bool operator !=(GameSession? left, GameSession? right) => !(left == right);
}
