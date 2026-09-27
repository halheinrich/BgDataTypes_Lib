using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// <see cref="ProblemKey"/>'s text did not change when its construction moved
/// to the session kinds (Hal, 2026-09-26; halheinrich/backgammon#273). Users'
/// saved quiz statistics are keyed by that text, so every key must stay
/// byte-identical. A sweep over the builders' variants — each decision kind
/// on four boards, every score shape a key spells (matches at the start,
/// before and after the Crawford game, in it with either player at match
/// point, a long match, money under either rule), and every cube shape —
/// derives each key through today's construction and holds it to the text
/// the construction at <c>ca83ab1</c> wrote for the same facts, captured from
/// that build (the table below, 138 keys).
/// </summary>
/// <remarks>
/// At <c>ca83ab1</c> a variant's scores were flat position members, money
/// 0-away each with the Jacoby fact beside them, and the match length a
/// descriptive member; today the same facts are a session — a match of the
/// larger of 7 and the away scores (the length is no key's fact), or money
/// under the same rule. The capture also checked, over 108 match variants,
/// that a Jacoby stamp on a match changed no key there; a match cannot state
/// one now.
/// </remarks>
public class ProblemKeyByteIdentityTests
{
    private static readonly BoardPosition Race = new(
        [0, 2, 2, 3, 3, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -3, -2, -1, -1, 0]);

    private static readonly BoardPosition Bars = new(
        [-1, 2, 0, 0, 0, 0, 4, 0, 3, 0, 0, 0, -5, 4, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 1, 1]);

    private static readonly (string Name, bool Cube, BoardPosition Mop, int[] Dice)[] Shapes =
    [
        ("play-standard-31", false, BoardPosition.Standard, [3, 1]),
        ("play-standard-13", false, BoardPosition.Standard, [1, 3]),
        ("play-race-66", false, Race, [6, 6]),
        ("play-bars-52", false, Bars, [5, 2]),
        ("cube-race", true, Race, []),
        ("cube-standard", true, BoardPosition.Standard, []),
        ("cube-bars", true, Bars, []),
    ];

    /// <summary>Each score shape, as the session that states it.</summary>
    private static readonly (string Label, Func<Session> Session, bool Crawford)[] Scores =
    [
        ("7a7a", () => Match(7, 7), false),
        ("1a5acr", () => Match(1, 5, isCrawford: true), true),
        ("5a1acr", () => Match(5, 1, isCrawford: true), true),
        ("1a5a", () => Match(1, 5), false),
        ("1a1a", () => Match(1, 1), false),
        ("2a2a", () => Match(2, 2), false),
        ("3a11a", () => Match(3, 11), false),
        ("11a3a", () => Match(11, 3), false),
        ("1a25acr", () => Match(1, 25, isCrawford: true), true),
        ("money-j", () => TestRecords.MoneySession(isJacoby: true), false),
        ("money-nj", () => TestRecords.MoneySession(isJacoby: false), false),
    ];

    private static readonly (string Label, int Size, CubeOwner Owner)[] Cubes =
    [
        ("1c", 1, CubeOwner.Centered),
        ("2o", 2, CubeOwner.OnRoll),
        ("2p", 2, CubeOwner.Opponent),
        ("4o", 4, CubeOwner.OnRoll),
        ("64p", 64, CubeOwner.Opponent),
        ("2c", 2, CubeOwner.Centered),
    ];

    private static MatchSession Match(int onRollNeeds, int opponentNeeds, bool isCrawford = false) =>
        TestRecords.MatchSession(
            length: Math.Max(7, Math.Max(onRollNeeds, opponentNeeds)),
            onRollNeeds: onRollNeeds, opponentNeeds: opponentNeeds, isCrawford: isCrawford);

    /// <summary>
    /// The sweep, in the capture's order: every shape at every score with the
    /// cube centred on 1 (no Crawford cube, which cannot exist), then every
    /// shape at every other cube, at 7-away to 7-away and at money under the
    /// Jacoby rule.
    /// </summary>
    private static IEnumerable<(string Id, BgDecisionData Record)> Sweep()
    {
        foreach (var shape in Shapes)
            foreach (var score in Scores)
                if (!(shape.Cube && score.Crawford))
                    yield return ($"{shape.Name}|{score.Label}|1c", Record(shape, score.Session(), Cubes[0]));
        foreach (var scoreLabel in new[] { "7a7a", "money-j" })
        {
            var score = Scores.Single(s => s.Label == scoreLabel);
            foreach (var shape in Shapes)
                foreach (var cube in Cubes.Skip(1))
                    yield return ($"{shape.Name}|{score.Label}|{cube.Label}", Record(shape, score.Session(), cube));
        }
    }

