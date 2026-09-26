using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The session a decision is played in, as it stands at the decision: its
/// terms, and the game's standing seen from the player on roll
/// (halheinrich/backgammon#273). A session is one of two types,
/// <see cref="MoneySession"/> and <see cref="MatchSession"/>, each composing
/// its kind's terms (<see cref="MoneyTerms"/>, <see cref="MatchTerms"/>) and
/// stating only the oriented standing itself, so code that reads one kind's
/// fact off the other does not compile and no member of either holds a value
/// standing for "not applicable". Money used to be spelled with match
/// stand-ins — a match length of 0, away scores of 0, a Crawford flag always
/// false — beside a Jacoby fact that meant nothing in a match; none of them is
/// expressible now.
/// </summary>
/// <remarks>
/// <para>
/// <b>Built from its header, by one operation.</b> A header states the
/// session's terms (<see cref="SessionTerms"/>, <see cref="IMatchInfo.Terms"/>)
/// and the game's standing seat by seat (<see cref="GameStanding"/>,
/// <see cref="IGameInfo.Standing"/>); a record states the standing from the
/// player on roll's side and holds no seat. <see cref="Create"/> is the one
/// place a standing is turned to the player on roll — code outside this
/// library cannot build a session any other way, so no producer orients the
/// scores itself. The three scopes share <see cref="SessionKind"/> and the
/// rules of the internal <see cref="SessionRules"/>, stated once.
/// </para>
/// <para>
/// <b>A closed pair.</b> The constructors are not reachable outside this
/// library, so these two kinds are the only ones there are, and
/// <see cref="Match{TResult}"/> and <see cref="Switch"/> take one branch per
/// kind: a consumer's match over the kind has no silent fall-through, and a
/// third kind would break every match at compile time.
/// </para>
/// <para>
/// <b>On the wire the kind is stated once, in the terms</b>: a session is
/// <c>{"Terms":{"Kind":…,…},…}</c> and the oriented standing, never a kind of
/// its own beside its terms' (a document stating one reads with it ignored,
/// as every derived member is). <see cref="SessionJsonConverter"/> finds the
/// terms and their kind wherever they sit, refuses a document without exactly
/// one of each, and hands the whole document to that kind's generated
/// contract — the one dispatch (the internal <see cref="KindDispatch"/>).
/// Every refusal — a missing, duplicated or unknown kind, terms stated twice
/// or not an object, a member of the other kind (each kind disallows unmapped
/// members, and so do its terms), a missing member, a rule of the kind
/// broken — is a <see cref="JsonException"/> on both paths, whether the
/// session is read as <see cref="Session"/>, as its own kind, or inside a
/// record. Its members are required through <see cref="JsonRequiredAttribute"/>,
/// since only this library sets them.
/// </para>
/// <para>
/// <b>A value.</b> A session is immutable, and two sessions are equal exactly
/// when they are the same kind with equal terms and the same standing — so a
/// record's view, its row and the row read back from JSON all state one
/// session, whatever instance each holds. The hash is consistent with that
/// equality.
/// </para>
/// </remarks>
[JsonConverter(typeof(SessionJsonConverter))]
public abstract class Session : IEquatable<Session>, IEqualityOperators<Session, Session, bool>
{
    // True for a session being read from a document: set only by the
    // serializer's constructor, before any member is set, so each kind's rules
    // refuse a breach as a JsonException rather than the guard's own
    // ArgumentException (DocumentRefusal).
    private readonly bool _read;

    /// <summary>
    /// Fixes the kind, and whether the session is being read from a
    /// document — reachable only from the two kinds in this library: their
    /// constructor for <see cref="Create"/>, and their serializer constructor.
    /// </summary>
    private protected Session(SessionKind kind, bool read)
    {
        Kind = kind;
        _read = read;
    }

    /// <summary>
    /// The session's kind, which is its terms' — every
    /// <see cref="MoneySession"/> is <see cref="SessionKind.Money"/> and every
    /// <see cref="MatchSession"/> is <see cref="SessionKind.Match"/>. Not a wire
    /// member of the session's own: a document states it once, in the terms.
    /// </summary>
    [JsonIgnore]
    public SessionKind Kind { get; }

