using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the filter view's <see cref="IDecisionFilterData.PlayerResult"/>
/// (<see cref="BgDecisionData.ViewFor"/>) for each kind, under each ranking,
/// each case by name, and a cube decision's taker members
/// (<see cref="IDecisionFilterData.Taker"/>, <see cref="IDecisionFilterData.TakerResult"/>).
/// A cube decision's player result is the doubler's half alone, whatever the
/// ranking: a stated action scored with its derived error, an unstated one
/// <see cref="PlayerResultKind.Unstated"/> with the analyser's, or not
/// recorded — never the taker's, which is its own member where a double was
/// offered (SPEC-filtering §8, halheinrich/backgammon#440). A checker play's
/// is the ranking's <see cref="RankedPlays.PlayerResult"/>. The records here
/// rank alike under both rankings; the case only depth first has, not scored,
/// is pinned in <see cref="PlayRankingTests"/>.
///
/// Renamed from BgDecisionDataFilterErrorTests when the view's error became
/// a result naming its case (the umbrella's third-round ruling). Originally
/// reconstructed from two cases dropped when XgFilter_Lib's filter suite
/// consolidated onto DecisionFilterAsserts; the logic they covered lives on
/// <see cref="BgDecisionData"/>, so the tests belong here.
/// </summary>
public class BgDecisionDataPlayerResultTests
{
    /// <summary>The record's filter view under each ranking.</summary>
    private static IEnumerable<IDecisionFilterData> Views(BgDecisionData record) =>
        Enum.GetValues<PlayRanking>().Select(record.ViewFor);

    /// <summary>The result's error, which the test has established it has.</summary>
    private static double ErrorOf(PlayerResult result) =>
        result.TryGetError(out double error) ? error : throw new InvalidOperationException($"{result} has no error.");

    /// <summary>
    /// A too-good position the user doubled and the opponent took: no double
    /// +1.042 against a cash of 1 is a doubling error of 0.042, and
    /// double/take +1.017 is a take error of 0.017.
    /// </summary>
    private static CubeDecisionData TooGoodDoubledAndTaken(CubeAction? doubled = CubeAction.Double) =>
        TestRecords.CubeData(
            noDoubleEquity: 1.042, doubleTakeEquity: 1.017,
            userDoublerAction: doubled, userTakerAction: CubeAction.Take);

