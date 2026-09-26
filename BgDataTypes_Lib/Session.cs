using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The session a decision is played in, as it stands at the decision: a money
/// session or a match (halheinrich/backgammon#273, Hal's ruling of
/// 2026-09-26). A session is one of two types, <see cref="MoneySession"/> and
/// <see cref="MatchSession"/>, each carrying only its own facts, so code that
/// reads one kind's fact off the other does not compile and no member of
/// either holds a value standing for "not applicable". Money used to be
/// spelled with match stand-ins — a match length of 0, away scores of 0, a
/// Crawford flag always false — beside a Jacoby fact that meant nothing in a
/// match; none of them is expressible now.
/// </summary>
/// <remarks>
/// <para>
/// <b>From the player on roll's side</b>, like the rest of
/// <see cref="PositionData"/>, which holds it: a match's away scores are the
/// player on roll's and the opponent's.
/// </para>
/// <para>
/// <b>A closed pair.</b> The constructor is not reachable outside this
/// library, so these two kinds are the only ones there are, and
/// <see cref="Match{TResult}"/> and <see cref="Switch"/> take one branch per
/// kind: a consumer's match over the kind has no silent fall-through, and a
/// third kind would break every match at compile time.
/// </para>
/// <para>
/// <b>On the wire</b> the kind is a real member, <c>"Kind"</c>, written first
/// whatever static type the value is serialized as, and read wherever it sits;
/// <see cref="SessionJsonConverter"/> finds it, refuses a document without
/// exactly one known kind, and hands the whole document to that kind's
/// generated contract — the decision kind's mechanism, reused
/// (<see cref="BgDecisionDataJsonConverter"/>). Every refusal — a missing,
/// duplicated or unknown kind, a member of the other kind (each kind disallows
/// unmapped members), a missing member, a rule of the kind broken — is a
/// <see cref="JsonException"/> on both paths, whether the session is read as
/// <see cref="Session"/>, as its own kind, or inside a record.
/// <see cref="Kind"/> is required through <see cref="JsonRequiredAttribute"/>,
/// since the type states it and code never does.
/// </para>
/// <para>
/// <b>A value.</b> A session is immutable, and two sessions are equal exactly
/// when they are the same kind with the same facts — so a record's view, its
/// row and the row read back from JSON all state one session, whatever
/// instance each holds. The hash is consistent with that equality.
/// </para>
/// </remarks>
[JsonConverter(typeof(SessionJsonConverter))]
public abstract class Session : IEquatable<Session>, IEqualityOperators<Session, Session, bool>
{
    private readonly SessionKind _kind;

    // True for a session being read from a document: set only by the
    // serializer's constructor, before any member is set, so each kind's rules
    // refuse a breach as a JsonException rather than the guard's own
    // ArgumentException (DocumentRefusal).
    private readonly bool _read;

    /// <summary>
    /// The constructor code builds a session through, reachable only from the
    /// two kinds in this library: the kind is fixed here, before any member is
    /// set.
    /// </summary>
    private protected Session(SessionKind kind) => _kind = kind;

    /// <summary>
    /// The constructor a document is read through, reachable only from each
    /// kind's serializer constructor: the kind is fixed as for code, the
    /// session is marked as read, and the kind the document states is held to
    /// it — all before any other member is set.
    /// </summary>
    /// <exception cref="JsonException"><paramref name="statedKind"/> is not <paramref name="kind"/>.</exception>
    private protected Session(SessionKind kind, SessionKind statedKind)
    {
        _kind = kind;
        _read = true;
        Kind = statedKind;
    }

    /// <summary>Whether this session is being read from a document — for a kind's rules, which then refuse a breach as a <see cref="JsonException"/>.</summary>
    private protected bool IsRead => _read;

    /// <summary>
    /// The session's kind — the value form of its type: every
    /// <see cref="MoneySession"/> is <see cref="SessionKind.Money"/> and every
    /// <see cref="MatchSession"/> is <see cref="SessionKind.Match"/>. A real
    /// wire member, written first; code never sets it, and a document may only
    /// state the session's own kind.
    /// </summary>
    /// <exception cref="JsonException">
    /// Thrown on read when the document states the other kind — reachable
    /// only from JSON: the serializer passes the stated kind to the kind's
    /// serializer constructor, which sets it here.
    /// </exception>
    [JsonInclude, JsonRequired, JsonPropertyOrder(-1)]
    public SessionKind Kind
    {
        get => _kind;
        internal init
        {
            if (value != _kind)
                throw new JsonException($"The document states Kind {value} for a {_kind} session.");
        }
    }

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
    /// Whether <paramref name="other"/> is a session of this kind with the
    /// same facts.
    /// </summary>
    /// <param name="other">The session to compare with; <see langword="null"/> is never equal.</param>
    public abstract bool Equals(Session? other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is Session other && Equals(other);

    /// <summary>A hash of the kind and its facts, consistent with <see cref="Equals(Session?)"/>.</summary>
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
