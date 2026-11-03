using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// The rules that hold a session's facts, and the cube's, to the game of
/// backgammon — each stated once, here, with the one sentence every refusal
/// of it carries — at every scope money versus match is stated: a header's
/// <see cref="MoneyTerms"/>, <see cref="MatchTerms"/>,
/// <see cref="MoneyStanding"/> and <see cref="MatchStanding"/>; a game's
/// session, <see cref="GameSession.Create"/>, which pairs them; a record's
/// <see cref="MoneySession"/>, <see cref="MatchSession"/> and
/// <see cref="PositionData"/>; and <see cref="Session.Create"/>, which builds
/// a session from its header. Each holds its members to them in its init
/// guards, refusing a breach from code with the guard's
/// <see cref="ArgumentException"/> and from a document with a
/// <see cref="System.Text.Json.JsonException"/> carrying it (the wire rule on
/// <see cref="BgDataTypesJsonContext"/>). A game's standing and its session
/// also have a door that does not throw — <see cref="MatchStanding.TryCreate"/>
/// and <see cref="GameSession.TryCreate"/> — which hands back the same
/// refusal, composed here once with the values it refuses, as the throwing
/// door's message begins with.
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

    // -----------------------------------------------------------------------
    //  A game's session, and a decision's, from the header
    //
    //  A game's session is its terms and its standing, as the header states
    //  them; a decision's is the same seen from the player on roll. The terms
    //  and the standing are of one kind — money terms with a money standing,
    //  a match's terms with a match standing — and a match standing's away
    //  scores are at most the terms' length, which the standing does not
    //  know. GameSession.Create is the one pairing: it holds the kinds to
    //  each other, and the match kind it builds holds the away scores to the
    //  length. Session.Create pairs through it, then turns the standing to
    //  the seat on roll, one of the two a header names.
    // -----------------------------------------------------------------------

    /// <summary>The rule binding a standing's kind to its terms', in the one sentence every refusal carries.</summary>
    internal const string StandingKindMessage =
        "A session's terms and its game's standing are of one kind: money terms take a money standing, a match's terms a match standing.";

    /// <summary>The seat rule, in the one sentence every refusal carries.</summary>
    internal const string SeatMessage = "The seat on roll is player 1 or player 2.";

    /// <summary>Whether terms of kind <paramref name="terms"/> may take a standing of kind <paramref name="standing"/>.</summary>
    internal static bool StandingKindHolds(SessionKind terms, SessionKind standing) => terms == standing;

    // -----------------------------------------------------------------------
    //  A game's standing and session, refused with the values refused
    //
    //  A refusal of a standing's rules, or of the pairing's, is the rule's
    //  sentence and then the values it refuses, on one line, written the
    //  same on every runtime (halheinrich/backgammon#514, Hal's ruling B of
    //  2026-11-03). Each is composed here once, beside its rule, and is null
    //  when the rule holds. Both doors carry it: MatchStanding's init guards
    //  and GameSession.Create as their exception's message, the runtime's own
    //  tail after it, and MatchStanding.TryCreate and GameSession.TryCreate as
    //  their refusal, with nothing after it. An away score's guard sees one
    //  member, so its refusal names that player alone; the Crawford standing
    //  is checked only once both away scores are known, so its refusal names
    //  both, player 1's first, whichever member completed it.
    // -----------------------------------------------------------------------

    /// <summary>
    /// The refusal of <paramref name="player"/>'s away score
    /// <paramref name="away"/> below the floor, naming the player and the
    /// score; <see langword="null"/> when the score holds.
    /// </summary>
    internal static string? AwayFloorRefusal(Seat player, int away) =>
        NeedsHolds(away, length: null)
            ? null
            : string.Create(CultureInfo.InvariantCulture, $"{NeedsMessage} Player {(player == Seat.Player1 ? 1 : 2)}'s away score is {away}.");

    /// <summary>
    /// The refusal of a Crawford flag of <paramref name="isCrawford"/> at
    /// <paramref name="away1"/> and <paramref name="away2"/>, naming the
    /// standing; <see langword="null"/> when the flag is clear or the standing
    /// may be the Crawford game's.
    /// </summary>
    internal static string? CrawfordStandingRefusal(bool isCrawford, int away1, int away2) =>
        !isCrawford || CrawfordStandingHolds(away1, away2)
            ? null
            : $"{CrawfordStandingMessage} The standing is {FormatAways(away1, away2)}.";

    /// <summary>
    /// The refusal of <paramref name="standing"/>'s away scores against
    /// <paramref name="terms"/>' length, naming the standing and the length;
    /// <see langword="null"/> when each away score is at most the length.
    /// </summary>
    internal static string? LengthBoundRefusal(MatchTerms terms, MatchStanding standing) =>
        NeedsHolds(standing.Away1, terms.Length) && NeedsHolds(standing.Away2, terms.Length)
            ? null
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{NeedsMessage} The standing is {FormatAways(standing.Away1, standing.Away2)}. The match length is {terms.Length}.");

    /// <summary>
    /// The refusal of <paramref name="terms"/> and a
    /// <paramref name="standing"/> of different kinds, naming both kinds;
    /// <see langword="null"/> when the kinds agree.
    /// </summary>
    internal static string? StandingKindRefusal(SessionTerms terms, GameStanding standing) =>
        StandingKindHolds(terms.Kind, standing.Kind)
            ? null
            : $"{StandingKindMessage} The terms are {terms.Kind}; the standing is {standing.Kind}.";

    /// <summary>
    /// A match's away scores for a reader — <c>"3-away/5-away"</c>, the first
    /// given first: the one spelling of a match standing, in a refusal and in
    /// each match type's <c>ToString</c>.
    /// </summary>
    internal static string FormatAways(int first, int second) =>
        string.Create(CultureInfo.InvariantCulture, $"{first}-away/{second}-away");
}
