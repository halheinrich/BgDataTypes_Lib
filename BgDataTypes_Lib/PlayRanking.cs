using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The ranking of a checker play's candidates — the one definition of which
/// play is best, the order and rank numbers, and each play's error (SPEC-scoring
/// §2a, Hal's ruling of 2026-09-26 on halheinrich/backgammon#282). A
/// checker-play decision derives all of them for a given ranking
/// (<see cref="CheckerPlayDecisionData.RankedBy"/>); nothing in this library
/// answers "best" or "error" without one.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rankings.</b> <see cref="Equity"/> orders the candidates by equity,
/// highest first, and analysis depth has no effect. <see cref="DepthFirst"/>
/// orders them by analysis depth, deepest first (<see cref="PlayCandidate.DepthRank"/>,
/// an unrecorded depth below every recorded one), then by equity within a
/// depth. Where both keys are equal, the stored order stands. The best play
/// is the ranking's first; a play's error is the best play's equity minus
/// its own.
/// </para>
/// <para>
/// <b>Not scored, under depth first only.</b> A candidate at a different
/// depth from the best play's (under depth first, always a shallower one)
/// whose equity is higher than the best's is not scored
/// (<see cref="RankedPlay.IsScored"/>): a consumer treats it as an off-list
/// play. Every other candidate is scored, so no scored error is ever
/// negative. Under <see cref="Equity"/> every candidate is scored.
/// </para>
/// <para>
/// <see cref="Equity"/> is the default, and deliberately the zero value: an
/// app without the setting uses it. The token is the member's declared name,
/// string-exact in both directions (<see cref="StrictJsonStringEnumConverter{TEnum}"/>);
/// a row states the ranking it was built for (<see cref="DecisionRow.Ranking"/>).
/// Every member carries a <see cref="DescriptionAttribute"/> — the UI-facing
/// label, which belongs to the type owner.
/// </para>
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<PlayRanking>))]
public enum PlayRanking
{
    /// <summary>By equity, highest first; analysis depth has no effect. The default.</summary>
    [Description("Equity")]
    Equity = 0,

    /// <summary>By analysis depth, deepest first, then by equity within a depth.</summary>
    [Description("Depth first")]
    DepthFirst,
}