    [Fact]
    public void Cube_AStatedDouble_IsScoredWithTheDoublingError()
    {
        // Rewritten: the errors are no longer stated beside the actions they
        // score; they are derived from the stated actions and the equities.
        var d = TestRecords.Cube(decision: TooGoodDoubledAndTaken());

        // Doubling error present → it wins over the take error.
        Assert.All(Views(d), view =>
        {
            Assert.Equal(PlayerResultKind.Scored, view.PlayerResult.Kind);
            Assert.Equal(0.042, ErrorOf(view.PlayerResult), 12);
        });
        Assert.Equal(0.017, d.Decision.UserTakeError!.Value, 12);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CubeAction.NoDouble)]
    public void Cube_ATakerHalfWithoutAStatedDouble_CannotBeBuilt_SoThereIsNoFallbackToIt(CubeAction? doubled)
    {
        // Rewritten from Cube_NoDoublerResult_FallsBackToTheTakersScoredError
        // (halheinrich/backgammon#440): a take without an offered double is
        // invalid data (SPEC-scoring §3, 2026-10-25), and the record refuses
        // it, so the view's fallback to the taker's result had nothing left
        // to read and is gone.
        var ex = Assert.Throws<ArgumentException>(() => TooGoodDoubledAndTaken(doubled));

        Assert.Equal("takerAction", ex.ParamName);
    }

    [Theory]
    [InlineData(null, 0.042, null)]
    [InlineData(CubeAction.Double, null, 0.013)]
    public void Cube_AnUnstatedActionsError_IsUnstated_TheAnalysersNumber_OnItsOwnHalf(
        CubeAction? doubled, double? doubler, double? taker)
    {
        // Added: where the record states no action, the analyser's error is
        // the only statement of it, and the view names it unstated.
        // Rewritten for the two halves (halheinrich/backgammon#440): the
        // doubler's unstated error is the player result; the taker's, under a
        // stated Double, is the taker result, and the player result is the
        // doubling scored.
        var d = TestRecords.Cube(decision: TestRecords.CubeData(
            userDoublerAction: doubled, userTakerAction: null,
            unstatedDoublerActionError: doubler, unstatedTakerActionError: taker));

        Assert.All(Views(d), view =>
        {
            if (doubler is double doublerError)
            {
                Assert.Equal(PlayerResult.Unstated(doublerError), view.PlayerResult);
                Assert.Null(view.TakerResult);
            }
            else
            {
                Assert.Equal(PlayerResult.Scored(d.Decision.DoublerActionError(CubeAction.Double)), view.PlayerResult);
                Assert.Equal(PlayerResult.Unstated(taker!.Value), view.TakerResult);
            }
        });
    }

    [Fact]
    public void Checker_AScoredPlay_IsScoredWithItsError()
    {
        // Rewritten: the user played the default's second candidate, 13/10
        // 6/5, whose equity loss against the best is 0.1604 − (−0.0127).
        var d = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 1));

        Assert.All(Views(d), view =>
        {
            Assert.Equal(PlayerResultKind.Scored, view.PlayerResult.Kind);
            Assert.Equal(0.1731, ErrorOf(view.PlayerResult), 12);
        });
    }

    [Fact]
    public void Checker_AnUnlistedPlay_IsUnstated_TheAnalysersNumber()
    {
        // Added: a user play outside the candidates has no equity here to
        // derive from; the analyser's number is its one statement.
        var d = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(
            userPlayIndex: null, unlistedPlayError: 0.031));

        Assert.All(Views(d), view => Assert.Equal(PlayerResult.Unstated(0.031), view.PlayerResult));
    }

    [Fact]
    public void NoUserDecisionRecorded_IsNotRecorded_EachKind()
    {
        // Added: "none recorded" is its own case on both kinds, with no error
        // — never a zero, and never confused with a move that is not scored.
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: null));
        var cube = TestRecords.Cube(decision: TestRecords.CubeData(userDoublerAction: null, userTakerAction: null));

        Assert.All(Views(play).Concat(Views(cube)), view =>
        {
            Assert.Equal(PlayerResult.NotRecorded, view.PlayerResult);
            Assert.False(view.PlayerResult.TryGetError(out _));
        });
    }

    [Fact]
    public void ACubeDecision_IsNeverNotScored()
    {
        // No ranking scores a cube decision; every case it has is one of the
        // other three, over each shape of what the record states. Rewritten
        // for the recorded cube actions: the shapes are the value's six (a
        // taker half under no stated double is no shape at all), and the
        // taker's result is never not scored either.
        foreach (var shape in AllShapes)
            Assert.All(Views(CubeWith(shape)), view =>
            {
                Assert.NotEqual(PlayerResultKind.NotScored, view.PlayerResult.Kind);
                Assert.NotEqual(PlayerResultKind.NotScored, view.TakerResult?.Kind);
            });
    }

    // -----------------------------------------------------------------------
    //  The taker's decision (SPEC-filtering §8, halheinrich/backgammon#440)
    // -----------------------------------------------------------------------

    /// <summary>Every valid shape of the recorded cube actions.</summary>
    private static readonly RecordedCubeActions[] AllShapes =
    [
        RecordedCubeActions.None,
        RecordedCubeActions.NoDouble,
        RecordedCubeActions.DoubleUnanswered,
        RecordedCubeActions.DoubleAnswered(CubeAction.Take),
        RecordedCubeActions.DoubleAnswered(CubeAction.Pass),
        RecordedCubeActions.DoubleWithUnstatedResponse(0.013),
        RecordedCubeActions.UnstatedDoubler(0.042),
    ];

    /// <summary>
    /// The too-good position of <see cref="TooGoodDoubledAndTaken"/> — a
    /// doubling error of 0.042, a take error of 0.017 and a pass error of 0 —
    /// with <paramref name="recorded"/> its recorded actions, Alice on roll
    /// against Bob unless the names are given.
    /// </summary>
    private static CubeDecision CubeWith(
        RecordedCubeActions recorded, string? onRollName = "Alice", string? opponentName = "Bob") =>
        TestRecords.Cube(
            decision: TestRecords.CubeData(
                noDoubleEquity: 1.042, doubleTakeEquity: 1.017,
                userDoublerAction: recorded.DoublerAction, userTakerAction: recorded.TakerAction,
                unstatedDoublerActionError: recorded.UnstatedDoublerError,
                unstatedTakerActionError: recorded.UnstatedTakerError),
            descriptive: TestRecords.Descriptive(onRollName: onRollName, opponentName: opponentName));

    public static TheoryData<string, string?, string?, string> TakerMembers => new()
    {
        // (shape, Taker, TakerResult, PlayerResult)
        { "None", null, null, "NotRecorded" },
        { "NoDouble", null, null, "Scored 0" },
        { "UnstatedDoubler", null, null, "Unstated 0.042" },
        { "DoubleAnswered Take", "Bob", "Scored 0.017", "Scored 0.042" },
        { "DoubleAnswered Pass", "Bob", "Scored 0", "Scored 0.042" },
        { "DoubleWithUnstatedResponse", "Bob", "Unstated 0.013", "Scored 0.042" },
        { "DoubleUnanswered", "Bob", "NotRecorded", "Scored 0.042" },
    };

    private static RecordedCubeActions Shape(string name) => name switch
    {
        "None" => RecordedCubeActions.None,
        "NoDouble" => RecordedCubeActions.NoDouble,
        "UnstatedDoubler" => RecordedCubeActions.UnstatedDoubler(0.042),
        "DoubleAnswered Take" => RecordedCubeActions.DoubleAnswered(CubeAction.Take),
        "DoubleAnswered Pass" => RecordedCubeActions.DoubleAnswered(CubeAction.Pass),
        "DoubleWithUnstatedResponse" => RecordedCubeActions.DoubleWithUnstatedResponse(0.013),
        "DoubleUnanswered" => RecordedCubeActions.DoubleUnanswered,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    /// <summary>A result as <see cref="PlayerResult.ToString"/> spells it, its error to twelve places.</summary>
    private static string? Spelled(PlayerResult? result) =>
        result is not { } r ? null
        : r.TryGetError(out double error) ? $"{r.Kind} {Math.Round(error, 12).ToString(System.Globalization.CultureInfo.InvariantCulture)}"
        : r.Kind.ToString();

    [Theory]
    [MemberData(nameof(TakerMembers))]
    public void Cube_TheTakersNameAndResult_AndThePlayersResult_ForEachShape(
        string shape, string? taker, string? takerResult, string playerResult)
    {
        // A cube record is two decisions: the doubler's is the player result,
        // and the taker's — only where a double was offered — is the taker's
        // name and result. Here doubling is a 0.042 error and not doubling
        // none; the take a 0.017 error, the pass none. A double the opponent answered by
        // resigning is a decision the taker was offered, not recorded (Hal,
        // 2026-10-26); the double itself is recorded, so it is scored.
        var record = CubeWith(Shape(shape));

        Assert.All(Views(record), view =>
        {
            Assert.Equal(taker, view.Taker);
            Assert.Equal(takerResult, Spelled(view.TakerResult));
            Assert.Equal(playerResult, Spelled(view.PlayerResult));
            Assert.Equal("Alice", view.Player);
        });
    }

    [Fact]
    public void Cube_AnOfferedDoubleWithNoNamesRecorded_HasATakerResult_AndNoTakersName()
    {
        // The taker's result is stated wherever a double was offered, name or
        // no name; the name is null where the source recorded none.
        var record = CubeWith(RecordedCubeActions.DoubleAnswered(CubeAction.Take), onRollName: null, opponentName: null);

        Assert.All(Views(record), view =>
        {
            Assert.Null(view.Taker);
            Assert.Null(view.Player);
            Assert.Equal(PlayerResultKind.Scored, view.TakerResult?.Kind);
        });
    }

    [Fact]
    public void Cube_TheTaker_IsTheOpponentOfThePlayerOnRoll()
    {
        // Never the on-roll name: the taker answers the player on roll's double.
        var record = CubeWith(RecordedCubeActions.DoubleUnanswered, onRollName: "Hal", opponentName: "Mochy");

        Assert.All(Views(record), view => Assert.Equal(("Hal", "Mochy"), (view.Player, view.Taker)));
    }

    [Fact]
    public void Checker_HasNoTakerNameOrResult()
    {
        var play = TestRecords.CheckerPlay();

        Assert.All(Views(play), view =>
        {
            Assert.Null(view.Taker);
            Assert.Null(view.TakerResult);
        });
    }
}