    private static BgDecisionData Record(
        (string Name, bool Cube, BoardPosition Mop, int[] Dice) shape, Session session, (string Label, int Size, CubeOwner Owner) cube)
    {
        var position = TestRecords.Position(mop: shape.Mop, cubeSize: cube.Size, cubeOwner: cube.Owner, session: session);
        return shape.Cube
            ? TestRecords.Cube(position: position)
            : TestRecords.CheckerPlay(
                position: position,
                decision: shape.Mop == BoardPosition.Standard
                    ? TestRecords.CheckerPlayData(dice: shape.Dice)
                    : TestRecords.CheckerPlayData(dice: shape.Dice, plays: [TestRecords.Candidate(play: [])]));
    }

    [Fact]
    public void EveryKeyOfTheSweep_IsTheTextCa83ab1Wrote()
    {
        var derived = new Dictionary<string, string>();
        foreach (var (id, record) in Sweep())
            derived.Add(id, ProblemKey.From(record).ToString());

        Assert.Equal(AtCa83ab1.Count, derived.Count);
        foreach (var (id, text) in AtCa83ab1)
            Assert.True(derived.TryGetValue(id, out var today) && today == text,
                $"{id}: ca83ab1 wrote {text}, today {today ?? "nothing"}");
    }

    [Fact]
    public void EveryKeyOfTheSweep_ParsesBackToItself()
    {
        // The parse door agrees with the derivation on every captured text.
        foreach (var text in AtCa83ab1.Values)
            Assert.Equal(text, ProblemKey.Parse(text).ToString());
    }

