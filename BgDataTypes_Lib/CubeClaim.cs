using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The doubler half of a cube answer, three-valued — a claim about the
/// position, not a board action (SPEC-scoring §1, ratified 2026-08-26;
/// halheinrich/backgammon#86). <see cref="NoDouble"/> claims the position is
/// not good enough to double yet; <see cref="Double"/> claims it is a double;
/// <see cref="TooGood"/> claims playing on is worth more than doubling and
/// cashing. The board action behind <see cref="NoDouble"/> and
/// <see cref="TooGood"/> is identical — <see cref="CubeAction.NoDouble"/> —
/// which is why the claim exists as its own layer: the distinction exists to
/// be scored (SPEC-scoring §3). <see cref="CubeClaimExtensions.ToCubeAction"/>
/// is the single spelling of that collapse.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not a widening of <see cref="CubeAction"/>: "too good" is a
/// rationale, not a playable action, and no third doubler board action exists
/// (SPEC-scoring §3, "Too Good is claim-layer only"). The truth claim of an
/// analysed cube decision is derived producer-side from the equities —
/// <see cref="DecisionData.BestDoublerClaim"/> is the one derivation site;
/// there is no action-to-claim conversion because the claim is
/// underdetermined by the action alone.
/// </para>
/// <para>
/// Declaration order is the claim axis as ruled — {No Double, Double,
/// Too Good} — and is what a UI offering the claims renders. Too Good
/// occurs in money too, including under Jacoby via redoubles (SPEC-scoring
/// §3, "Uniform availability"), with one ruled exception: a money position
/// under Jacoby with the cube centred cannot be too good, so Too Good is
/// not offered there (the 2026-09-02 amendment, halheinrich/backgammon#187).
/// That offerability fact is derived once, producer-side, as
/// <see cref="BgDecisionData.CanBeTooGood"/>; consumers read it and never
/// re-derive it.
/// </para>
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<CubeClaim>))]
public enum CubeClaim
{
    /// <summary>The position is not good enough to double yet — the correct
    /// action is to play on, and doubling would lose equity.</summary>
    NoDouble,

    /// <summary>The position is a double — offering the cube has higher
    /// equity than playing on against optimal opponent response.</summary>
    Double,

    /// <summary>The position is too good to double — the correct action is
    /// still not to double, but because playing on (typically for a gammon)
    /// is worth more than the cashed point a double would collect.</summary>
    TooGood
}
