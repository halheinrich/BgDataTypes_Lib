using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The terms a session is played on, as its header states them — what
/// <see cref="IMatchInfo.Terms"/> reports before any game or decision is
/// read: a money session's rules (<see cref="MoneyTerms"/>) or a match's
/// length (<see cref="MatchTerms"/>) (halheinrich/backgammon#273, Hal's ruling
/// of 2026-09-26). Each kind carries only its own facts, so money is the
/// kind, never a match length of 0, and a match states no money rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>One of the three scopes of money versus match.</b> A session's terms
/// are the header's; a game's standing is <see cref="GameStanding"/>
/// (<see cref="IGameInfo.Standing"/>); a decision's session, from the player
/// on roll's side, is <see cref="Session"/> — the terms and the standing
/// together, which is what a record holds. The three share
/// <see cref="SessionKind"/> and the rules of the internal
/// <see cref="SessionRules"/>, stated once.
/// </para>
/// <para>
/// <b>A closed pair, a value.</b> The constructors are not reachable outside
/// this library, <see cref="Match{TResult}"/> and <see cref="Switch"/> take
/// one branch per kind, and two terms are equal exactly when they are the
/// same kind with the same facts.
/// </para>
/// <para>
/// <b>On the wire</b> the terms are a kinded document, read and written as
/// a session's and a record's are: the kind is a real member,
/// <c>"Kind"</c>, written first and read wherever it sits, and
/// <see cref="SessionTermsJsonConverter"/> hands the document to that kind's
/// generated contract through the one dispatch (the internal
/// <see cref="KindDispatch"/>). Every refusal — a missing, duplicated or
/// unknown kind, a member of the other kind, a missing member, a rule
/// broken — is a <see cref="JsonException"/> on both paths, whether the terms
/// are read as <see cref="SessionTerms"/> or as their kind. A producer that
/// serializes its <see cref="IMatchInfo"/> implementer (the converter's
/// header types are) writes the terms this way with nothing to register.
/// </para>
/// </remarks>
[JsonConverter(typeof(SessionTermsJsonConverter))]
public abstract class SessionTerms : IEquatable<SessionTerms>, IEqualityOperators<SessionTerms, SessionTerms, bool>
{
    private readonly SessionKind _kind;

    // True for terms being read from a document: set only by the serializer's
    // constructor, before any member is set, so each kind's rules refuse a
    // breach as a JsonException rather than the guard's own ArgumentException
    // (DocumentRefusal).
    private readonly bool _read;

    /// <summary>The constructor code builds terms through: the kind is fixed here, by the two kinds in this library only.</summary>
    private protected SessionTerms(SessionKind kind) => _kind = kind;

    /// <summary>
    /// The constructor a document is read through, reachable only from each
    /// kind's serializer constructor: the kind is fixed as for code, the terms
    /// are marked as read, and the kind the document states is held to it —
    /// all before any other member is set.
    /// </summary>
    /// <exception cref="JsonException"><paramref name="statedKind"/> is not <paramref name="kind"/>.</exception>
    private protected SessionTerms(SessionKind kind, SessionKind statedKind)
    {
        _kind = kind;
        _read = true;
        Kind = statedKind;
    }

    /// <summary>
    /// The session's kind: every <see cref="MoneyTerms"/> is
    /// <see cref="SessionKind.Money"/>, every <see cref="MatchTerms"/>
    /// <see cref="SessionKind.Match"/>. A real wire member, written first;
    /// code never sets it, and a document may only state the terms' own kind.
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
                throw new JsonException($"The document states Kind {value} for {_kind} terms.");
        }
    }

    /// <summary>The result of the branch for this kind — one branch per kind, so a new kind breaks every call at compile time.</summary>
    /// <typeparam name="TResult">The type both branches return.</typeparam>
    /// <param name="money">The branch for <see cref="MoneyTerms"/>.</param>
    /// <param name="match">The branch for <see cref="MatchTerms"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract TResult Match<TResult>(Func<MoneyTerms, TResult> money, Func<MatchTerms, TResult> match);

    /// <summary>Runs the branch for this kind — the statement form of <see cref="Match{TResult}"/>.</summary>
    /// <param name="money">The branch for <see cref="MoneyTerms"/>.</param>
    /// <param name="match">The branch for <see cref="MatchTerms"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract void Switch(Action<MoneyTerms> money, Action<MatchTerms> match);

    /// <summary>Whether <paramref name="other"/> is terms of this kind with the same facts.</summary>
    /// <param name="other">The terms to compare with; <see langword="null"/> is never equal.</param>
    public abstract bool Equals(SessionTerms? other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is SessionTerms other && Equals(other);

    /// <summary>A hash of the kind and its facts, consistent with <see cref="Equals(SessionTerms?)"/>.</summary>
    public abstract override int GetHashCode();

    /// <summary>Whether the two are equal terms; two nulls are equal.</summary>
    public static bool operator ==(SessionTerms? left, SessionTerms? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the two are not equal terms.</summary>
    public static bool operator !=(SessionTerms? left, SessionTerms? right) => !(left == right);

    /// <summary>
    /// Runs <paramref name="check"/>, a guard of one of the kind's rules, and
    /// refuses its breach as a document's <see cref="JsonException"/> when the
    /// terms are being read (<see cref="DocumentRefusal"/>).
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
