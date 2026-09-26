using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The kind of session a decision is played in: money or a match
/// (halheinrich/backgammon#273, Hal's ruling of 2026-09-26). A session is one
/// of two types, <see cref="MoneySession"/> and <see cref="MatchSession"/>,
/// each carrying only its own facts; this is the kind as a value — what
/// <see cref="Session.Kind"/> reports and what the flat
/// <see cref="DecisionRow.SessionKind"/> stores.
/// </summary>
/// <remarks>
/// <para>
/// <b>Recorded explicitly on the wire.</b> Every session document carries its
/// kind as a <c>"Kind"</c> member, written first; a reader never infers the
/// kind from which members happen to be present (see
/// <see cref="SessionJsonConverter"/>). The token is the member's declared
/// name, string-exact in both directions like every enum here
/// (<see cref="StrictJsonStringEnumConverter{TEnum}"/>).
/// </para>
/// <para>
/// <b>Matching exhaustively.</b> A session's kind is its type: match on it
/// with <see cref="Session.Match{TResult}"/> or <see cref="Session.Switch"/>,
/// which take one branch per kind, so adding a kind breaks every such call at
/// compile time rather than falling through. A <c>switch</c> over this enum
/// cannot give that guarantee (the compiler requires a discard arm for an
/// enum), so reach for the session's match wherever the session is at hand.
/// </para>
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<SessionKind>))]
public enum SessionKind
{
    /// <summary>A money session: each game is played for points, under the money rules.</summary>
    Money,

    /// <summary>A match: games are played until a player reaches the match's length.</summary>
    Match,
}
