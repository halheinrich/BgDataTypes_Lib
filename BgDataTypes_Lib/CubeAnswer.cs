using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A cube answer: one of exactly four (SPEC-scoring §3, amended 2026-09-30 on
/// halheinrich/backgammon#326). Hal: "Either you double or you don't; either
/// you take or you pass. That makes four not six." The same type serves both
/// sides of the scoring: a submitted answer, and the truth of an analysed
/// decision (<see cref="CubeDecisionData.BestAnswer"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Its projections.</b> The doubling action (double or not) and the response
/// (take or pass) are total projections of the answer, read through
/// <see cref="CubeAnswerExtensions.DoublerAction"/> and
/// <see cref="CubeAnswerExtensions.TakerAction"/>, and never stored beside it.
/// No other combination of a claim and a response is representable: the
/// six-value claim × response pair this type replaced is retired.
/// </para>
/// <para>
/// <b>What it reads as depends on the decision.</b> The three-way claim
/// (<see cref="CubeClaim"/>) is a reading of the answer at a decision,
/// <see cref="CubeDecision.ClaimOf"/>: the fourth answer,
/// <see cref="NoDoublePass"/>, reads Too good where gammons are possible and
/// No double / Pass where they are not (<see cref="CubeDecision.GammonsPossible"/>).
/// So the fourth answer is named by its meaning, "don't double, they'd pass",
/// never by either of its labels; the labels' wording, full and short, is the
/// label home's, in BackgammonDiagram_Lib. What an answer costs is the
/// decision's too, <see cref="CubeDecision.CostOf"/>.
/// </para>
/// <para>
/// Declaration order is the order the four are offered in, SPEC-scoring §3's
/// column order; all four are always offered. The zero value is
/// <see cref="NoDouble"/>, a meaningful answer: "no answer" is
/// <c>CubeAnswer?</c> <see langword="null"/>, never <see langword="default"/>.
/// Every operation taking an answer refuses a value outside the four with an
/// <see cref="ArgumentOutOfRangeException"/>. Like every enum here it carries
/// the strict string-token converter, though no document embeds it yet.
/// </para>
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<CubeAnswer>))]
public enum CubeAnswer
{
    /// <summary>
    /// Don't double. Its response, a take, is implied and never charged: no
    /// cube is offered, so the answer commits to no response
    /// (<see cref="CubeAnswerExtensions.CommitsToResponse"/>).
    /// </summary>
    NoDouble,

    /// <summary>Double, and they'd take.</summary>
    DoubleTake,

    /// <summary>Double, and they'd pass.</summary>
    DoublePass,

    /// <summary>
    /// Don't double, and they'd pass — the fourth answer. It reads Too good
    /// where gammons are possible and No double / Pass where they are not
    /// (<see cref="CubeDecision.ClaimOf"/>); either way it commits to its
    /// pass, which its cost charges.
    /// </summary>
    NoDoublePass,
}
