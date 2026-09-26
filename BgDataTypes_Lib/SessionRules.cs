namespace BgDataTypes_Lib;

/// <summary>
/// The rules that hold a session's facts, and the cube's, to the game of
/// backgammon — each stated once, here, with the one sentence every refusal
/// of it carries. <see cref="MatchSession"/>, <see cref="MoneySession"/> and
/// <see cref="PositionData"/> hold their members to them in their init
/// guards, refusing a breach from code with the guard's
/// <see cref="ArgumentException"/> and from a document with a
/// <see cref="System.Text.Json.JsonException"/> carrying it (the wire rule on
/// <see cref="BgDataTypesJsonContext"/>).
/// </summary>
internal static class SessionRules
{
    // -----------------------------------------------------------------------
    //  The cube's values
    //
    //  The cube starts at 1 and each double doubles it, so its value — and a
    //  limit on it — is a positive power of two; the XGID states each as its
    //  exponent. A money session's cube limit caps the cube, which never
    //  exceeds it.
    // -----------------------------------------------------------------------

    /// <summary>The cube-size rule, in the one sentence every refusal carries.</summary>
    internal const string CubeSizeMessage = "The cube's value is a positive power of two: 1, 2, 4, 8, …";

    /// <summary>The cube-limit rule, in the one sentence every refusal carries.</summary>
    internal const string CubeLimitMessage = "A cube limit is a positive power of two: the highest value the cube may reach.";

    /// <summary>The rule binding the cube to its limit, in the one sentence every refusal carries.</summary>
    internal const string CubeWithinLimitMessage = "The cube never exceeds the money session's cube limit.";

    /// <summary>The cube-owner rule, in the one sentence every refusal carries.</summary>
    internal const string CubeOwnerMessage = "The cube is on roll's, the opponent's, or centred.";

    /// <summary>Whether <paramref name="value"/> is a value the cube, or a limit on it, can take.</summary>
    internal static bool CubeValueHolds(int value) => value >= 1 && (value & (value - 1)) == 0;

    // -----------------------------------------------------------------------
    //  A money session's scores
    //
    //  The points each player has won in the session before the game: never
    //  negative.
    // -----------------------------------------------------------------------

    /// <summary>The money score rule, in the one sentence every refusal carries.</summary>
    internal const string ScoreMessage = "A money session's score is the points a player has won in it: never negative.";

    /// <summary>Whether <paramref name="score"/> is a money session's score.</summary>
    internal static bool ScoreHolds(int score) => score >= 0;

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