    /// <summary>
    /// The session of a game, seen from the player on roll: the session's
    /// <paramref name="terms"/>, and the game's seat-anchored
    /// <paramref name="standing"/> turned to the <paramref name="onRoll"/>
    /// seat — the seat on roll's score or away score becomes the player on
    /// roll's, the other seat's the opponent's, and a match's Crawford flag is
    /// the game's. The one orientation rule, and the one way code outside this
    /// library builds a session.
    /// </summary>
    /// <param name="terms">The session's terms, as its header states them.</param>
    /// <param name="standing">The game's standing, player 1's and player 2's, as its header states it; of the terms' kind.</param>
    /// <param name="onRoll">The seat of the player on roll at the decision.</param>
    /// <returns>A <see cref="MoneySession"/> for money terms, a <see cref="MatchSession"/> for a match's.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="terms"/> or <paramref name="standing"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="onRoll"/> is not a defined seat; or a match
    /// <paramref name="standing"/> has an away score above the terms' length.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="standing"/> is not of <paramref name="terms"/>' kind.</exception>
    public static Session Create(SessionTerms terms, GameStanding standing, Seat onRoll)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(standing);
        if (!Enum.IsDefined(onRoll))
            throw new ArgumentOutOfRangeException(nameof(onRoll), onRoll, SessionRules.SeatMessage);

        switch (terms, standing)
        {
            case (MoneyTerms money, MoneyStanding scores):
            {
                var (onRollScore, opponentScore) = Oriented(scores.Score1, scores.Score2, onRoll);
                return new MoneySession { Terms = money, OnRollScore = onRollScore, OpponentScore = opponentScore };
            }
            case (MatchTerms match, MatchStanding aways):
            {
                var (onRollNeeds, opponentNeeds) = Oriented(aways.Away1, aways.Away2, onRoll);
                if (!SessionRules.NeedsHolds(onRollNeeds, match.Length) || !SessionRules.NeedsHolds(opponentNeeds, match.Length))
                    throw new ArgumentOutOfRangeException(nameof(standing), standing, SessionRules.NeedsMessage);
                return new MatchSession
                {
                    Terms = match,
                    OnRollNeeds = onRollNeeds,
                    OpponentNeeds = opponentNeeds,
                    IsCrawford = aways.IsCrawford,
                };
            }
            default:
                throw new ArgumentException(SessionRules.StandingKindMessage, nameof(standing));
        }
    }

    /// <summary>
    /// A seat-anchored pair, player 1's first, as the player on roll's and the
    /// opponent's: the orientation <see cref="Create"/> applies to either kind.
    /// </summary>
    private static (int OnRoll, int Opponent) Oriented(int player1, int player2, Seat onRoll) =>
        onRoll == Seat.Player1 ? (player1, player2) : (player2, player1);

    /// <summary>
    /// The result of the branch for this session's kind — one branch per kind,
    /// so a match has no fall-through and a new kind breaks every call at
    /// compile time.
    /// </summary>
    /// <typeparam name="TResult">The type both branches return.</typeparam>
    /// <param name="money">The branch for a <see cref="MoneySession"/>.</param>
    /// <param name="match">The branch for a <see cref="MatchSession"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract TResult Match<TResult>(Func<MoneySession, TResult> money, Func<MatchSession, TResult> match);

    /// <summary>
    /// Runs the branch for this session's kind — the statement form of
    /// <see cref="Match{TResult}"/>, with the same guarantee.
    /// </summary>
    /// <param name="money">The branch for a <see cref="MoneySession"/>.</param>
    /// <param name="match">The branch for a <see cref="MatchSession"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract void Switch(Action<MoneySession> money, Action<MatchSession> match);

    /// <summary>
    /// Whether <paramref name="other"/> is a session of this kind with equal
    /// terms and the same standing.
    /// </summary>
    /// <param name="other">The session to compare with; <see langword="null"/> is never equal.</param>
    public abstract bool Equals(Session? other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is Session other && Equals(other);

    /// <summary>A hash of the terms and the standing, consistent with <see cref="Equals(Session?)"/>.</summary>
    public abstract override int GetHashCode();

    /// <summary>Whether the two are equal sessions (<see cref="Equals(Session?)"/>); two nulls are equal.</summary>
    public static bool operator ==(Session? left, Session? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the two are not equal sessions (<see cref="Equals(Session?)"/>).</summary>
    public static bool operator !=(Session? left, Session? right) => !(left == right);

    /// <summary>
    /// Runs <paramref name="check"/>, a guard of one of the kind's rules, and
    /// refuses its breach as a document's <see cref="JsonException"/> when the
    /// session is being read (<see cref="DocumentRefusal"/>).
    /// </summary>
    private protected void Guard(Action check)
    {
        try
        {
            check();
        }
        catch (ArgumentException fault) when (_read)
        {
            throw DocumentRefusal.Of(fault);
        }
    }
}
