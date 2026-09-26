namespace BgDataTypes_Lib;

/// <summary>
/// The rules that hold a session's facts to the game of backgammon — each
/// stated once, here, with the one sentence every refusal of it carries.
/// <see cref="MatchSession"/> holds its members to them in its init guards,
/// refusing a breach from code with the guard's
/// <see cref="ArgumentException"/> and from a document with a
/// <see cref="System.Text.Json.JsonException"/> carrying it (the wire rule on
/// <see cref="BgDataTypesJsonContext"/>).
/// </summary>
internal static class SessionRules
{
    // -----------------------------------------------------------------------
    //  A match's length and its away scores
    //
    //  A match is played to at least one point. An away score is what a
    //  player still needs: at least 1 while the match is being played — a
    //  player 0-away has won it, so no decision remains — and never more than
    //  the length, since a score is never below 0.
    // -----------------------------------------------------------------------

    /// <summary>The length rule, in the one sentence every refusal carries.</summary>
    internal const string LengthMessage = "A match is played to at least one point.";

    /// <summary>The away-score rule, in the one sentence every refusal carries.</summary>
    internal const string NeedsMessage =
        "A player in a match needs at least one point, and never more than the match's length: a player 0-away has already won.";

    /// <summary>Whether <paramref name="length"/> is a match's length.</summary>
    internal static bool LengthHolds(int length) => length >= 1;

    /// <summary>Whether a player may need <paramref name="needs"/> points in a match of <paramref name="length"/>, when the length is known.</summary>
    internal static bool NeedsHolds(int needs, int? length) => needs >= 1 && (length is not int known || needs <= known);

    // -----------------------------------------------------------------------
    //  The Crawford game's standing
    //
    //  The Crawford game is the one game after a player first reaches match
    //  point, 1-away. The other player is not 1-away then — had they reached
    //  it first, the Crawford game would already be over, and a 1-point match
    //  starts at 1-away each with no Crawford game — so exactly one player is
    //  1-away. (XgFilter_Lib's score tokens and BgGame_Lib's game state state
    //  the same rule for their own surfaces.)
    // -----------------------------------------------------------------------

    /// <summary>The Crawford standing rule, in the one sentence every refusal carries.</summary>
    internal const string CrawfordStandingMessage =
        "The Crawford game is the game after a player first reaches match point: exactly one player is 1-away in it.";

    /// <summary>Whether a game at <paramref name="onRollNeeds"/> and <paramref name="opponentNeeds"/> may be the Crawford game.</summary>
    internal static bool CrawfordStandingHolds(int onRollNeeds, int opponentNeeds) =>
        (onRollNeeds == 1) != (opponentNeeds == 1);
}
