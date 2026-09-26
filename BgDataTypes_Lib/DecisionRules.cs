namespace BgDataTypes_Lib;

/// <summary>
/// The rules that bind a decision's members to one another — each stated
/// once, here, with the one sentence every refusal of it carries. Two kinds
/// of caller hold a decision to them: the records' init guards
/// (<see cref="BgDecisionData"/> and its two kinds), which refuse with an
/// <see cref="ArgumentException"/> naming the member that completed the
/// contradiction, and the flat row's read-time check
/// (<see cref="DecisionRow"/>), which refuses the document with a
/// <see cref="System.Text.Json.JsonException"/>. The candidates' validity
/// from the decision's position is a rule of the same standing, stated
/// beside the play rule it relies on, on <see cref="BoardState.IsSamePlay"/>.
/// </summary>
internal static class DecisionRules
{
    // -----------------------------------------------------------------------
    //  The Crawford rule (halheinrich/backgammon#201)
    //
    //  Doubling is prohibited in the Crawford game, so a cube decision in a
    //  Crawford position describes a decision that cannot exist. The Crawford
    //  game is a match's (MatchSession.IsCrawford): a money session has none,
    //  so the rule reads the session's kind, never a flag standing for "no".
    //  BgGame_Lib's GameState states the rule again for live play, and the
    //  two stay separate by layering: that one is a three-reason legality
    //  predicate over live match state (no cube, Crawford, opponent owns
    //  it), this one a wire-record invariant.
    // -----------------------------------------------------------------------

    /// <summary>The Crawford rule, in the one sentence every refusal carries.</summary>
    internal const string CrawfordMessage =
        "Doubling is prohibited in the Crawford game, so a cube decision cannot carry the Crawford flag.";

    /// <summary>Whether a decision of <paramref name="kind"/> may be made in <paramref name="session"/>.</summary>
    internal static bool CrawfordAllows(DecisionKind kind, Session session) =>
        !(kind == DecisionKind.Cube && session is MatchSession { IsCrawford: true });

    // -----------------------------------------------------------------------
    //  The identifier's kind
    //
    //  An XgDecisionId names the decision's kind in its canonical form
    //  (":cube" or ":play"), so it must name the decision's own; an
    //  XgpDecisionId names no kind.
    // -----------------------------------------------------------------------

    /// <summary>The identifier rule, in the one sentence every refusal carries.</summary>
    internal const string IdKindMessage =
        "The decision's identifier names the other kind of decision: an XgDecisionId's IsCube must agree with the decision's kind.";

    /// <summary>Whether <paramref name="id"/> agrees with a decision of <paramref name="kind"/>.</summary>
    internal static bool IdAgrees(DecisionId id, DecisionKind kind) =>
        id is not XgDecisionId xg || xg.IsCube == (kind == DecisionKind.Cube);

    // -----------------------------------------------------------------------
    //  A standalone position's start (halheinrich/backgammon#124)
    //
    //  Whether the game started from the standard opening position is a fact
    //  about a game. A standalone position (an XgpDecisionId) belongs to no
    //  game, so it has none; a decision in a game (an XgDecisionId) has one.
    // -----------------------------------------------------------------------

    /// <summary>The start rule, in the one sentence every refusal carries.</summary>
    internal const string StartMessage =
        "IsStandardStart is a fact about a game: null exactly for a decision in a standalone position (an XgpDecisionId), stated for a decision in a game (an XgDecisionId).";

    /// <summary>Whether <paramref name="isStandardStart"/> agrees with <paramref name="id"/>.</summary>
    internal static bool StartAgrees(DecisionId id, bool? isStandardStart) =>
        (id.GameInFile is null) == (isStandardStart is null);
}