    /// <summary>The keys ca83ab1's construction derived for the sweep, captured from that build.</summary>
    private static readonly Dictionary<string, string> AtCa83ab1 = new()
    {
        ["play-standard-31|7a7a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31",
        ["play-standard-31|1a5acr|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a5cr/1c/31",
        ["play-standard-31|5a1acr|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/5a1cr/1c/31",
        ["play-standard-31|1a5a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a5/1c/31",
        ["play-standard-31|1a1a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a1/1c/31",
        ["play-standard-31|2a2a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/2a2/1c/31",
        ["play-standard-31|3a11a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/3a11/1c/31",
        ["play-standard-31|11a3a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/11a3/1c/31",
        ["play-standard-31|1a25acr|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a25cr/1c/31",
        ["play-standard-31|money-j|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/1c/31",
        ["play-standard-31|money-nj|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0nj/1c/31",
        ["play-standard-13|7a7a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31",
        ["play-standard-13|1a5acr|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a5cr/1c/31",
        ["play-standard-13|5a1acr|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/5a1cr/1c/31",
        ["play-standard-13|1a5a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a5/1c/31",
        ["play-standard-13|1a1a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a1/1c/31",
        ["play-standard-13|2a2a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/2a2/1c/31",
        ["play-standard-13|3a11a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/3a11/1c/31",
        ["play-standard-13|11a3a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/11a3/1c/31",
        ["play-standard-13|1a25acr|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a25cr/1c/31",
        ["play-standard-13|money-j|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/1c/31",
        ["play-standard-13|money-nj|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0nj/1c/31",
        ["play-race-66|7a7a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/1c/66",
        ["play-race-66|1a5acr|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/1a5cr/1c/66",
        ["play-race-66|5a1acr|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/5a1cr/1c/66",
        ["play-race-66|1a5a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/1a5/1c/66",
        ["play-race-66|1a1a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/1a1/1c/66",
        ["play-race-66|2a2a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/2a2/1c/66",
        ["play-race-66|3a11a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/3a11/1c/66",
        ["play-race-66|11a3a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/11a3/1c/66",
        ["play-race-66|1a25acr|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/1a25cr/1c/66",
        ["play-race-66|money-j|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/1c/66",
        ["play-race-66|money-nj|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0nj/1c/66",
        ["play-bars-52|7a7a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/1c/52",
        ["play-bars-52|1a5acr|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/1a5cr/1c/52",
        ["play-bars-52|5a1acr|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/5a1cr/1c/52",
        ["play-bars-52|1a5a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/1a5/1c/52",
        ["play-bars-52|1a1a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/1a1/1c/52",
        ["play-bars-52|2a2a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/2a2/1c/52",
        ["play-bars-52|3a11a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/3a11/1c/52",
        ["play-bars-52|11a3a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/11a3/1c/52",
        ["play-bars-52|1a25acr|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/1a25cr/1c/52",
        ["play-bars-52|money-j|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/1c/52",
        ["play-bars-52|money-nj|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0nj/1c/52",
        ["cube-race|7a7a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/1c",
        ["cube-race|1a5a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/1a5/1c",
        ["cube-race|1a1a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/1a1/1c",
        ["cube-race|2a2a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/2a2/1c",
        ["cube-race|3a11a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/3a11/1c",
        ["cube-race|11a3a|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/11a3/1c",
        ["cube-race|money-j|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/1c",
        ["cube-race|money-nj|1c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0nj/1c",
        ["cube-standard|7a7a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c",
        ["cube-standard|1a5a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a5/1c",
        ["cube-standard|1a1a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/1a1/1c",
        ["cube-standard|2a2a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/2a2/1c",
        ["cube-standard|3a11a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/3a11/1c",
        ["cube-standard|11a3a|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/11a3/1c",
        ["cube-standard|money-j|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/1c",
        ["cube-standard|money-nj|1c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0nj/1c",
        ["cube-bars|7a7a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/1c",
        ["cube-bars|1a5a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/1a5/1c",
        ["cube-bars|1a1a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/1a1/1c",
        ["cube-bars|2a2a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/2a2/1c",
        ["cube-bars|3a11a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/3a11/1c",
        ["cube-bars|11a3a|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/11a3/1c",
        ["cube-bars|money-j|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/1c",
        ["cube-bars|money-nj|1c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0nj/1c",
        ["play-standard-31|7a7a|2o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2o/31",
        ["play-standard-31|7a7a|2p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2p/31",
        ["play-standard-31|7a7a|4o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/4o/31",
        ["play-standard-31|7a7a|64p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/64p/31",
        ["play-standard-31|7a7a|2c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2c/31",
        ["play-standard-13|7a7a|2o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2o/31",
        ["play-standard-13|7a7a|2p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2p/31",
        ["play-standard-13|7a7a|4o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/4o/31",
        ["play-standard-13|7a7a|64p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/64p/31",
        ["play-standard-13|7a7a|2c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2c/31",
        ["play-race-66|7a7a|2o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/2o/66",
        ["play-race-66|7a7a|2p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/2p/66",
        ["play-race-66|7a7a|4o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/4o/66",
        ["play-race-66|7a7a|64p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/64p/66",
        ["play-race-66|7a7a|2c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/2c/66",
        ["play-bars-52|7a7a|2o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/2o/52",
        ["play-bars-52|7a7a|2p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/2p/52",
        ["play-bars-52|7a7a|4o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/4o/52",
        ["play-bars-52|7a7a|64p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/64p/52",
        ["play-bars-52|7a7a|2c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/2c/52",
        ["cube-race|7a7a|2o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/2o",
        ["cube-race|7a7a|2p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/2p",
        ["cube-race|7a7a|4o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/4o",
        ["cube-race|7a7a|64p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/64p",
        ["cube-race|7a7a|2c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/7a7/2c",
        ["cube-standard|7a7a|2o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2o",
        ["cube-standard|7a7a|2p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2p",
        ["cube-standard|7a7a|4o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/4o",
        ["cube-standard|7a7a|64p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/64p",
        ["cube-standard|7a7a|2c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/2c",
        ["cube-bars|7a7a|2o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/2o",
        ["cube-bars|7a7a|2p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/2p",
        ["cube-bars|7a7a|4o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/4o",
        ["cube-bars|7a7a|64p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/64p",
        ["cube-bars|7a7a|2c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/7a7/2c",
        ["play-standard-31|money-j|2o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2o/31",
        ["play-standard-31|money-j|2p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2p/31",
        ["play-standard-31|money-j|4o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/4o/31",
        ["play-standard-31|money-j|64p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/64p/31",
        ["play-standard-31|money-j|2c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2c/31",
        ["play-standard-13|money-j|2o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2o/31",
        ["play-standard-13|money-j|2p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2p/31",
        ["play-standard-13|money-j|4o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/4o/31",
        ["play-standard-13|money-j|64p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/64p/31",
        ["play-standard-13|money-j|2c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2c/31",
        ["play-race-66|money-j|2o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/2o/66",
        ["play-race-66|money-j|2p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/2p/66",
        ["play-race-66|money-j|4o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/4o/66",
        ["play-race-66|money-j|64p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/64p/66",
        ["play-race-66|money-j|2c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/2c/66",
        ["play-bars-52|money-j|2o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/2o/52",
        ["play-bars-52|money-j|2p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/2p/52",
        ["play-bars-52|money-j|4o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/4o/52",
        ["play-bars-52|money-j|64p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/64p/52",
        ["play-bars-52|money-j|2c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/2c/52",
        ["cube-race|money-j|2o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/2o",
        ["cube-race|money-j|2p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/2p",
        ["cube-race|money-j|4o"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/4o",
        ["cube-race|money-j|64p"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/64p",
        ["cube-race|money-j|2c"] = "0,2,2,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,-4,-4,-3,-2,-1,-1,0/0a0j/2c",
        ["cube-standard|money-j|2o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2o",
        ["cube-standard|money-j|2p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2p",
        ["cube-standard|money-j|4o"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/4o",
        ["cube-standard|money-j|64p"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/64p",
        ["cube-standard|money-j|2c"] = "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/0a0j/2c",
        ["cube-bars|money-j|2o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/2o",
        ["cube-bars|money-j|2p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/2p",
        ["cube-bars|money-j|4o"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/4o",
        ["cube-bars|money-j|64p"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/64p",
        ["cube-bars|money-j|2c"] = "-1,2,0,0,0,0,4,0,3,0,0,0,-5,4,0,0,0,-3,0,-5,0,0,0,0,1,1/0a0j/2c",
    };
}
