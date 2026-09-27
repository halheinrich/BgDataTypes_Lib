using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A decision position has a checker of each side on the board or the bar
/// (<see cref="PositionData"/>; halheinrich/backgammon#273, Hal's ruling of
/// 2026-09-27). A board where the player on roll has none, where the opponent
/// has none, or where neither has — the empty board — is a well-formed
/// <see cref="BoardPosition"/>, and no decision is made on it. The category
/// refuses it wherever a decision position is built: from code, with the
/// guard's <see cref="ArgumentException"/> naming <see cref="PositionData.Mop"/>,
/// so neither decision kind can be built on it; from a document read as the
/// category, or as a record of either kind read as the base or as its kind,
/// with a <see cref="JsonException"/> carrying that exception, on both paths;
/// and from a row of either kind read back, with a <see cref="JsonException"/>
/// stating the rule. The bar counts as the board: a side whose one checker is
/// on the bar has a checker.
/// </summary>
public class PositionBoardRuleTests
{
    /// <summary>The boards a decision position refuses, named by what they lack.</summary>
    private static readonly Dictionary<string, BoardPosition> RefusedBoards = new()
    {
        // The player on roll has borne off all fifteen; the opponent's are home.
        ["no checker of the player on roll"] = new(
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -3, -3, -2, -2, -2, 0]),
        // The opponent has borne off all fifteen; the player on roll's are home.
        ["no checker of the opponent"] = new(
            [0, 3, 3, 3, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
        ["no checker of either: the empty board"] = BoardPosition.Empty,
    };

    /// <summary>
    /// Boards a decision position accepts, at the rule's edge: a side with one
    /// checker left, on a point or on the bar.
    /// </summary>
    private static readonly Dictionary<string, BoardPosition> AcceptedBoards = new()
    {
        ["one checker each, on points"] = new(
            [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0]),
        ["the player on roll's one checker, on the bar"] = new(
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, 0, 0, 0, 0, 0, 1]),
        ["the opponent's one checker, on the bar"] = new(
            [-1, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
        ["the standard start"] = BoardPosition.Standard,
    };

    public static TheoryData<string> Refused => [.. RefusedBoards.Keys];

    public static TheoryData<string> Accepted => [.. AcceptedBoards.Keys];

    private static JsonNode Json(BoardPosition board) => JsonSerializer.SerializeToNode(board, WirePaths.Context)!;

    /// <summary>
    /// Asserts <paramref name="json"/> read as <paramref name="readAs"/> is
    /// refused on both paths by the decision-position rule itself: a
    /// <see cref="JsonException"/> carrying the guard's exception, which names
    /// <see cref="PositionData.Mop"/> and states the rule — never another
    /// layer's refusal.
    /// </summary>
    private static void AssertRefusedByTheRule(string json, Type readAs)
    {
        foreach (var (path, options) in WirePaths.Both)
        {
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, readAs, options));
            var guard = Assert.IsType<ArgumentException>(ex.InnerException);
            Assert.True(guard.ParamName == "Mop", $"read as {readAs.Name} on the {path} path: the guard names {guard.ParamName}");
            Assert.Contains(PositionData.BoardMessage, ex.Message);
        }
    }

    // ── From code ────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Refused))]
    public void ABoardWithoutACheckerOfEachSide_IsRefusedByTheCategory_SoNeitherKindCanBeBuiltOnIt(string board)
    {
        var mop = RefusedBoards[board];

        var ex = Assert.Throws<ArgumentException>(() => new PositionData
        {
            Mop = mop,
            CubeSize = 1,
            CubeOwner = CubeOwner.Centered,
            Session = TestRecords.MatchSession(),
        });
        Assert.Equal("Mop", ex.ParamName);
        Assert.Contains(PositionData.BoardMessage, ex.Message);

        // Both kinds inherit it: each takes the category, which cannot exist.
        Assert.Equal("Mop", Assert.Throws<ArgumentException>(
            () => TestRecords.CheckerPlay(position: TestRecords.Position(mop: mop))).ParamName);
        Assert.Equal("Mop", Assert.Throws<ArgumentException>(
            () => TestRecords.Cube(position: TestRecords.Position(mop: mop))).ParamName);
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public void ABoardWithoutACheckerOfEachSide_IsStillAPosition(string board)
    {
        // Control: the rule is the decision position's, not the board's
        // (BoardPosition stays broad, by the same ruling) — its counts form a
        // position, from its own door and as a working board.
        Span<int> counts = stackalloc int[26];
        RefusedBoards[board].CopyTo(counts);

        Assert.True(BoardPosition.TryCreate(counts, out var position));
        Assert.Equal(RefusedBoards[board], new BoardPosition(counts));
        Assert.Equal(position, new BoardState(position).ToPosition());
    }

    // ── From a document, on both paths ───────────────────────────

    [Theory]
    [MemberData(nameof(Refused))]
    public void ADocument_StatingSuchABoard_IsRefused_ReadAsTheCategory(string board)
    {
        var document = WirePaths.Document(TestRecords.Position());
        document["Mop"] = Json(RefusedBoards[board]);

        AssertRefusedByTheRule(document.ToJsonString(), typeof(PositionData));
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public void ADocument_StatingSuchABoard_IsRefused_ForEitherKind_ReadAsTheBaseAndAsTheKind(string board)
    {
        foreach (var record in new BgDecisionData[] { TestRecords.CheckerPlay(), TestRecords.Cube() })
        {
            var document = WirePaths.Document(record);
            document["Position"]!["Mop"] = Json(RefusedBoards[board]);
            string json = document.ToJsonString();

            AssertRefusedByTheRule(json, typeof(BgDecisionData));
            AssertRefusedByTheRule(json, record.GetType());
        }
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public void ARow_StatingSuchABoard_IsRefused_ForEitherKind(string board)
    {
        // A row is a record's projection, so it holds its board to the
        // record's rule when read back.
        foreach (var record in new BgDecisionData[] { TestRecords.CheckerPlay(), TestRecords.Cube() })
        {
            var document = WirePaths.Document(TestRecords.Row(record));
            document["Board"] = Json(RefusedBoards[board]);
            string json = document.ToJsonString();

            foreach (var (path, options) in WirePaths.Both)
            {
                var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionRow>(json, options));
                Assert.True(ex.Message == PositionData.BoardMessage, $"a {record.Kind} row on the {path} path: {ex.Message}");
            }
        }
    }

    // ── The rule's edge: a side with one checker left ────────────

    [Theory]
    [MemberData(nameof(Accepted))]
    public void ABoardWithACheckerOfEachSide_IsADecisionPosition_ForEitherKind_AndReadsBack(string board)
    {
        // Control for every refusal above: the bar counts, and one checker is
        // enough.
        var mop = AcceptedBoards[board];
        var records = new BgDecisionData[]
        {
            TestRecords.CheckerPlay(position: TestRecords.Position(mop: mop)),
            TestRecords.Cube(position: TestRecords.Position(mop: mop)),
        };

        foreach (var record in records)
        {
            Assert.Equal(mop, record.Position.Mop);
            foreach (var (_, options) in WirePaths.Both)
            {
                Assert.Equal(mop, WirePaths.RoundTrip(record, options).Position.Mop);
                Assert.Equal(mop, WirePaths.RoundTrip(record.Position, options).Mop);
                Assert.Equal(mop, WirePaths.RoundTrip(TestRecords.Row(record), options).Board);
            }
        }
    }
}
