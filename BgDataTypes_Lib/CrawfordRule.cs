namespace BgDataTypes_Lib;

/// <summary>
/// The Crawford rule as it binds the wire records
/// (halheinrich/backgammon#201): doubling is prohibited in the Crawford
/// game, so a cube decision flagged Crawford describes a decision that
/// cannot exist. This is the single spelling of the rule and of its
/// rejection. Each record's two init setters call it from whichever half
/// is set second (<see cref="BgDecisionData.Position"/> /
/// <see cref="BgDecisionData.Decision"/>; <see cref="DecisionRow.Roll"/> /
/// <see cref="DecisionRow.IsCrawford"/>), so an object initializer in
/// either member order and a JSON document in either property order fail
/// the same way, at construction. <c>BgGame_Lib</c>'s <c>GameState</c>
/// states the rule again for live play, and the two stay separate by
/// layering: that one is a three-reason legality predicate over live match
/// state (no cube, Crawford, opponent owns it), this one a wire-record
/// invariant.
/// </summary>
internal static class CrawfordRule
{
    /// <summary>The rule, in the one sentence every rejection carries.</summary>
    internal const string Message =
        "Doubling is prohibited in the Crawford game, so a cube decision cannot carry the Crawford flag.";

    /// <summary>
    /// Rejects the Crawford-cube combination. Called by an init setter with
    /// the incoming value of its own half and the already-set value of the
    /// other; a half still at its default never trips it.
    /// </summary>
    /// <param name="isCrawford">The record's Crawford flag as it would stand after the set.</param>
    /// <param name="isCube">Whether the record would be a cube decision after the set.</param>
    /// <param name="paramName">The member being set — the one the exception names.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both facts hold: the member named by
    /// <paramref name="paramName"/> would complete a Crawford cube.
    /// </exception>
    internal static void ThrowIfCrawfordCube(bool isCrawford, bool isCube, string paramName)
    {
        if (isCrawford && isCube)
            throw new ArgumentException(Message, paramName);
    }
}
