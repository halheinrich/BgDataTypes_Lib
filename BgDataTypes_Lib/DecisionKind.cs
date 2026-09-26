using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The kind of a decision: a checker play or a cube decision
/// (halheinrich/backgammon#273, Hal's ruling of 2026-09-25). A decision
/// record is one of two types, <see cref="CheckerPlayDecision"/> and
/// <see cref="CubeDecision"/>, each carrying only its own fields; this is the
/// kind as a value — what <see cref="BgDecisionData.Kind"/> reports, what the
/// flat <see cref="DecisionRow"/> stores, and what
/// <see cref="IDecisionFilterData.Kind"/> filters on.
/// </summary>
/// <remarks>
/// <para>
/// <b>Recorded explicitly on the wire.</b> Every decision document carries
/// its kind as a <c>"Kind"</c> member, written first; a reader never infers
/// the kind from which members happen to be present (see
/// <see cref="BgDecisionDataJsonConverter"/>). The token is the member's
/// declared name, string-exact in both directions like every enum here
/// (<see cref="StrictJsonStringEnumConverter{TEnum}"/>).
/// </para>
/// <para>
/// <b>Matching exhaustively.</b> A record's kind is its type: match on it with
/// <see cref="BgDecisionData.Match{TResult}"/> or
/// <see cref="BgDecisionData.Switch"/>, which take one branch per kind, so
/// adding a kind breaks every such call at compile time rather than falling
/// through. A <c>switch</c> over this enum cannot give that guarantee (the
/// compiler requires a discard arm for an enum), so reach for the record's
/// match wherever the record is at hand.
/// </para>
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<DecisionKind>))]
public enum DecisionKind
{
    /// <summary>A checker-play decision: the dice are rolled and a play is chosen.</summary>
    CheckerPlay,

    /// <summary>A cube decision: whether to double, and whether to take.</summary>
    Cube,
}
