using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Full cube decisions at given equities, with gammons possible or not —
/// what the cube-answer suites build (halheinrich/backgammon#326). Each is a
/// whole record, built through <see cref="TestRecords"/>: the gammon fact is
/// the position's and the session's, so no suite states it as a flag.
/// </summary>
internal static class CubeAt
{
    /// <summary>The builders' short race (54 pips to 65): nothing borne off on either side.</summary>
    internal static readonly BoardPosition Race = TestRecords.Cube().Position.Mop;

    /// <summary>The same race with one of the opponent's checkers borne off, from its 1-point (slot 24).</summary>
    internal static readonly BoardPosition OpponentBorneOff = new(
        [0, 2, 2, 3, 3, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -3, -2, -1, 0, 0]);

    /// <summary>
    /// A decision where gammons are possible: the race, in a 7-point match at
    /// 0-0, the cube centred on 1 — every gate open (the builder's default).
    /// </summary>
    internal static CubeDecision GammonsPossible(double noDoubleEquity, double doubleTakeEquity) =>
        TestRecords.Cube(decision: Equities(noDoubleEquity, doubleTakeEquity));

    /// <summary>
    /// The same decision where gammons are not possible, by the first gate:
    /// the opponent has a checker borne off.
    /// </summary>
    internal static CubeDecision GammonsNotPossible(double noDoubleEquity, double doubleTakeEquity) =>
        TestRecords.Cube(
            position: TestRecords.Position(mop: OpponentBorneOff),
            decision: Equities(noDoubleEquity, doubleTakeEquity));

    /// <summary>The decision of <paramref name="gammonsPossible"/>'s kind, at the given equities.</summary>
    internal static CubeDecision Decision(bool gammonsPossible, double noDoubleEquity, double doubleTakeEquity) =>
        gammonsPossible
            ? GammonsPossible(noDoubleEquity, doubleTakeEquity)
            : GammonsNotPossible(noDoubleEquity, doubleTakeEquity);

    private static CubeDecisionData Equities(double noDoubleEquity, double doubleTakeEquity) =>
        TestRecords.CubeData(noDoubleEquity: noDoubleEquity, doubleTakeEquity: doubleTakeEquity);
}
