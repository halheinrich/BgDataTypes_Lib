using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Every record has a key (halheinrich/backgammon#273, Hal's ruling of
/// 2026-09-27): <see cref="ProblemKey.From"/> has no failure case, because a
/// record's construction holds every fact the key reads to rules at least as
/// strict as those the parse door holds a key's text to — the board most of
/// all, a decision position having a checker of each side
/// (<see cref="PositionData"/>). Pinned as a property over records of both
/// kinds, swept across each of those rules' edges: boards down to one
/// checker a side, on a point or on the bar; every match standing of a short
/// match, the Crawford game included, and a long one; money under each of its
/// rules; the cube at 1 and at its limits, with each owner; every roll in
/// both orders. Each record's key reads back from its text as an equal key
/// of the record's kind, and the lot read back as a statistics document's
/// keys through the bundled converter — so no record's key can fail the
/// document it is written to.
/// </summary>
public class ProblemKeyTotalityTests
{
    private static readonly BoardPosition[] Boards =
    [
        BoardPosition.Standard,
        // A race.
        new([0, 2, 2, 3, 3, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -3, -2, -1, -1, 0]),
        // A checker of each side on the bar.
        new([-1, 2, 0, 0, 0, 0, 4, 0, 3, 0, 0, 0, -5, 4, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 1, 1]),
        // One checker each, on points.
        new([0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0]),
        // The player on roll's one checker, on the bar.
        new([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, 0, 0, 0, 0, 0, 1]),
        // The opponent's one checker, on the bar.
        new([-1, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
        // One checker left against fifteen.
        new([0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -5, 0, 0, 0, 0, -3, 0, -3, -2, -2, 0, 0, 0, 0]),
    ];

    /// <summary>
    /// Every standing of a 3-point match, the Crawford game at each of its
    /// four; a 25-point match at its start and in its Crawford game; money
    /// under each of its rules, at the session's start and later.
    /// </summary>
    private static IEnumerable<Session> Sessions()
    {
        for (int onRoll = 1; onRoll <= 3; onRoll++)
            for (int opponent = 1; opponent <= 3; opponent++)
            {
                yield return TestRecords.MatchSession(length: 3, onRollNeeds: onRoll, opponentNeeds: opponent);
                if ((onRoll == 1) != (opponent == 1))
                    yield return TestRecords.MatchSession(length: 3, onRollNeeds: onRoll, opponentNeeds: opponent, isCrawford: true);
            }
        yield return TestRecords.MatchSession(length: 25, onRollNeeds: 25, opponentNeeds: 25);
        yield return TestRecords.MatchSession(length: 25, onRollNeeds: 1, opponentNeeds: 25, isCrawford: true);
        foreach (bool isJacoby in new[] { true, false })
            foreach (bool isBeaver in new[] { true, false })
                yield return TestRecords.MoneySession(isJacoby: isJacoby, isBeaver: isBeaver);
        yield return TestRecords.MoneySession(onRollScore: 7, opponentScore: 2);
    }

    /// <summary>The cube at 1, doubled, high, at money's default limit, and past it (the record holds no limit for a match).</summary>
    private static readonly int[] CubeSizes = [1, 2, 64, 1024, 4096];

    /// <summary>Every roll, in both orders.</summary>
    private static readonly int[][] Rolls =
        [.. Enumerable.Range(1, 6).SelectMany(first => Enumerable.Range(1, 6).Select(second => new[] { first, second }))];

    /// <summary>
    /// Every record of <paramref name="kind"/> the sweep's facts can build:
    /// a cube decision is not made in the Crawford game, and a money
    /// session's cube never passes its limit, so those combinations are no
    /// records and are not built. A checker play holds the pass, the one play
    /// valid from every board, and takes the rolls in turn.
    /// </summary>
    private static IEnumerable<BgDecisionData> Records(DecisionKind kind)
    {
        int roll = 0;
        foreach (var board in Boards)
            foreach (var session in Sessions())
                foreach (int cubeSize in CubeSizes)
                    foreach (var cubeOwner in Enum.GetValues<CubeOwner>())
                    {
                        if (kind == DecisionKind.Cube && session is MatchSession { IsCrawford: true })
                            continue;
                        if (session is MoneySession money && cubeSize > money.Terms.CubeLimit)
                            continue;

                        var position = TestRecords.Position(mop: board, cubeSize: cubeSize, cubeOwner: cubeOwner, session: session);
                        yield return kind == DecisionKind.Cube
                            ? TestRecords.Cube(position: position)
                            : TestRecords.CheckerPlay(
                                position: position,
                                decision: TestRecords.CheckerPlayData(
                                    dice: Rolls[roll++ % Rolls.Length],
                                    plays: [TestRecords.Candidate(play: [])]));
                    }
    }

    public static TheoryData<DecisionKind> Kinds => [DecisionKind.CheckerPlay, DecisionKind.Cube];

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EveryRecord_HasAKey_ThatReadsBackAsItself(DecisionKind kind)
    {
        var keys = new Dictionary<ProblemKey, int>();
        var rolls = new HashSet<(int, int)>();
        int records = 0;

        foreach (var record in Records(kind))
        {
            records++;
            var key = ProblemKey.From(record);
            string text = key.ToString();

            Assert.True(ProblemKey.TryParse(text, null, out var parsed), $"{text} does not parse");
            Assert.Equal(key, parsed);
            Assert.Equal(text, parsed.ToString());
            Assert.Equal(kind == DecisionKind.Cube, key.IsCubeDecision);
            Assert.Equal(kind == DecisionKind.Cube, parsed.IsCubeDecision);

            keys[key] = records;
            if (record is CheckerPlayDecision play)
                rolls.Add((play.Decision.Dice[0], play.Decision.Dice[1]));
        }

        // The sweep is not vacuous: it built records, and the checker plays
        // took every roll in both orders.
        Assert.True(records > 1000, $"{records} records");
        Assert.Equal(kind == DecisionKind.CheckerPlay ? 36 : 0, rolls.Count);

        // Written as a statistics document keys its problems — a map keyed by
        // the key, read through the converter's property-name overloads —
        // every key reads back. (The document is a consumer's, so this
        // library's context declares no such map; the reflection path reads
        // it, as Json_WorksAsDictionaryKey does.)
        var read = JsonSerializer.Deserialize<Dictionary<ProblemKey, int>>(JsonSerializer.Serialize(keys))!;
        Assert.Equal(keys.Count, read.Count);
        Assert.All(keys, entry => Assert.Equal(entry.Value, read[entry.Key]));
    }
}
