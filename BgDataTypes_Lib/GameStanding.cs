using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Where the players stand as a game begins, as its header states it — what
/// <see cref="IGameInfo.Standing"/> reports before any decision is read:
/// in a money session, the points each has won (<see cref="MoneyStanding"/>);
/// in a match, what each still needs and whether this is the Crawford game
/// (<see cref="MatchStanding"/>) (halheinrich/backgammon#273, Hal's ruling of
/// 2026-09-26). Each kind carries only its own facts, so money is the kind,
/// never away scores of 0 beside a Crawford flag that is always false.
/// </summary>
/// <remarks>
/// <para>
/// <b>Seat-anchored, not on-roll-anchored.</b> A game header knows player 1
/// and player 2, and both roll within the game, so the standing is theirs
/// (<c>1</c>, <c>2</c>); a decision's <see cref="Session"/> states the same
/// facts from the player on roll's side. The three scopes of money versus
/// match — <see cref="SessionTerms"/>, this, and <see cref="Session"/> — share
/// <see cref="SessionKind"/> and the rules of the internal
/// <see cref="SessionRules"/>, stated once.
/// </para>
/// <para>
/// <b>A closed pair, a value.</b> The constructors are not reachable outside
/// this library, <see cref="Match{TResult}"/> and <see cref="Switch"/> take
/// one branch per kind, and two standings are equal exactly when they are
/// the same kind with the same facts.
/// </para>
/// <para>
/// <b>On the wire</b> a standing is a kinded document, as
/// <see cref="SessionTerms"/> are: the kind is a real member,
/// <c>"Kind"</c>, written first and read wherever it sits, and
/// <see cref="GameStandingJsonConverter"/> hands the document to that kind's
/// generated contract through the one dispatch (the internal
/// <see cref="KindDispatch"/>). Every refusal — a missing, duplicated or
/// unknown kind, a member of the other kind, a missing member, a rule
/// broken — is a <see cref="JsonException"/> on both paths, whether the
/// standing is read as <see cref="GameStanding"/> or as its kind. A producer
/// that serializes its <see cref="IGameInfo"/> implementer (the converter's
/// header types are) writes the standing this way with nothing to register.
/// </para>
/// </remarks>
[JsonConverter(typeof(GameStandingJsonConverter))]
public abstract class GameStanding : IEquatable<GameStanding>, IEqualityOperators<GameStanding, GameStanding, bool>
{
    private readonly SessionKind _kind;

    // True for a standing being read from a document: set only by the
    // serializer's constructor, before any member is set, so each kind's rules
    // refuse a breach as a JsonException rather than the guard's own
    // ArgumentException (DocumentRefusal).
    private readonly bool _read;

    /// <summary>The constructor code builds a standing through: the kind is fixed here, by the two kinds in this library only.</summary>
    private protected GameStanding(SessionKind kind) => _kind = kind;

    /// <summary>
    /// The constructor a document is read through, reachable only from each
    /// kind's serializer constructor: the kind is fixed as for code, the
    /// standing is marked as read, and the kind the document states is held to
    /// it — all before any other member is set.
    /// </summary>
    /// <exception cref="JsonException"><paramref name="statedKind"/> is not <paramref name="kind"/>.</exception>
    private protected GameStanding(SessionKind kind, SessionKind statedKind)
    {
        _kind = kind;
        _read = true;
        Kind = statedKind;
    }

    /// <summary>
    /// The session's kind: every <see cref="MoneyStanding"/> is
    /// <see cref="SessionKind.Money"/>, every <see cref="MatchStanding"/>
    /// <see cref="SessionKind.Match"/>. A real wire member, written first;
    /// code never sets it, and a document may only state the standing's own
    /// kind.
    /// </summary>
    /// <exception cref="JsonException">
    /// Thrown on read when the document states the other kind — reachable
    /// only from JSON, through the kind's serializer constructor.
    /// </exception>
    [JsonInclude, JsonRequired, JsonPropertyOrder(-1)]
    public SessionKind Kind
    {
        get => _kind;
        internal init
        {
            if (value != _kind)
                throw new JsonException($"The document states Kind {value} for a {_kind} standing.");
        }
    }

    /// <summary>The result of the branch for this kind — one branch per kind, so a new kind breaks every call at compile time.</summary>
    /// <typeparam name="TResult">The type both branches return.</typeparam>
    /// <param name="money">The branch for a <see cref="MoneyStanding"/>.</param>
    /// <param name="match">The branch for a <see cref="MatchStanding"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract TResult Match<TResult>(Func<MoneyStanding, TResult> money, Func<MatchStanding, TResult> match);

    /// <summary>Runs the branch for this kind — the statement form of <see cref="Match{TResult}"/>.</summary>
    /// <param name="money">The branch for a <see cref="MoneyStanding"/>.</param>
    /// <param name="match">The branch for a <see cref="MatchStanding"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract void Switch(Action<MoneyStanding> money, Action<MatchStanding> match);

    /// <summary>Whether <paramref name="other"/> is a standing of this kind with the same facts.</summary>
    /// <param name="other">The standing to compare with; <see langword="null"/> is never equal.</param>
    public abstract bool Equals(GameStanding? other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is GameStanding other && Equals(other);

    /// <summary>A hash of the kind and its facts, consistent with <see cref="Equals(GameStanding?)"/>.</summary>
    public abstract override int GetHashCode();

    /// <summary>Whether the two are equal standings; two nulls are equal.</summary>
    public static bool operator ==(GameStanding? left, GameStanding? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the two are not equal standings.</summary>
    public static bool operator !=(GameStanding? left, GameStanding? right) => !(left == right);

    /// <summary>
    /// Runs <paramref name="check"/>, a guard of one of the kind's rules, and
    /// refuses its breach as a document's <see cref="JsonException"/> when the
    /// standing is being read (<see cref="DocumentRefusal"/>).
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
