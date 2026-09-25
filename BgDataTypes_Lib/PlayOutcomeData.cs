using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Game states derived from the play choices of a decision: the board after
/// the best play and the board after the player's actual play.
///
/// <para>
/// <b>Frame: the next mover's.</b> Both boards are the position a play
/// reaches, flipped as <see cref="BoardState.ApplyPlay"/> leaves it: the
/// opponent of the decision-maker is on roll, so slot 25 is the opponent's
/// bar and their checkers are positive, while the decision-maker's checkers
/// are negative and slot 0 is the decision-maker's bar.
/// </para>
///
/// <para>
/// <b>Null when absent</b> (halheinrich/backgammon#15). No play is made in a
/// cube decision, so both boards are always <see langword="null"/> there; a
/// producer may also leave them <see langword="null"/> on a checker play
/// whose boards it could not compute (<c>ConvertXgToJson_Lib</c> does when
/// the player's play is not among the analysed candidates). Consumers test
/// for <see langword="null"/>; an absent board is never an empty or all-zero
/// one. On the wire an absent board is written as <c>null</c>, and a
/// document's missing member or the empty array older documents wrote reads
/// as <see langword="null"/> (<see cref="NullableBoardPositionJsonConverter"/>).
/// </para>
/// </summary>
public class PlayOutcomeData
{
    /// <summary>
    /// The board after the best play, in the next mover's frame (see the
    /// class summary); <see langword="null"/> when absent.
    /// </summary>
    [JsonConverter(typeof(NullableBoardPositionJsonConverter))]
    public BoardPosition? AfterBestBoard { get; init; }

    /// <summary>
    /// The board after the player's actual play, in the next mover's frame
    /// (see the class summary); <see langword="null"/> when absent.
    /// </summary>
    [JsonConverter(typeof(NullableBoardPositionJsonConverter))]
    public BoardPosition? AfterPlayerBoard { get; init; }
}
