using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class BgDecisionDataSerializationTests
{
    // No explicit enum-converter registration: CubeOwner bundles its
    // own [JsonConverter(typeof(StrictJsonStringEnumConverter<CubeOwner>))]
    // attribute. The test
    // relies on the attribute alone so that removing it from the type would
    // fail this suite loudly (rather than silently passing because an
    // option-level registration covered for it).
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
    };

    // -----------------------------------------------------------------------
    //  Absence (halheinrich/backgammon#222). The "defaults to" pins below
    //  were rewritten into these two shapes: a required member's absence is
    //  refused, a nullable member's absence reads as null. WireAbsenceTests
    //  walks every member of the graph on both paths; these keep the pins
    //  beside the members they were written for.
    // -----------------------------------------------------------------------

    /// <summary>
    /// <paramref name="full"/>'s document without each of
    /// <paramref name="members"/> is refused; the full document loads (the
    /// control, so the refusal is the absence's).
    /// </summary>
    private static void AssertAbsentIsRefused<T>(T full, params string[] members)
    {
        var json = JsonSerializer.Serialize(full, Options);
        Assert.NotNull(JsonSerializer.Deserialize<T>(json, Options));
        foreach (var member in members)
        {
            var document = JsonNode.Parse(json)!.AsObject();
            Assert.True(document.Remove(member), $"{member} is not in the document");
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<T>(document.ToJsonString(), Options));
        }
    }

    /// <summary><paramref name="full"/> read back from a document without <paramref name="members"/>.</summary>
    private static T ReadWithout<T>(T full, params string[] members)
    {
        var document = JsonNode.Parse(JsonSerializer.Serialize(full, Options))!.AsObject();
        foreach (var member in members)
            Assert.True(document.Remove(member), $"{member} is not in the document");
        return JsonSerializer.Deserialize<T>(document.ToJsonString(), Options)!;
    }

    // -----------------------------------------------------------------------
    //  Leaf types
    // -----------------------------------------------------------------------

    [Fact]
    public void PlayCandidate_EquityLoss_Zero_RoundTrips()
    {
        // Rewritten from PlayCandidate_EquityLoss_DefaultsToZero: the loss is
        // required now, so a best play states its 0.0.
        // Best plays carry EquityLoss = 0.0; the test for "is this a best play"
        // is EquityLoss == 0.0 (or membership-by-equity equivalence). Identifying
        // a canonical best uses DecisionData.BestPlayIndex.
        var original = TestRecords.Candidate(
            moveNotation: "8/5(2) 6/3(2)",
            equity: -0.142,
            equityLoss: 0.0);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.MoveNotation, restored.MoveNotation);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(0.0, restored.EquityLoss);
    }

    [Fact]
    public void PlayCandidate_RoundTrip_PopulatedEquityLoss()
    {
        var original = TestRecords.Candidate(
            moveNotation: "13/8 13/11",
            equity: -0.187,
            equityLoss: 0.045);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.EquityLoss, restored.EquityLoss);
    }

    [Fact]
    public void PlayCandidate_RoundTrip_Probabilities_AllPopulated()
    {
        var original = TestRecords.Candidate(
            moveNotation: "8/5(2) 6/3(2)",
            equity: -0.142,
            winPct: 0.481,
            winGammonPct: 0.112,
            winBgPct: 0.004,
            losePct: 0.519,
            loseGammonPct: 0.143,
            loseBgPct: 0.006);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.WinPct, restored.WinPct);
        Assert.Equal(original.WinGammonPct, restored.WinGammonPct);
        Assert.Equal(original.WinBgPct, restored.WinBgPct);
        Assert.Equal(original.LosePct, restored.LosePct);
        Assert.Equal(original.LoseGammonPct, restored.LoseGammonPct);
        Assert.Equal(original.LoseBgPct, restored.LoseBgPct);
    }

    [Fact]
    public void PlayCandidate_RoundTrip_Probabilities_AllNull()
    {
        var original = TestRecords.Candidate(
            moveNotation: "8/5(2) 6/3(2)",
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Null(restored.WinPct);
        Assert.Null(restored.WinGammonPct);
        Assert.Null(restored.WinBgPct);
        Assert.Null(restored.LosePct);
        Assert.Null(restored.LoseGammonPct);
        Assert.Null(restored.LoseBgPct);
    }

    [Fact]
    public void PlayCandidate_RoundTrip_Probabilities_PartiallyPopulated()
    {
        var original = TestRecords.Candidate(
            moveNotation: "13/8 13/11",
            equity: -0.187,
            winPct: 0.476,
            losePct: 0.524
            // gammon/bg fields left null — partial evaluation
        );
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.WinPct, restored.WinPct);
        Assert.Equal(original.LosePct, restored.LosePct);
        Assert.Null(restored.WinGammonPct);
        Assert.Null(restored.WinBgPct);
        Assert.Null(restored.LoseGammonPct);
        Assert.Null(restored.LoseBgPct);
    }

    [Fact]
    public void PlayCandidate_RoundTripWithDepth()
    {
        var original = TestRecords.Candidate(
            moveNotation: "8/5(2) 6/3(2)",
            depth: "Rollout: 1296 trials. 3-ply",
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.Depth, restored.Depth);
        Assert.Equal(original.MoveNotation, restored.MoveNotation);
        Assert.Equal(original.Equity, restored.Equity);
    }

    [Fact]
    public void PlayCandidate_Depth_AbsentIsRefused()
    {
        // Rewritten from PlayCandidate_Depth_DefaultsToEmpty.
        AssertAbsentIsRefused(TestRecords.Candidate(moveNotation: "8/5 6/1"), "Depth");
    }

    [Fact]
    public void PlayCandidate_DepthAbbreviation_RoundTrip()
    {
        var original = TestRecords.Candidate(
            moveNotation: "8/5(2) 6/3(2)",
            depthAbbreviation: "3p1296",
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.DepthAbbreviation, restored.DepthAbbreviation);
        Assert.Equal(original.MoveNotation, restored.MoveNotation);
        Assert.Equal(original.Equity, restored.Equity);
    }

    [Fact]
    public void PlayCandidate_DepthAbbreviation_AbsentIsRefused()
    {
        // Rewritten from PlayCandidate_DepthAbbreviation_DefaultsToEmpty.
        AssertAbsentIsRefused(TestRecords.Candidate(moveNotation: "8/5 6/1"), "DepthAbbreviation");
    }

    [Fact]
    public void PlayCandidate_DepthRank_RoundTrip()
    {
        var original = TestRecords.Candidate(
            moveNotation: "8/5(2) 6/3(2)",
            depthRank: 7,
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(7, restored.DepthRank);
        Assert.Equal(original.MoveNotation, restored.MoveNotation);
    }

    [Fact]
    public void PlayCandidate_DepthRank_AbsentIsRefused()
    {
        // Rewritten from PlayCandidate_DepthRank_DefaultsToZero.
        AssertAbsentIsRefused(TestRecords.Candidate(moveNotation: "8/5 6/1"), "DepthRank");
    }

    [Fact]
    public void PlayCandidate_AnalysisModeAndLevel_RoundTrip()
    {
        var original = TestRecords.Candidate(
            moveNotation: "8/5(2) 6/3(2)",
            analysisMode: AnalysisMode.Rollout,
            analysisLevel: AnalysisLevel.Ply3,
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        // String form via the enums' bundled converters — no options-level registration.
        Assert.Contains("\"AnalysisMode\":\"Rollout\"", json);
        Assert.Contains("\"AnalysisLevel\":\"Ply3\"", json);
        Assert.Equal(AnalysisMode.Rollout, restored.AnalysisMode);
        Assert.Equal(AnalysisLevel.Ply3, restored.AnalysisLevel);
    }

    [Fact]
    public void PlayCandidate_AnalysisModeAndLevel_AbsentIsRefused()
    {
        // Rewritten from PlayCandidate_AnalysisModeAndLevel_DefaultToUnknown.
        // "Not recorded" is spelled Unknown by the producer; an absent member
        // is not read as it.
        AssertAbsentIsRefused(TestRecords.Candidate(moveNotation: "8/5 6/1"), "AnalysisMode", "AnalysisLevel");
    }

    [Fact]
    public void PlayCandidate_LegacyDepthClassJson_IsRefused_TheRetiredPropertyStillIgnored()
    {
        // Rewritten from PlayCandidate_LegacyDepthClassJson_DeserializesToUnknownPair.
        // JSON written before the two-axis pair existed lacks it; the pair is
        // required now (halheinrich/backgammon#222), so such a document is
        // refused rather than read as "depth not recorded". The retired flat
        // "DepthClass" property is still an unrecognized property, ignored
        // beside a full candidate.
        var legacy = "{\"MoveNotation\":\"8/5 6/1\",\"Depth\":\"3-ply\",\"DepthClass\":\"Ply3\",\"Equity\":-0.12}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlayCandidate>(legacy, Options));

        var full = JsonNode.Parse(JsonSerializer.Serialize(
            TestRecords.Candidate(moveNotation: "8/5 6/1", depth: "3-ply"), Options))!.AsObject();
        full["DepthClass"] = "Ply3";
        var restored = JsonSerializer.Deserialize<PlayCandidate>(full.ToJsonString(), Options)!;

        Assert.Equal(AnalysisMode.Unknown, restored.AnalysisMode);
        Assert.Equal(AnalysisLevel.Unknown, restored.AnalysisLevel);
        Assert.Equal("3-ply", restored.Depth);
    }

    [Fact]
    public void PlayCandidate_Play_AbsentIsRefused()
    {
        // Rewritten from PlayCandidate_Play_DefaultsToEmpty: an absent play
        // would read as the empty play, a pass.
        AssertAbsentIsRefused(TestRecords.Candidate(moveNotation: "8/5 6/1"), "Play");
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_Empty()
    {
        var original = TestRecords.Candidate(
            moveNotation: "8/5 6/1",
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(0, restored.Play.Count);
        Assert.Contains("\"Play\":[]", json);
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_Populated()
    {
        Play play = [new(13, 7), new(8, 5)];

        var original = TestRecords.Candidate(
            moveNotation: "13/7 8/5",
            equity: -0.118,
            play: play);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(2, restored.Play.Count);
        Assert.Equal(new Move(13, 7), restored.Play[0]);
        Assert.Equal(new Move(8, 5), restored.Play[1]);
        Assert.True(restored.Play.IsSameEncoding(original.Play));
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_PreservesHitEncoding()
    {
        Play play = [new(13, -7)];   // hit on the 7-point

        var original = TestRecords.Candidate(
            moveNotation: "13/7*",
            equity: 0.05,
            play: play);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(1, restored.Play.Count);
        Assert.Equal(13, restored.Play[0].FrPt);
        Assert.Equal(-7, restored.Play[0].ToPt);
    }

    [Fact]
    public void PlayCandidate_Play_NestedInBgDecisionData_RoundTrip()
    {
        Play play = [new(24, 18), new(13, 9)];

        var original = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(
                dice: [6, 4],
                plays: [
                    TestRecords.Candidate(
                        moveNotation: "24/18 13/9",
                        equity: 0.211,
                        play: play)
                ],
                isCube: false));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        var restoredPlay = restored.Decision.Plays[0].Play;
        Assert.Equal(2, restoredPlay.Count);
        Assert.Equal(new Move(24, 18), restoredPlay[0]);
        Assert.Equal(new Move(13, 9), restoredPlay[1]);
    }

    [Fact]
    public void DecisionData_CubeDepth_RoundTrip()
    {
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            cubeDepth: "Rollout: 1296 trials. 3-ply",
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal("Rollout: 1296 trials. 3-ply", restored.CubeDepth);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeDepth_AbsentIsRefused()
    {
        // Rewritten from DecisionData_CubeDepth_DefaultsToEmpty.
        AssertAbsentIsRefused(TestRecords.Decision(), "CubeDepth");
    }

    [Fact]
    public void DecisionData_CubeDepthAbbreviation_RoundTrip()
    {
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            cubeDepthAbbreviation: "3p1296",
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal("3p1296", restored.CubeDepthAbbreviation);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeDepthAbbreviation_AbsentIsRefused()
    {
        // Rewritten from DecisionData_CubeDepthAbbreviation_DefaultsToEmpty.
        AssertAbsentIsRefused(TestRecords.Decision(), "CubeDepthAbbreviation");
    }

    [Fact]
    public void DecisionData_CubeDepthRank_RoundTrip()
    {
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            cubeDepthRank: 7,
            noDoubleEquity: 0.312);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(7, restored.CubeDepthRank);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeDepthRank_AbsentIsRefused()
    {
        // Rewritten from DecisionData_CubeDepthRank_DefaultsToZero.
        AssertAbsentIsRefused(TestRecords.Decision(), "CubeDepthRank");
    }

    [Fact]
    public void DecisionData_CubeAnalysisModeAndLevel_RoundTrip()
    {
        // BookRollout + XG Roller is the motivating cube case: the shipped
        // opening-book database carries cube rollout levels of XG Roller,
        // which the retired flat depth class could not represent.
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            cubeAnalysisMode: AnalysisMode.BookRollout,
            cubeAnalysisLevel: AnalysisLevel.XgRoller,
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Contains("\"CubeAnalysisMode\":\"BookRollout\"", json);
        Assert.Contains("\"CubeAnalysisLevel\":\"XgRoller\"", json);
        Assert.Equal(AnalysisMode.BookRollout, restored.CubeAnalysisMode);
        Assert.Equal(AnalysisLevel.XgRoller, restored.CubeAnalysisLevel);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeAnalysisModeAndLevel_AbsentIsRefused()
    {
        // Rewritten from DecisionData_CubeAnalysisModeAndLevel_DefaultToUnknown.
        AssertAbsentIsRefused(TestRecords.Decision(), "CubeAnalysisMode", "CubeAnalysisLevel");
    }

    [Fact]
    public void DecisionData_LegacyCubeDepthClassJson_IsRefused_TheRetiredPropertyStillIgnored()
    {
        // Rewritten from DecisionData_LegacyCubeDepthClassJson_DeserializesToUnknownPair:
        // the legacy document lacks the required pair and is refused; the
        // retired flat "CubeDepthClass" property is still ignored beside a
        // full decision.
        var legacy = "{\"Dice\":[0,0],\"IsCube\":true,\"CubeDepth\":\"3-ply\",\"CubeDepthClass\":\"Ply3\",\"NoDoubleEquity\":0.312}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionData>(legacy, Options));

        var full = JsonNode.Parse(JsonSerializer.Serialize(
            TestRecords.Decision(isCube: true, cubeDepth: "3-ply", noDoubleEquity: 0.312), Options))!.AsObject();
        full["CubeDepthClass"] = "Ply3";
        var restored = JsonSerializer.Deserialize<DecisionData>(full.ToJsonString(), Options)!;

        Assert.Equal(AnalysisMode.Unknown, restored.CubeAnalysisMode);
        Assert.Equal(AnalysisLevel.Unknown, restored.CubeAnalysisLevel);
        Assert.Equal("3-ply", restored.CubeDepth);
    }

    [Fact]
    public void CubeOwner_Serializes_AsString()
    {
        var data = TestRecords.Position(cubeOwner: CubeOwner.Opponent);
        var json = JsonSerializer.Serialize(data, Options);

        Assert.Contains("\"CubeOwner\":\"Opponent\"", json);
    }

    // -----------------------------------------------------------------------
    //  PositionData
    // -----------------------------------------------------------------------

    [Fact]
    public void PositionData_RoundTrip()
    {
        var mop = new int[26];
        mop[1] = 2; mop[6] = -5; mop[24] = -2; mop[25] = 1;

        var original = TestRecords.Position(
            mop: new BoardPosition(mop),
            onRollNeeds: 3,
            opponentNeeds: 5,
            cubeSize: 2,
            cubeOwner: CubeOwner.OnRoll,
            isCrawford: false);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PositionData>(json, Options)!;

        Assert.Equal(original.Mop, restored.Mop);
        Assert.Equal(original.OnRollNeeds, restored.OnRollNeeds);
        Assert.Equal(original.OpponentNeeds, restored.OpponentNeeds);
        Assert.Equal(original.CubeSize, restored.CubeSize);
        Assert.Equal(original.CubeOwner, restored.CubeOwner);
        Assert.Equal(original.IsCrawford, restored.IsCrawford);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void PositionData_IsJacoby_RoundTrips(bool? isJacoby)
    {
        // Three states on the wire, not two: null is "the producer did not
        // supply the fact", which ProblemKey's no-key rung reads on a money
        // record (halheinrich/backgammon#120).
        var original = TestRecords.Position(isJacoby: isJacoby);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PositionData>(json, Options)!;

        Assert.Equal(isJacoby, restored.IsJacoby);
    }

    [Fact]
    public void PositionData_IsJacoby_AbsentFromJson_ReadsAsNotSupplied()
    {
        // A record written before the fact existed carries no such property.
        // It must read back as null — "unknown" — never as a silent "off",
        // which on a money record would key it wrongly. Rewritten onto a
        // full position: every other member is required now, so the legacy
        // literal (which also lacked the pip counts) would be refused for
        // those.
        var restored = ReadWithout(
            TestRecords.Position(cubeOwner: CubeOwner.Centered, isJacoby: true), "IsJacoby");

        Assert.Null(restored.IsJacoby);
    }

    [Fact]
    public void PositionData_IsJacoby_SerializesUnderItsOwnName()
    {
        var json = JsonSerializer.Serialize(TestRecords.Position(isJacoby: true), Options);

        Assert.Contains("\"IsJacoby\":true", json);
    }

    // -----------------------------------------------------------------------
    //  DecisionData
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionData_RoundTrip_PlayDecision()
    {
        var original = TestRecords.Decision(
            dice: [3, 5],
            plays: [
                TestRecords.Candidate(moveNotation: "8/5 6/1", depth: "3-ply", equity: -0.120),
                TestRecords.Candidate(moveNotation: "8/3 6/1", depth: "3-ply", equity: -0.165, equityLoss: 0.045)
            ],
            isCube: false);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(original.Dice, restored.Dice);
        Assert.Equal(2, restored.Plays.Count);
        Assert.Equal("8/5 6/1", restored.Plays[0].MoveNotation);
        Assert.Equal("3-ply", restored.Plays[0].Depth);
        Assert.Equal("3-ply", restored.Plays[1].Depth);
        Assert.Equal(0.045, restored.Plays[1].EquityLoss);
        Assert.False(restored.IsCube);
    }

    [Fact]
    public void DecisionData_RoundTrip_CubeDecision()
    {
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            winPctAfterNoDouble: 0.621,
            gammonPctAfterNoDouble: 0.183,
            bgPctAfterNoDouble: 0.012,
            losePctAfterNoDouble: 0.379,
            loseGammonPctAfterNoDouble: 0.091,
            loseBgPctAfterNoDouble: 0.003,
            winPctAfterDoubleTake: 0.618,
            gammonPctAfterDoubleTake: 0.181,
            bgPctAfterDoubleTake: 0.011,
            losePctAfterDoubleTake: 0.382,
            loseGammonPctAfterDoubleTake: 0.093,
            loseBgPctAfterDoubleTake: 0.004,
            probOfOpponentErrorJustifyingDouble: 0.078);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.True(restored.IsCube);
        Assert.Equal(original.NoDoubleEquity, restored.NoDoubleEquity);
        Assert.Equal(original.DoubleTakeEquity, restored.DoubleTakeEquity);
        Assert.Equal(original.ProbOfOpponentErrorJustifyingDouble,
                     restored.ProbOfOpponentErrorJustifyingDouble);
        Assert.Equal(original.WinPctAfterNoDouble, restored.WinPctAfterNoDouble);
        Assert.Equal(original.LoseBgPctAfterDoubleTake, restored.LoseBgPctAfterDoubleTake);
    }

    // -----------------------------------------------------------------------
    //  DescriptiveData
    // -----------------------------------------------------------------------

    [Fact]
    public void DescriptiveData_RoundTrip_AllNullableFieldsNull()
    {
        var original = TestRecords.Descriptive(
            matchLength: 11,
            onRollName: "Mochy",
            opponentName: "Falafel",
            title: null,
            date: null,
            @event: null,
            sourceFile: null);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;

        Assert.Equal(original.MatchLength, restored.MatchLength);
        Assert.Equal(original.OnRollName, restored.OnRollName);
        Assert.Equal(original.OpponentName, restored.OpponentName);
        Assert.Null(restored.Title);
        Assert.Null(restored.Date);
        Assert.Null(restored.Event);
        Assert.Null(restored.SourceFile);
    }

    [Fact]
    public void DescriptiveData_RoundTrip_DateOnly()
    {
        var original = TestRecords.Descriptive(
            matchLength: 7,
            onRollName: "Player A",
            opponentName: "Player B",
            date: new DateOnly(2024, 11, 15),
            @event: "Monte Carlo 2024");

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;

        Assert.Equal(original.Date, restored.Date);
        Assert.Equal(original.Event, restored.Event);
    }

    [Fact]
    public void DescriptiveData_RoundTrip_WithSourceFile()
    {
        var original = TestRecords.Descriptive(
            matchLength: 7,
            onRollName: "Mochy",
            opponentName: "Falafel",
            sourceFile: "mochy-falafel.xg");

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;

        Assert.Equal("mochy-falafel.xg", restored.SourceFile);
    }

    [Fact]
    public void DescriptiveData_SourceFile_AbsentReadsAsNull()
    {
        // Rewritten from DescriptiveData_SourceFile_DefaultsToNull: the member
        // is nullable (none recorded), so its absence reads as null.
        var d = ReadWithout(
            TestRecords.Descriptive(onRollName: "A", opponentName: "B", sourceFile: "a-b.xg"), "SourceFile");
        Assert.Null(d.SourceFile);
    }

    // -----------------------------------------------------------------------
    //  BgDecisionData — full composite
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_RoundTrip()
    {
        var mop = new int[26];
        mop[6] = -5; mop[8] = -3; mop[13] = 5; mop[24] = 2;

        var original = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            position: TestRecords.Position(
                mop: new BoardPosition(mop),
                onRollNeeds: 2,
                opponentNeeds: 4,
                cubeSize: 4,
                cubeOwner: CubeOwner.Centered,
                isCrawford: false),
            decision: TestRecords.Decision(
                dice: [6, 4],
                plays: [
                    TestRecords.Candidate(moveNotation: "24/18 24/20", depth: "3-ply", equity: 0.211),
                    TestRecords.Candidate(moveNotation: "24/18 13/9",  depth: "3-ply", equity: 0.198, equityLoss: 0.013)
                ],
                isCube: false),
            descriptive: TestRecords.Descriptive(
                matchLength: 5,
                onRollName: "Hal",
                opponentName: "Bot",
                title: "Opening Run",
                date: new DateOnly(2025, 3, 1),
                @event: "Test Match",
                sourceFile: "hal-bot.xg"));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        Assert.Equal(original.Position.Mop, restored.Position.Mop);
        Assert.Equal(original.Position.CubeOwner, restored.Position.CubeOwner);
        Assert.Equal(original.Decision.Dice, restored.Decision.Dice);
        Assert.Equal(2, restored.Decision.Plays.Count);
        Assert.Equal(0.013, restored.Decision.Plays[1].EquityLoss);
        Assert.Equal(original.Descriptive.OnRollName, restored.Descriptive.OnRollName);
        Assert.Equal(original.Descriptive.Date, restored.Descriptive.Date);
        Assert.Equal(original.Descriptive.Event, restored.Descriptive.Event);
        Assert.Equal(original.Descriptive.SourceFile, restored.Descriptive.SourceFile);
    }
    // -----------------------------------------------------------------------
    //  UserPlayError / UserDoubleError / UserTakeError
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionData_RoundTrip_PlayDecision_WithUserPlayError()
    {
        var original = TestRecords.Decision(
            dice: [3, 5],
            plays: [
                TestRecords.Candidate(moveNotation: "8/5 6/1", equity: -0.120),
                TestRecords.Candidate(moveNotation: "8/3 6/1", equity: -0.165, equityLoss: 0.045)
            ],
            isCube: false,
            userPlayIndex: 1,
            userPlayError: 0.045);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(0.045, restored.UserPlayError);
        Assert.Null(restored.UserDoubleError);
        Assert.Null(restored.UserTakeError);
    }

    [Fact]
    public void DecisionData_RoundTrip_CubeDecision_WithUserErrors()
    {
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            userDoubleError: 0.025,
            userTakeError: 0.011);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(0.025, restored.UserDoubleError);
        Assert.Equal(0.011, restored.UserTakeError);
        Assert.Null(restored.UserPlayError);
    }

    [Fact]
    public void DecisionData_UserErrors_NullOrAbsent_ReadAsNull()
    {
        // Rewritten from DecisionData_UserErrors_DefaultToNull: the three are
        // nullable (not recorded), so a null round-trips and an absent member
        // reads as null too.
        var original = TestRecords.Decision(
            dice: [6, 4],
            isCube: false);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Null(restored.UserPlayError);
        Assert.Null(restored.UserDoubleError);
        Assert.Null(restored.UserTakeError);

        var absent = ReadWithout(
            TestRecords.Decision(dice: [6, 4], userPlayError: 0.1, userDoubleError: 0.2, userTakeError: 0.3),
            "UserPlayError", "UserDoubleError", "UserTakeError");
        Assert.Null(absent.UserPlayError);
        Assert.Null(absent.UserDoubleError);
        Assert.Null(absent.UserTakeError);
    }

    // -----------------------------------------------------------------------
    //  UserDoublerAction / UserTakerAction — played cube actions
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionData_UserCubeActions_RoundTrip_DoubleWithResponse()
    {
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            userDoublerAction: CubeAction.Double,
            userTakerAction: CubeAction.Take);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        // String form via CubeAction's bundled converter — no options-level
        // registration.
        Assert.Contains("\"UserDoublerAction\":\"Double\"", json);
        Assert.Contains("\"UserTakerAction\":\"Take\"", json);
        Assert.Equal(CubeAction.Double, restored.UserDoublerAction);
        Assert.Equal(CubeAction.Take, restored.UserTakerAction);
    }

    [Fact]
    public void DecisionData_UserCubeActions_RoundTrip_UndoubledGame()
    {
        // No double was offered, so no taker decision exists — the taker half
        // stays null by shape while the doubler half records the NoDouble.
        var original = TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            userDoublerAction: CubeAction.NoDouble);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(CubeAction.NoDouble, restored.UserDoublerAction);
        Assert.Null(restored.UserTakerAction);
    }

    [Fact]
    public void DecisionData_UserCubeActions_NullRoundTrips()
    {
        // Rewritten from DecisionData_UserCubeActions_DefaultToNull: "not
        // recorded" is the null a producer states, and it round-trips (the
        // absent case is DecisionData_LegacyJsonWithoutUserCubeActions_DeserializesToNull).
        var json = JsonSerializer.Serialize(TestRecords.Decision(isCube: true), Options);
        var d = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Contains("\"UserDoublerAction\":null", json);
        Assert.Null(d.UserDoublerAction);
        Assert.Null(d.UserTakerAction);
    }

    [Fact]
    public void DecisionData_LegacyJsonWithoutUserCubeActions_DeserializesToNull()
    {
        // JSON written before the played-action fields existed carries
        // neither property; both read back null — played action not
        // recorded, never an error. Rewritten onto a full decision without
        // the two: every other member is required now.
        var restored = ReadWithout(
            TestRecords.Decision(
                isCube: true, noDoubleEquity: 0.312, doubleTakeEquity: 0.287, userDoubleError: 0.025,
                userDoublerAction: CubeAction.Double, userTakerAction: CubeAction.Take),
            "UserDoublerAction", "UserTakerAction");

        Assert.True(restored.IsCube);
        Assert.Equal(0.025, restored.UserDoubleError);
        Assert.Null(restored.UserDoublerAction);
        Assert.Null(restored.UserTakerAction);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — BgDecisionData
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CheckerPlay()
    {
        var mop = new int[26];
        mop[1] = 2; mop[6] = -5;

        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            position: TestRecords.Position(
                mop: new BoardPosition(mop),
                onRollNeeds: 3,
                opponentNeeds: 5,
                isCrawford: false),
            decision: TestRecords.Decision(
                isCube: false,
                userPlayError: 0.034),
            descriptive: TestRecords.Descriptive(onRollName: "Hal"));

        Assert.Equal("Hal", data.Player);
        Assert.False(data.IsCube);
        Assert.Equal(3, data.OnRollNeeds);
        Assert.Equal(5, data.OpponentNeeds);
        Assert.False(data.IsCrawford);
        Assert.Equal(0.034, data.FilterError);
        Assert.Equal(new BoardPosition(mop), data.Board);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CubePlay_UserDoubleError()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(
                isCube: true,
                userDoubleError: 0.025,
                userTakeError: 0.011));

        Assert.True(data.IsCube);
        Assert.Equal(0.025, data.FilterError);  // UserDoubleError takes precedence
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CubePlay_UserTakeError()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(
                isCube: true,
                userDoubleError: null,
                userTakeError: 0.011));

        Assert.Equal(0.011, data.FilterError);  // Falls through to UserTakeError
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_Board_MatchesMop()
    {
        var mop = new int[26];
        mop[6] = -5; mop[13] = 5;

        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            position: TestRecords.Position(mop: new BoardPosition(mop)));

        Assert.Equal(new BoardPosition(mop), data.Board);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MatchLength()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            descriptive: TestRecords.Descriptive(matchLength: 11));

        Assert.Equal(11, data.MatchLength);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_IsMoneyGame_MoneySession()
    {
        // BgDecisionData declares no IsMoneyGame of its own — the interface
        // default (MatchLength == 0) is what answers here.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            descriptive: TestRecords.Descriptive(matchLength: 0));

        Assert.True(data.IsMoneyGame);
    }

    [Theory]
    [InlineData(1)]  // shortest possible match
    [InlineData(11)]
    public void BgDecisionData_IDecisionFilterData_IsMoneyGame_FalseForAnyMatchLength(int matchLength)
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            descriptive: TestRecords.Descriptive(matchLength: matchLength));

        Assert.False(data.IsMoneyGame);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — IsJacoby forwarding (tri-state)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BgDecisionData_IDecisionFilterData_IsJacoby_MoneyRecord_CarriesTheValue(bool isJacoby)
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            position: TestRecords.Position(onRollNeeds: 0, opponentNeeds: 0, isJacoby: isJacoby),
            descriptive: TestRecords.Descriptive(matchLength: 0));

        Assert.True(data.IsMoneyGame);
        Assert.Equal(isJacoby, data.IsJacoby);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_IsJacoby_MatchRecord_IsNull()
    {
        // A match record carries null because the question does not arise —
        // the producer stamps the fact onto money records only.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            position: TestRecords.Position(onRollNeeds: 3, opponentNeeds: 5),
            descriptive: TestRecords.Descriptive(matchLength: 9));

        Assert.False(data.IsMoneyGame);
        Assert.Null(data.IsJacoby);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_IsJacoby_MoneyRecordUnstamped_IsNull()
    {
        // The unknown rung: a money record whose rule was never stamped. The
        // forwarder reports null rather than defaulting to either rule — which
        // is what makes it match neither money score token downstream.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            position: TestRecords.Position(onRollNeeds: 0, opponentNeeds: 0),
            descriptive: TestRecords.Descriptive(matchLength: 0));

        Assert.True(data.IsMoneyGame);
        Assert.Null(data.IsJacoby);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_IsJacoby_ForwardsPositionVerbatim()
    {
        // A stray non-null stamp on a match record is tolerated, not rejected
        // (PositionData.IsJacoby's contract). The forwarder passes it through
        // unchanged rather than nulling it out — the interface is a view of
        // the stored fact, not a second place the rule is decided.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            position: TestRecords.Position(onRollNeeds: 3, opponentNeeds: 5, isJacoby: true),
            descriptive: TestRecords.Descriptive(matchLength: 9));

        Assert.True(data.IsJacoby);
    }

    [Fact]
    public void BgDecisionData_IsMoneyGame_NotSerialized_MatchLengthRemainsTheWire()
    {
        // Interface default implementations never reach the JSON — the wire
        // shape carries Descriptive.MatchLength only.
        var data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            descriptive: TestRecords.Descriptive(matchLength: 0));
        var json = JsonSerializer.Serialize(data, Options);

        Assert.DoesNotContain("IsMoneyGame", json);
        Assert.Contains("\"MatchLength\":0", json);

        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;
        Assert.True(((IDecisionFilterData)restored).IsMoneyGame);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — AnalysisMode / AnalysisLevel derivation
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CubeDecision_UseCubeAnalysis()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(
                isCube: true,
                cubeAnalysisMode: AnalysisMode.Evaluation,
                cubeAnalysisLevel: AnalysisLevel.XgRollerPlus));

        Assert.Equal(AnalysisMode.Evaluation, data.AnalysisMode);
        Assert.Equal(AnalysisLevel.XgRollerPlus, data.AnalysisLevel);
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CheckerPlay_UseBestPlayCandidate()
    {
        // BestPlayIndex deliberately not 0, to pin that derivation indexes by
        // it rather than taking the first candidate.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(
                isCube: false,
                bestPlayIndex: 1,
                plays: [
                    TestRecords.Candidate(
                        moveNotation: "8/5 6/1",
                        analysisMode: AnalysisMode.Evaluation,
                        analysisLevel: AnalysisLevel.Ply3),
                    TestRecords.Candidate(
                        moveNotation: "8/3 6/1",
                        analysisMode: AnalysisMode.Rollout,
                        analysisLevel: AnalysisLevel.Ply1)
                ]));

        Assert.Equal(AnalysisMode.Rollout, data.AnalysisMode);
        Assert.Equal(AnalysisLevel.Ply1, data.AnalysisLevel);
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CheckerPlay_OutOfRangeBestPlayIndex_ReturnUnknown()
    {
        // Malformed or legacy data can carry a BestPlayIndex that doesn't
        // identify a candidate; the derivation degrades to Unknown rather
        // than throwing from a property getter that runs on every filter
        // pass and serialization.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(
                isCube: false,
                bestPlayIndex: 2,
                plays: [
                    TestRecords.Candidate(
                        moveNotation: "8/5 6/1",
                        analysisMode: AnalysisMode.Evaluation,
                        analysisLevel: AnalysisLevel.Ply3)
                ]));

        Assert.Equal(AnalysisMode.Unknown, data.AnalysisMode);
        Assert.Equal(AnalysisLevel.Unknown, data.AnalysisLevel);
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CheckerPlay_EmptyPlays_ReturnUnknown()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false));

        Assert.Equal(AnalysisMode.Unknown, data.AnalysisMode);
        Assert.Equal(AnalysisLevel.Unknown, data.AnalysisLevel);
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_NoCandidates_AreUnknown()
    {
        // Rewritten from BgDecisionData_AnalysisModeAndLevel_DefaultToUnknown:
        // what it pinned is the derivation, stated now — a checker play whose
        // BestPlayIndex identifies no candidate (empty Plays) reports Unknown.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false, plays: [], bestPlayIndex: 0));

        Assert.Equal(AnalysisMode.Unknown, data.AnalysisMode);
        Assert.Equal(AnalysisLevel.Unknown, data.AnalysisLevel);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — Dice forwarding
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_Dice_CheckerPlay_ForwardsFromDecision()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false, dice: [6, 3]));

        Assert.Equal(new DiceRoll(6, 3), data.Dice);
    }

    [Fact]
    public void BgDecisionData_Dice_RolledOrderCanonicalized()
    {
        // The XG parser stamps Decision.Dice in rolled order; the forwarding
        // property canonicalizes, so [1, 3] reads back high-first.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false, dice: [1, 3]));

        Assert.Equal(new DiceRoll(3, 1), data.Dice);
    }

    [Fact]
    public void BgDecisionData_Dice_CubeDecision_IsNull()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: true, dice: [0, 0]));

        Assert.Null(data.Dice);
    }

    [Fact]
    public void BgDecisionData_Dice_MalformedStoredDice_Throws()
    {
        // A checker play whose Dice was never stamped carries the {0, 0}
        // default — corrupt data fails loud in the DiceRoll constructor.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false));

        Assert.Throws<ArgumentOutOfRangeException>(() => data.Dice);
    }

    [Fact]
    public void BgDecisionData_Dice_NotSerialized_DecisionDiceRemainsTheWire()
    {
        var original = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false, dice: [6, 3]));

        var json = JsonSerializer.Serialize(original, Options);

        // The nested int-pair wire is unchanged; the derived canonical token
        // never appears.
        Assert.Contains("\"Dice\":[6,3]", json);
        Assert.DoesNotContain("\"Dice\":\"63\"", json);

        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;
        Assert.Equal(new DiceRoll(6, 3), ((IDecisionFilterData)restored).Dice);
    }

    // -----------------------------------------------------------------------
    //  PlayOutcomeData — after-boards
    // -----------------------------------------------------------------------

    [Fact]
    public void PlayOutcomeData_RoundTrip_Populated()
    {
        var best = new int[26];
        best[1] = 2; best[6] = -5; best[20] = -2;
        var player = new int[26];
        player[1] = 2; player[6] = -5; player[19] = -2;

        var original = TestRecords.Outcome(
            afterBestBoard: new BoardPosition(best),
            afterPlayerBoard: new BoardPosition(player));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayOutcomeData>(json, Options)!;

        Assert.Equal(original.AfterBestBoard, restored.AfterBestBoard);
        Assert.Equal(original.AfterPlayerBoard, restored.AfterPlayerBoard);
    }

    [Fact]
    public void PlayOutcomeData_AbsentBoards_AreNull()
    {
        // Rewritten from PlayOutcomeData_DefaultsToEmptyBoards: an absent
        // after-board is null, never an empty list (halheinrich/backgammon#15),
        // and it is written as null.
        var data = TestRecords.Outcome();
        Assert.Null(data.AfterBestBoard);
        Assert.Null(data.AfterPlayerBoard);
        Assert.Equal("{\"AfterBestBoard\":null,\"AfterPlayerBoard\":null}",
            JsonSerializer.Serialize(data, Options));
    }

    [Fact]
    public void BgDecisionData_RoundTrip_Outcome()
    {
        var best = new int[26];
        best[4] = 2; best[6] = -5; best[20] = -2;
        var player = new int[26];
        player[5] = 2; player[6] = -5; player[20] = -2;

        var original = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false, userPlayError: 0.018),
            outcome: TestRecords.Outcome(
                afterBestBoard: new BoardPosition(best),
                afterPlayerBoard: new BoardPosition(player)));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        Assert.Equal(original.Outcome.AfterBestBoard, restored.Outcome.AfterBestBoard);
        Assert.Equal(original.Outcome.AfterPlayerBoard, restored.Outcome.AfterPlayerBoard);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_AfterBoards_ForwardFromOutcome()
    {
        var best = new int[26];
        best[1] = 2; best[20] = -2;
        var player = new int[26];
        player[1] = 2; player[19] = -2;

        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: false),
            outcome: TestRecords.Outcome(
                afterBestBoard: new BoardPosition(best),
                afterPlayerBoard: new BoardPosition(player)));

        Assert.Equal(new BoardPosition(best), data.AfterBestBoard);
        Assert.Equal(new BoardPosition(player), data.AfterPlayerBoard);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_AfterBoards_Null_CubeDecision()
    {
        // Rewritten from ..._AfterBoards_EmptyByDefault_CubeDecision: a cube
        // decision's after-boards are absent, which is null.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            decision: TestRecords.Decision(isCube: true, userDoubleError: 0.025));

        Assert.True(data.IsCube);
        Assert.Null(data.AfterBestBoard);
        Assert.Null(data.AfterPlayerBoard);
    }

    // -----------------------------------------------------------------------
    //  Game, MoveNumber and IsStandardStart — DescriptiveData and BgDecisionData
    // -----------------------------------------------------------------------

    [Fact]
    public void DescriptiveData_Game_RoundTrip()
    {
        var original = TestRecords.Descriptive(
            onRollName: "Mochy",
            opponentName: "Falafel",
            game: 4);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;
        Assert.Equal(4, restored.Game);
    }

    [Fact]
    public void DescriptiveData_Game_AbsentIsRefused()
    {
        // Rewritten from DescriptiveData_Game_DefaultsToZero.
        AssertAbsentIsRefused(TestRecords.Descriptive(game: 3), "Game");
    }

    [Fact]
    public void DescriptiveData_MoveNumber_RoundTrip()
    {
        var original = TestRecords.Descriptive(
            onRollName: "Mochy",
            opponentName: "Falafel",
            moveNumber: 17);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;
        Assert.Equal(17, restored.MoveNumber);
    }

    [Fact]
    public void DescriptiveData_MoveNumber_AbsentIsRefused()
    {
        // Rewritten from DescriptiveData_MoveNumber_DefaultsToZero.
        AssertAbsentIsRefused(TestRecords.Descriptive(moveNumber: 12), "MoveNumber");
    }

    [Fact]
    public void DescriptiveData_IsStandardStart_RoundTrip()
    {
        var original = TestRecords.Descriptive(
            onRollName: "Mochy",
            opponentName: "Falafel",
            isStandardStart: true);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;
        Assert.True(restored.IsStandardStart);
    }

    [Fact]
    public void DescriptiveData_IsStandardStart_AbsentIsRefused()
    {
        // Rewritten from DescriptiveData_IsStandardStart_DefaultsToFalse: an
        // absent flag would read as a non-standard start.
        AssertAbsentIsRefused(TestRecords.Descriptive(isStandardStart: true), "IsStandardStart");
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MoveNumberAndIsStandardStart()
    {
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            descriptive: TestRecords.Descriptive(
                onRollName: "Hal",
                moveNumber: 12,
                isStandardStart: true));

        Assert.Equal(12, data.MoveNumber);
        Assert.True(data.IsStandardStart);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MoveNumberAndIsStandardStart_ForwardZeroAndFalse()
    {
        // Rewritten from ..._MoveNumberAndIsStandardStart_Defaults: the view
        // forwards the stated zero and false, which are values now, not
        // defaults.
        IDecisionFilterData data = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            descriptive: TestRecords.Descriptive(moveNumber: 0, isStandardStart: false));

        Assert.Equal(0, data.MoveNumber);
        Assert.False(data.IsStandardStart);
    }

    // -----------------------------------------------------------------------
    //  Id — persistent decision identifier
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_Id_RoundTrip_Xgp()
    {
        var original = TestRecords.Record(id: new XgpDecisionId("match.xgp"));
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        Assert.Equal(new XgpDecisionId("match.xgp"), restored.Id);
        Assert.Contains("\"Id\":\"match.xgp\"", json);
    }

    [Fact]
    public void BgDecisionData_Id_RoundTrip_Xg()
    {
        var original = TestRecords.Record(
            id: new XgDecisionId("match.xg", Game: 4, MoveNumber: 22, IsCube: true));
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        Assert.Equal(
            new XgDecisionId("match.xg", 4, 22, IsCube: true),
            restored.Id);
        Assert.Contains("\"Id\":\"match.xg:g4:m22:cube\"", json);
    }

    // -----------------------------------------------------------------------
    //  Xgid / Comment / Flagged / cubeless equities — new decision fields
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_NewDecisionFields_AbsentIsRefused()
    {
        // Rewritten from BgDecisionData_NewDecisionFields_DefaultToEmptyAndZero:
        // the fields added after documents existed read as empty and zero
        // when absent; they are required now (halheinrich/backgammon#222),
        // each at its own level of the record.
        AssertAbsentIsRefused(TestRecords.Record(id: new XgpDecisionId("test.xgp"), xgid: "XGID=x"), "Xgid");
        AssertAbsentIsRefused(TestRecords.Descriptive(comment: "note", flagged: true), "Comment", "Flagged");
        AssertAbsentIsRefused(
            TestRecords.Decision(cubelessNoDoubleEquity: 0.2, cubelessDoubleTakeEquity: 0.3),
            "CubelessNoDoubleEquity", "CubelessDoubleTakeEquity");
    }

    [Fact]
    public void BgDecisionData_NewDecisionFields_RoundTrip()
    {
        var original = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10",
            decision: TestRecords.Decision(
                dice: [0, 0],
                isCube: true,
                noDoubleEquity: 0.312,
                doubleTakeEquity: 0.287,
                cubelessNoDoubleEquity: 0.205,
                cubelessDoubleTakeEquity: 0.198),
            descriptive: TestRecords.Descriptive(
                onRollName: "Hal",
                opponentName: "Bot",
                comment: "Tricky double — too good?",
                flagged: true));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        // Xgid serializes at the top level, not inside Position.
        Assert.Contains("\"Xgid\":\"XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10\"", json);
        Assert.Equal(original.Xgid, restored.Xgid);

        Assert.Equal(original.Descriptive.Comment, restored.Descriptive.Comment);
        Assert.True(restored.Descriptive.Flagged);

        Assert.Equal(original.Decision.CubelessNoDoubleEquity, restored.Decision.CubelessNoDoubleEquity);
        Assert.Equal(original.Decision.CubelessDoubleTakeEquity, restored.Decision.CubelessDoubleTakeEquity);
    }

    // -----------------------------------------------------------------------
    //  Wire shape — the IDecisionFilterData view is excluded from JSON
    //  (halheinrich/backgammon#14)
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_TopLevelJson_IsExactlyTheSixWireMembers()
    {
        // The filter view forwards into the category members, so serializing
        // it would write top-level duplicates of nested data with no
        // read-back path (the members are get-only). This pins the top-level
        // wire shape as exactly the stored members, in declaration order — a
        // forwarding member added without [JsonIgnore] fails here.
        var mop = new int[26];
        mop[6] = -5; mop[13] = 5;

        var original = TestRecords.Record(
            id: new XgpDecisionId("test.xgp"),
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10",
            position: TestRecords.Position(
                mop: new BoardPosition(mop),
                onRollNeeds: 3,
                opponentNeeds: 5,
                isCrawford: true),
            decision: TestRecords.Decision(
                dice: [6, 4],
                plays: [TestRecords.Candidate(moveNotation: "24/18 13/9", equity: 0.198)],
                isCube: false,
                userPlayError: 0.013),
            descriptive: TestRecords.Descriptive(
                matchLength: 9,
                onRollName: "Hal",
                moveNumber: 12,
                isStandardStart: true),
            outcome: TestRecords.Outcome(afterBestBoard: new BoardPosition(mop), afterPlayerBoard: new BoardPosition(mop)));

        var json = JsonSerializer.Serialize(original, Options);
        using var doc = JsonDocument.Parse(json);
        var topLevelNames = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.Equal(
            ["Id", "Xgid", "Position", "Decision", "Descriptive", "Outcome"],
            topLevelNames);
    }
}