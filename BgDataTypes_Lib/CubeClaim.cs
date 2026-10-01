using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The three-way claim a cube answer makes about the position — No double,
/// Double or Too good (SPEC-scoring §1 and §3; halheinrich/backgammon#86). A
/// claim is not part of the answer: it is what the answer reads as at a
/// decision (amended 2026-09-30 on halheinrich/backgammon#326), derived once,
/// by <see cref="CubeDecision.ClaimOf"/>, from the answer and whether gammons
/// are possible there.
/// </summary>
/// <remarks>
/// <para>
/// Too good is not a board action: an answer reading it does not double, as
/// No double does not (<see cref="CubeAnswerExtensions.DoublerAction"/>), so
/// there is no third doubler action and no <see cref="CubeAction"/> member for
/// it. The claim exists to say why. Only the fourth answer,
/// <see cref="CubeAnswer.NoDoublePass"/>, can read it, and only where gammons
/// are possible: there it reads Too good, and elsewhere No double, with its
/// pass. For that answer the reading is which of its two labels applies, Too
/// good or No double / Pass; the wording is the label home's.
/// </para>
/// <para>
/// No action or answer leads back to a claim on its own: the reading needs
/// the decision. A played action is never read as a claim — the rationale is
/// the analysis's, not the game record's. Like every enum here it carries the
/// strict string-token converter; no document embeds it.
/// </para>
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<CubeClaim>))]
public enum CubeClaim
{
    /// <summary>Don't double: the reading of No double, and of the fourth
    /// answer where gammons are not possible.</summary>
    NoDouble,

    /// <summary>Double: the reading of Double / Take and Double / Pass.</summary>
    Double,

    /// <summary>Too good to double: playing on, typically for a gammon, is
    /// worth more than the cash a double would collect. The reading of the
    /// fourth answer where gammons are possible.</summary>
    TooGood
}
