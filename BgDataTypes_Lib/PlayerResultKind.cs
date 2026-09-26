using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Which of four cases a player's result on a decision is
/// (<see cref="PlayerResult.Kind"/>): no move recorded, a move the ranking
/// does not score, a scored move, or a move the record does not state. "Not
/// scored" and "not recorded" are told apart by this value, never by one
/// <see langword="null"/> (the umbrella's third-round ruling on the records
/// leg of halheinrich/backgammon#273).
/// </summary>
/// <remarks>
/// <see cref="NotRecorded"/> is the zero value, so a
/// <c>default</c> <see cref="PlayerResult"/> means "no move recorded". The
/// token is the member's declared name, string-exact in both directions
/// (<see cref="StrictJsonStringEnumConverter{TEnum}"/>); a row states it
/// (<see cref="DecisionRow.Result"/>). Every member carries a
/// <see cref="DescriptionAttribute"/> — the UI-facing label, which belongs to
/// the type owner.
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<PlayerResultKind>))]
public enum PlayerResultKind
{
    /// <summary>No player move is recorded: no candidate is the user's play and no analyser's error is stated.</summary>
    [Description("Not recorded")]
    NotRecorded = 0,

    /// <summary>
    /// A listed move the ranking does not score (SPEC-scoring §2a): a
    /// candidate whose equity is higher than the best play's under depth
    /// first. It has no error; a consumer treats it as an off-list play.
    /// </summary>
    [Description("Not scored")]
    NotScored,

    /// <summary>
    /// A move this library scores, with its error: a listed checker play the
    /// ranking scores (<see cref="RankedPlay.Error"/>), or a stated cube
    /// action (<see cref="CubeDecisionData.DoublerActionError"/>,
    /// <see cref="CubeDecisionData.TakerActionError"/>). Never negative.
    /// </summary>
    [Description("Scored")]
    Scored,

    /// <summary>
    /// A move the record does not state, with the analyser's error, which is
    /// the one statement of it: a checker play outside the candidates
    /// (<see cref="CheckerPlayDecisionData.UnlistedPlayError"/>), or a cube
    /// action the record does not state
    /// (<see cref="CubeDecisionData.UnstatedDoublerActionError"/>,
    /// <see cref="CubeDecisionData.UnstatedTakerActionError"/>).
    /// </summary>
    [Description("Unlisted")]
    Unlisted,
}
