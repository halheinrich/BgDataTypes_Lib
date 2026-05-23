using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A combined cube-decision verdict — the four possible outcomes of an
/// entire cube decision rendered as one judgment, covering both the
/// on-roll player's double/no-double half and the opponent's take/pass half.
/// </summary>
/// <remarks>
/// Complements <see cref="CubeAction"/>, which is the atomic per-player view
/// used for stats attribution and substrate calls. <c>CubeVerdict</c> is the
/// aggregate view used by quizzes and analyzers that judge the whole
/// decision at once. The bidirectional mapping to the atomic
/// <see cref="CubeActionPair"/> form lives on <see cref="CubeActionPair"/>.
///
/// Beaver and raccoon are deliberately not yet members — parallel to the
/// same omission on <see cref="CubeAction"/>. Both can be appended without
/// disturbing the existing members or their serialized string forms.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CubeVerdict
{
    /// <summary>
    /// The on-roll player correctly does not double; the decision ends with
    /// play continuing at the current cube level. Decomposes to
    /// (<see cref="CubeAction.NoDouble"/>, <see cref="CubeAction.Take"/>) —
    /// the responder's notional <c>Take</c> records that they would have
    /// accepted had a double been offered.
    /// </summary>
    NoDouble,

    /// <summary>
    /// The on-roll player doubles and the opponent takes. Decomposes to
    /// (<see cref="CubeAction.Double"/>, <see cref="CubeAction.Take"/>).
    /// </summary>
    DoubleTake,

    /// <summary>
    /// The on-roll player doubles and the opponent passes, conceding the
    /// game at the current stake. Decomposes to
    /// (<see cref="CubeAction.Double"/>, <see cref="CubeAction.Pass"/>).
    /// </summary>
    DoublePass,

    /// <summary>
    /// "Too good to double" — the position is strong enough that the on-roll
    /// player declines to double and plays on for the gammon, even though
    /// the opponent would correctly pass if doubled. Decomposes to
    /// (<see cref="CubeAction.NoDouble"/>, <see cref="CubeAction.Pass"/>) —
    /// the doubler's <c>NoDouble</c> records the actual restraint; the
    /// responder's notional <c>Pass</c> records the correct response had a
    /// double been offered.
    /// </summary>
    TooGood
}
