using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the gammon fact of SPEC-scoring §3 (amended 2026-10-01 on
/// halheinrich/backgammon#326), <see cref="CubeDecision.GammonsPossible"/>:
/// gammons are not possible when the opponent has a checker borne off, when a
/// match's cube is at least the points the player on roll needs, or in money
/// under the Jacoby rule with the cube centred. Each gate on its own, each
/// with its complement, through the full decision — the position, the
/// session, the score and the cube. It replaced the Too good offerability
/// fact, whose suite this was: the fourth answer is always offered now.
/// </summary>
public class CubeDecisionGammonsPossibleTests
{
    private static readonly BoardPosition Race = CubeAt.Race;

    private static readonly BoardPosition OpponentBorneOff = CubeAt.OpponentBorneOff;

    // The race with one of the player on roll's checkers borne off instead,
    // from its 6-point: the gate is the opponent's alone.
    private static readonly BoardPosition OnRollBorneOff = new(
        [0, 2, 2, 3, 3, 3, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -3, -2, -1, -1, 0]);

    private static CubeDecision Make(
        BoardPosition board, Session session, int cubeSize = 1, CubeOwner cubeOwner = CubeOwner.Centered,
        double gammonPctAfterNoDouble = 0.012)
        => TestRecords.Cube(
            position: TestRecords.Position(mop: board, cubeSize: cubeSize, cubeOwner: cubeOwner, session: session),
            decision: TestRecords.CubeData(gammonPctAfterNoDouble: gammonPctAfterNoDouble));

    private static MatchSession Match(int onRollNeeds, int opponentNeeds = 7) =>
        TestRecords.MatchSession(length: 7, onRollNeeds: onRollNeeds, opponentNeeds: opponentNeeds);

    private static MoneySession Money(bool isJacoby) => TestRecords.MoneySession(isJacoby: isJacoby);

    // ---------------------------------------------------------------------
    //  The first gate: the opponent has a checker borne off
    // ---------------------------------------------------------------------

    [Fact]
    public void OpponentBorneOff_GammonsAreNotPossible_AndItsComplementIs()
    {
        // The other two gates held open: a match with the cube below the
        // points needed, and money without the Jacoby rule.
        Assert.False(Make(OpponentBorneOff, Match(onRollNeeds: 7)).GammonsPossible);
        Assert.False(Make(OpponentBorneOff, Money(isJacoby: false)).GammonsPossible);

        Assert.True(Make(Race, Match(onRollNeeds: 7)).GammonsPossible);
        Assert.True(Make(Race, Money(isJacoby: false)).GammonsPossible);
    }

    // The player on roll's own borne-off checkers decide nothing: a gammon
    // the player on roll wins needs only the opponent to have none off.
    [Fact]
    public void OnRollBorneOff_LeavesGammonsPossible()
    {
        Assert.True(Make(OnRollBorneOff, Match(onRollNeeds: 7)).GammonsPossible);
        Assert.True(Make(OnRollBorneOff, Money(isJacoby: false)).GammonsPossible);
    }

    // ---------------------------------------------------------------------
    //  The second gate: in a match, the cube at least the points needed
    // ---------------------------------------------------------------------

    [Theory]
    // (cube, on roll needs, expected): possible exactly while the cube is
    // below the points the player on roll needs.
    [InlineData(1, 2, true)]
    [InlineData(1, 1, false)]   // at the points needed
    [InlineData(2, 3, true)]
    [InlineData(2, 2, false)]   // at the points needed
    [InlineData(2, 1, false)]   // above them
    [InlineData(4, 5, true)]
    [InlineData(4, 4, false)]
    [InlineData(8, 7, false)]
    public void MatchCube_AtLeastThePointsNeeded_GammonsAreNotPossible(int cubeSize, int onRollNeeds, bool expected)
    {
        var owner = cubeSize == 1 ? CubeOwner.Centered : CubeOwner.OnRoll;

        Assert.Equal(expected, Make(Race, Match(onRollNeeds), cubeSize, owner).GammonsPossible);
    }

    // The opponent's needs decide nothing: the gate reads the player on
    // roll's.
    [Fact]
    public void MatchCube_ReadsOnlyThePlayerOnRollsNeeds()
    {
        Assert.True(Make(Race, Match(onRollNeeds: 3, opponentNeeds: 1), cubeSize: 2, CubeOwner.Opponent).GammonsPossible);
        Assert.False(Make(Race, Match(onRollNeeds: 2, opponentNeeds: 7), cubeSize: 2, CubeOwner.Opponent).GammonsPossible);
    }

    // ---------------------------------------------------------------------
    //  The third gate: money under the Jacoby rule, the cube centred
    // ---------------------------------------------------------------------

    [Fact]
    public void MoneyJacobyCentred_GammonsAreNotPossible()
    {
        Assert.False(Make(Race, Money(isJacoby: true)).GammonsPossible);
    }

    // Its complements: the cube turned, either owner, re-arms gammons; and
    // without the Jacoby rule they count from the start.
    [Theory]
    [InlineData(true, CubeOwner.OnRoll)]
    [InlineData(true, CubeOwner.Opponent)]
    [InlineData(false, CubeOwner.Centered)]
    [InlineData(false, CubeOwner.OnRoll)]
    public void MoneyJacobyCentred_ItsComplements_GammonsArePossible(bool isJacoby, CubeOwner owner)
    {
        int cubeSize = owner == CubeOwner.Centered ? 1 : 2;

        Assert.True(Make(Race, Money(isJacoby), cubeSize, owner).GammonsPossible);
    }

    // ---------------------------------------------------------------------
    //  The three together
    // ---------------------------------------------------------------------

    // Every combination of the board and the session's posture: gammons are
    // possible exactly when no gate is closed, so a spelling that missed a
    // gate, or closed one that is open, fails here.
    [Fact]
    public void GammonsArePossible_ExactlyWhenNoGateIsClosed()
    {
        (string Name, Session Session, int Cube, CubeOwner Owner, bool Closed)[] postures =
        [
            ("money, Jacoby, centred", Money(isJacoby: true), 1, CubeOwner.Centered, true),
            ("money, Jacoby, turned", Money(isJacoby: true), 2, CubeOwner.OnRoll, false),
            ("money, no Jacoby, centred", Money(isJacoby: false), 1, CubeOwner.Centered, false),
            ("match, cube below the needs", Match(onRollNeeds: 5), 2, CubeOwner.Opponent, false),
            ("match, cube at the needs", Match(onRollNeeds: 2), 2, CubeOwner.Opponent, true),
        ];
        (string Name, BoardPosition Board, bool Closed)[] boards =
        [
            ("nothing borne off", Race, false),
            ("on roll's borne off", OnRollBorneOff, false),
            ("opponent's borne off", OpponentBorneOff, true),
        ];

        foreach (var (boardName, board, boardClosed) in boards)
            foreach (var (name, session, cube, owner, closed) in postures)
                Assert.True(
                    !(boardClosed || closed) == Make(board, session, cube, owner).GammonsPossible,
                    $"{boardName}; {name}");
    }

    // ---------------------------------------------------------------------
    //  A fact of the position, not of the analysis
    // ---------------------------------------------------------------------

    // The analyser's gammon chances are not part of the fact: no gammon
    // chance with every gate open is gammons possible, and a large one with a
    // gate closed is not.
    [Fact]
    public void TheAnalysersGammonChances_DecideNothing()
    {
        Assert.True(Make(Race, Match(onRollNeeds: 7), gammonPctAfterNoDouble: 0.0).GammonsPossible);
        Assert.False(Make(Race, Money(isJacoby: true), gammonPctAfterNoDouble: 0.9).GammonsPossible);
        Assert.False(Make(OpponentBorneOff, Match(onRollNeeds: 7), gammonPctAfterNoDouble: 0.9).GammonsPossible);
    }

    // ---------------------------------------------------------------------
    //  A cube decision's member only, and serialization posture
    // ---------------------------------------------------------------------

    [Fact]
    public void GammonsPossible_IsACubeDecisionsMemberOnly_AndOfferabilityIsGone()
    {
        Assert.NotNull(typeof(CubeDecision).GetProperty(nameof(CubeDecision.GammonsPossible)));
        Assert.Null(typeof(CheckerPlayDecision).GetProperty(nameof(CubeDecision.GammonsPossible)));
        Assert.Null(typeof(BgDecisionData).GetProperty(nameof(CubeDecision.GammonsPossible)));
        Assert.Null(typeof(CubeDecisionData).GetProperty(nameof(CubeDecision.GammonsPossible)));
        Assert.Null(typeof(CubeDecision).GetProperty("CanBeTooGood"));
    }

    // A derivation, not wire (halheinrich/backgammon#14).
    [Fact]
    public void GammonsPossible_IsNotSerialised_BothPaths()
    {
        foreach (var (_, options) in WirePaths.Both)
        {
            string json = JsonSerializer.Serialize<BgDecisionData>(Make(OpponentBorneOff, Money(isJacoby: true)), options);
            Assert.DoesNotContain("GammonsPossible", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
