using System.Reflection;
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

    // The source-generated path, for the pins that hold on both.
    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
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

    /// <summary><paramref name="record"/> written and read back as the wire unit.</summary>
    private static BgDecisionData RoundTrip(BgDecisionData record, JsonSerializerOptions? options = null) =>
        JsonSerializer.Deserialize<BgDecisionData>(
            JsonSerializer.Serialize(record, options ?? Options), options ?? Options)!;

    // -----------------------------------------------------------------------
    //  Leaf types
    // -----------------------------------------------------------------------

    [Fact]
    public void CheckerPlay_ErrorOfABestPlay_IsZero_AfterARoundTrip()
    {
        // Rewritten from PlayCandidate_EquityLoss_Zero_RoundTrips: the loss is
        // no longer a candidate's member but a ranking's derivation — the
        // candidate's equity against the ranking's best — so it round-trips as
        // the equities do. An error of exactly 0 is the test for "is this a
        // best play"; several candidates may tie, and the ranking's best is
        // the first of them in the stored order.
        var original = TestRecords.CheckerPlayData(plays:
        [
            TestRecords.Candidate(play: [new(8, 5), new(6, 5)], equity: -0.142),
            TestRecords.Candidate(play: [new(13, 10), new(6, 5)], equity: -0.142),
        ]);
        var restored = JsonSerializer.Deserialize<CheckerPlayDecisionData>(
            JsonSerializer.Serialize(original, Options), Options)!;

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            var ranked = restored.RankedBy(ranking);
            Assert.Equal(0, ranked.Best.Index);
            Assert.Equal(0.0, ranked.ForCandidate(0).Error);
            Assert.Equal(0.0, ranked.ForCandidate(1).Error);
        }
    }

    [Fact]
    public void CheckerPlay_ErrorOfAWorsePlay_IsTheEquityGap_AfterARoundTrip()
    {
        // Rewritten from PlayCandidate_RoundTrip_PopulatedEquityLoss: the gap
        // is derived from the two equities, which are what the wire carries.
        var original = TestRecords.CheckerPlayData(plays:
        [
            TestRecords.Candidate(play: [new(8, 5), new(6, 5)], equity: -0.142),
            TestRecords.Candidate(play: [new(13, 10), new(6, 5)], equity: -0.187),
        ]);
        var restored = JsonSerializer.Deserialize<CheckerPlayDecisionData>(
            JsonSerializer.Serialize(original, Options), Options)!;

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            Assert.Equal(-0.142 - -0.187, restored.RankedBy(ranking).ForCandidate(1).Error);
            Assert.Equal(original.RankedBy(ranking).ForCandidate(1).Error, restored.RankedBy(ranking).ForCandidate(1).Error);
        }
    }

    [Fact]
    public void PlayCandidate_RoundTrip_Probabilities_AllPopulated()
    {
        var original = TestRecords.Candidate(
            play: [new(8, 5), new(8, 5), new(6, 3), new(6, 3)],
            equity: -0.142,
            winPct: 0.481,
            winGammonPct: 0.112,
            winBgPct: 0.004,
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
        // Rewritten: the builder's default candidate is evaluated now, so the
        // unevaluated one states its nulls.
        var original = TestRecords.Candidate(
            play: [new(8, 5), new(8, 5), new(6, 3), new(6, 3)],
            equity: -0.142,
            winPct: null, winGammonPct: null, winBgPct: null,
            loseGammonPct: null, loseBgPct: null);
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
        // Rewritten as the test above: the unevaluated fields state their nulls.
        var original = TestRecords.Candidate(
            play: [new(13, 8), new(13, 11)],
            equity: -0.187,
            winPct: 0.476,
            winGammonPct: null, winBgPct: null, loseGammonPct: null, loseBgPct: null);
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
        // Rewritten: the label is derived from the typed depth facts, which
        // are what round-trip; the label itself is not on the wire.
        var original = TestRecords.Candidate(
            play: [new(8, 5), new(8, 5), new(6, 3), new(6, 3)],
            analysisMode: AnalysisMode.Rollout, analysisLevel: AnalysisLevel.Ply3, rolloutTrials: 1296,
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Contains("\"RolloutTrials\":1296", json);
        Assert.DoesNotContain("\"Depth\"", json);
        Assert.Equal("Rollout: 1296 trials. 3-ply", restored.Depth);
        Assert.Equal("8/5(2) 6/3(2)", restored.Notation);
        Assert.Equal(original.Equity, restored.Equity);
    }

    [Fact]
    public void PlayCandidate_Depth_NoneRecorded_IsNull()
    {
        // Rewritten from PlayCandidate_Depth_AbsentReadsAsNoneRecorded (itself
        // from ..._AbsentIsRefused and ..._DefaultsToEmpty): the label is no
        // member to be absent. "No depth recorded" is the facts' — the mode
        // and the level unknown, no raw code — and the label it derives is
        // null, never empty text.
        var candidate = TestRecords.Candidate(
            play: [new(8, 5), new(6, 1)], analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown);

        Assert.Null(candidate.Depth);
        Assert.Null(JsonSerializer.Deserialize<PlayCandidate>(JsonSerializer.Serialize(candidate, Options), Options)!.Depth);
    }

    [Fact]
    public void PlayCandidate_DepthAbbreviation_RoundTrip()
    {
        // Rewritten: the abbreviation is derived from the typed facts — here a
        // book hit whose rollout parameters were recovered.
        var original = TestRecords.Candidate(
            play: [new(8, 5), new(8, 5), new(6, 3), new(6, 3)],
            analysisMode: AnalysisMode.BookRollout, analysisLevel: AnalysisLevel.Ply4,
            rolloutTrials: 12960, bookEdition: BookEdition.V2,
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.DoesNotContain("DepthAbbreviation", json);
        Assert.Equal("B4_12960", restored.DepthAbbreviation);
        Assert.Equal("8/5(2) 6/3(2)", restored.Notation);
        Assert.Equal(original.Equity, restored.Equity);
    }

    [Fact]
    public void PlayCandidate_DepthAbbreviation_NoneRecorded_IsNull()
    {
        // Rewritten from PlayCandidate_DepthAbbreviation_AbsentReadsAsNoneRecorded,
        // as the label's test above.
        Assert.Null(TestRecords.Candidate(
            play: [new(8, 5), new(6, 1)], analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown)
            .DepthAbbreviation);
    }

    [Fact]
    public void PlayCandidate_DepthRank_IsDerivedFromTheModeAndLevel_AfterARoundTrip()
    {
        // Rewritten from PlayCandidate_DepthRank_RoundTrip: the rank is not
        // stated but derived from the mode and level, which the wire carries —
        // a 3-ply rollout ranks 100 + 30.
        var original = TestRecords.Candidate(
            play: [new(8, 5), new(8, 5), new(6, 3), new(6, 3)],
            analysisMode: AnalysisMode.Rollout,
            analysisLevel: AnalysisLevel.Ply3,
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.DoesNotContain("DepthRank", json);
        Assert.Equal(130, restored.DepthRank);
        Assert.Equal("8/5(2) 6/3(2)", restored.Notation);
    }

    [Fact]
    public void PlayCandidate_RetiredDerivedMembers_AreIgnored_TheDerivationsStand()
    {
        // Rewritten from PlayCandidate_DepthRank_AbsentIsRefused: DepthRank is
        // no longer a member, so its absence is the wire form. A document
        // still stating it, or the retired EquityLoss, reads with the member
        // ignored, as every member this category does not have is — a stated
        // number never overrides the derivation.
        var candidate = TestRecords.Candidate(play: [new(8, 5), new(6, 1)]);
        var document = WirePaths.Document(candidate);
        document["DepthRank"] = 7;
        document["EquityLoss"] = 0.5;

        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = JsonSerializer.Deserialize<PlayCandidate>(document.ToJsonString(), options)!;
            Assert.Equal(candidate.DepthRank, restored.DepthRank);
            Assert.Equal(30, restored.DepthRank);
        }
    }

    [Fact]
    public void PlayCandidate_AnalysisModeAndLevel_RoundTrip()
    {
        var original = TestRecords.Candidate(
            play: [new(8, 5), new(8, 5), new(6, 3), new(6, 3)],
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
        AssertAbsentIsRefused(TestRecords.Candidate(play: [new(8, 5), new(6, 1)]), "AnalysisMode", "AnalysisLevel");
    }

    [Fact]
    public void PlayCandidate_LegacyDepthClassJson_IsRefused_TheRetiredPropertyStillIgnored()
    {
        // Rewritten from PlayCandidate_LegacyDepthClassJson_DeserializesToUnknownPair.
        // JSON written before the two-axis pair existed lacks it; the pair is
        // required now (halheinrich/backgammon#222), so such a document is
        // refused rather than read as "depth not recorded". The retired flat
        // "DepthClass" property is still an unrecognized property, ignored
        // beside a full candidate: a candidate has no member of either
        // decision kind, so it keeps the serializer's tolerance.
        var legacy = "{\"MoveNotation\":\"8/5 6/1\",\"Depth\":\"3-ply\",\"DepthClass\":\"Ply3\",\"Equity\":-0.12}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlayCandidate>(legacy, Options));

        var candidate = TestRecords.Candidate(play: [new(8, 5), new(6, 1)]);
        var full = JsonNode.Parse(JsonSerializer.Serialize(candidate, Options))!.AsObject();
        full["DepthClass"] = "Ply3";
        var restored = JsonSerializer.Deserialize<PlayCandidate>(full.ToJsonString(), Options)!;

        Assert.Equal(candidate.AnalysisMode, restored.AnalysisMode);
        Assert.Equal(candidate.AnalysisLevel, restored.AnalysisLevel);
        Assert.Equal("3-ply", restored.Depth);
    }

    [Fact]
    public void PlayCandidate_Play_AbsentIsRefused()
    {
        // Rewritten from PlayCandidate_Play_DefaultsToEmpty: an absent play
        // would read as the empty play, a pass.
        AssertAbsentIsRefused(TestRecords.Candidate(play: [new(8, 5), new(6, 1)]), "Play");
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_Empty()
    {
        // Rewritten for halheinrich/backgammon#273: the fixture stored the
        // notation "8/5 6/1" beside an empty play, the disagreement a stored
        // copy allowed. The notation is the play's now, so a pass reads as
        // the empty string.
        var original = TestRecords.Candidate(
            play: [],
            equity: -0.142);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(0, restored.Play.Count);
        Assert.Contains("\"Play\":[]", json);
        Assert.Equal(string.Empty, restored.Notation);
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_Populated()
    {
        Play play = [new(13, 7), new(8, 5)];

        var original = TestRecords.Candidate(
            equity: -0.118,
            play: play);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(2, restored.Play.Count);
        Assert.Equal(new Move(13, 7), restored.Play[0]);
        Assert.Equal(new Move(8, 5), restored.Play[1]);
        Assert.True(restored.Play.IsSameEncoding(original.Play));
        Assert.Equal("13/7 8/5", restored.Notation);
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_PreservesHitEncoding()
    {
        Play play = [new(13, -7)];   // hit on the 7-point

        var original = TestRecords.Candidate(
            equity: 0.05,
            play: play);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(1, restored.Play.Count);
        Assert.Equal(13, restored.Play[0].FrPt);
        Assert.Equal(-7, restored.Play[0].ToPt);
        Assert.Equal("13/7*", restored.Notation);
    }

    [Fact]
    public void PlayCandidate_Play_NestedInBgDecisionData_RoundTrip()
    {
        Play play = [new(24, 18), new(13, 9)];

        var original = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(
            dice: [6, 4],
            plays: [TestRecords.Candidate(equity: 0.211, play: play)]));

        var restored = Assert.IsType<CheckerPlayDecision>(RoundTrip(original));

        var restoredPlay = restored.Decision.Plays[0].Play;
        Assert.Equal(2, restoredPlay.Count);
        Assert.Equal(new Move(24, 18), restoredPlay[0]);
        Assert.Equal(new Move(13, 9), restoredPlay[1]);
        Assert.Equal("24/18 13/9", restored.Decision.Plays[0].Notation);
    }

    // -----------------------------------------------------------------------
    //  CubeDecisionData — the cube analysis's depth (renamed from the
    //  Cube-prefixed members of the retired flat DecisionData)
    // -----------------------------------------------------------------------

    [Fact]
    public void CubeDecisionData_Depth_RoundTrip()
    {
        // Rewritten from DecisionData_CubeDepth_RoundTrip: the facts round-trip,
        // and the label is derived from them.
        var original = TestRecords.CubeData(
            analysisMode: AnalysisMode.Rollout, analysisLevel: AnalysisLevel.Ply3, rolloutTrials: 1296);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        Assert.DoesNotContain("\"Depth\"", json);
        Assert.Equal("Rollout: 1296 trials. 3-ply", restored.Depth);
    }

    [Fact]
    public void CubeDecisionData_Depth_NoneRecorded_IsNull()
    {
        // Rewritten from CubeDecisionData_Depth_AbsentReadsAsNoneRecorded
        // (itself from ..._AbsentIsRefused): no depth recorded, no label.
        Assert.Null(TestRecords.CubeData(analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown).Depth);
    }

    [Fact]
    public void CubeDecisionData_DepthAbbreviation_RoundTrip()
    {
        // Rewritten from DecisionData_CubeDepthAbbreviation_RoundTrip.
        var original = TestRecords.CubeData(
            analysisMode: AnalysisMode.Rollout, analysisLevel: AnalysisLevel.Ply3, rolloutTrials: 1296);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        Assert.DoesNotContain("DepthAbbreviation", json);
        Assert.Equal("3p1296", restored.DepthAbbreviation);
    }

    [Fact]
    public void CubeDecisionData_DepthAbbreviation_NoneRecorded_IsNull()
    {
        // Rewritten from CubeDecisionData_DepthAbbreviation_AbsentReadsAsNoneRecorded.
        Assert.Null(TestRecords.CubeData(analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown)
            .DepthAbbreviation);
    }

    [Fact]
    public void CubeDecisionData_DepthRank_IsDerivedFromTheModeAndLevel_AfterARoundTrip()
    {
        // Rewritten from CubeDecisionData_DepthRank_RoundTrip (itself from
        // DecisionData_CubeDepthRank_RoundTrip): the rank is derived from the
        // mode and level the wire carries, never stated — a book rollout ranks
        // 99 whatever its level.
        var original = TestRecords.CubeData(
            analysisMode: AnalysisMode.BookRollout, analysisLevel: AnalysisLevel.XgRoller);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        Assert.DoesNotContain("DepthRank", json);
        Assert.Equal(99, restored.DepthRank);
    }

    [Fact]
    public void CubeDecisionData_RetiredDepthRank_IsIgnored_TheDerivationStands()
    {
        // Rewritten from CubeDecisionData_DepthRank_AbsentIsRefused: DepthRank
        // is no longer stored, so its absence is the wire form. A document
        // still stating it reads with it ignored — the serializer knows the
        // derived member and skips its JSON even where unmapped members are
        // refused (measured on .NET 10, both paths) — and the derivation
        // stands: a stated rank never overrides it.
        var document = WirePaths.Document(TestRecords.CubeData());
        document["DepthRank"] = 7;

        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal(30, JsonSerializer.Deserialize<CubeDecisionData>(document.ToJsonString(), options)!.DepthRank);
    }

    [Fact]
    public void CubeDecisionData_AnalysisModeAndLevel_RoundTrip()
    {
        // Rewritten from DecisionData_CubeAnalysisModeAndLevel_RoundTrip.
        // BookRollout + XG Roller is the motivating cube case: the shipped
        // opening-book database carries cube rollout levels of XG Roller,
        // which the retired flat depth class could not represent.
        var original = TestRecords.CubeData(
            analysisMode: AnalysisMode.BookRollout,
            analysisLevel: AnalysisLevel.XgRoller);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        Assert.Contains("\"AnalysisMode\":\"BookRollout\"", json);
        Assert.Contains("\"AnalysisLevel\":\"XgRoller\"", json);
        Assert.Equal(AnalysisMode.BookRollout, restored.AnalysisMode);
        Assert.Equal(AnalysisLevel.XgRoller, restored.AnalysisLevel);
    }

    [Fact]
    public void CubeDecisionData_AnalysisModeAndLevel_AbsentIsRefused()
    {
        // Rewritten from DecisionData_CubeAnalysisModeAndLevel_AbsentIsRefused.
        AssertAbsentIsRefused(TestRecords.CubeData(), "AnalysisMode", "AnalysisLevel");
    }

    [Fact]
    public void CubeDecisionData_RetiredMembers_AreRefused()
    {
        // Rewritten from DecisionData_LegacyCubeDepthClassJson_IsRefused_TheRetiredPropertyStillIgnored.
        // The legacy document is still refused. What changed is the retired
        // flat "CubeDepthClass" beside a full cube decision: the cube
        // category refuses every member it does not have — a checker play's
        // and a retired one alike — because a member of the other kind must
        // not be dropped in silence (halheinrich/backgammon#273).
        var legacy = "{\"Dice\":[0,0],\"IsCube\":true,\"CubeDepth\":\"3-ply\",\"CubeDepthClass\":\"Ply3\",\"NoDoubleEquity\":0.312}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CubeDecisionData>(legacy, Options));

        foreach (var (member, value) in new (string, JsonNode)[]
                 { ("CubeDepthClass", "Ply3"), ("IsCube", true), ("CubeDepth", "3-ply") })
        {
            var full = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.CubeData(), Options))!.AsObject();
            full[member] = value;
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<CubeDecisionData>(full.ToJsonString(), Options));
        }
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

        // Rewritten: the away scores and the Crawford flag are the match
        // session's (halheinrich/backgammon#273), read back through it.
        var original = TestRecords.Position(
            mop: new BoardPosition(mop),
            cubeSize: 2,
            cubeOwner: CubeOwner.OnRoll,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 3, opponentNeeds: 5, isCrawford: false));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PositionData>(json, Options)!;

        Assert.Equal(original.Mop, restored.Mop);
        Assert.Equal(original.CubeSize, restored.CubeSize);
        Assert.Equal(original.CubeOwner, restored.CubeOwner);
        var match = Assert.IsType<MatchSession>(restored.Session);
        Assert.Equal((7, 3, 5, false), (match.Length, match.OnRollNeeds, match.OpponentNeeds, match.IsCrawford));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PositionData_MoneySessionsJacobyRule_RoundTrips(bool isJacoby)
    {
        // Rewritten from PositionData_IsJacoby_RoundTrips: two states, not
        // three. The rule is the money session's, and every money session
        // states it — "not supplied" is gone with the stand-in it served.
        var original = TestRecords.Position(session: TestRecords.MoneySession(isJacoby: isJacoby));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PositionData>(json, Options)!;

        Assert.Equal(isJacoby, Assert.IsType<MoneySession>(restored.Session).IsJacoby);
    }

    [Fact]
    public void PositionData_MoneySessionWithoutItsJacobyRule_IsRefused_BothPaths()
    {
        // Rewritten from PositionData_IsJacoby_AbsentFromJson_ReadsAsNotSupplied:
        // a money session without its rule used to read as "unknown", the
        // no-key rung's input. The rule is required now, so the absence is
        // refused rather than read as anything.
        var document = WirePaths.Document(TestRecords.Position(session: TestRecords.MoneySession()));
        document["Session"]!.AsObject().Remove("IsJacoby");

        WirePaths.AssertRefused<PositionData>(document.ToJsonString());
    }

    [Fact]
    public void PositionData_MoneySessionsJacobyRule_SerializesUnderItsOwnName_InsideTheSession()
    {
        // Rewritten from PositionData_IsJacoby_SerializesUnderItsOwnName.
        var json = JsonSerializer.Serialize(TestRecords.Position(session: TestRecords.MoneySession(isJacoby: true)), Options);

        Assert.Contains("\"Session\":{\"Kind\":\"Money\",\"IsJacoby\":true}", json);
    }

    // -----------------------------------------------------------------------
    //  The two decision categories
    // -----------------------------------------------------------------------

    [Fact]
    public void CheckerPlayDecisionData_RoundTrip()
    {
        // Rewritten from DecisionData_RoundTrip_PlayDecision: the checker
        // play's own category, with no IsCube to assert — the type is the kind.
        var original = TestRecords.CheckerPlayData(
            dice: [3, 5],
            plays: [
                TestRecords.Candidate(play: [new(8, 5), new(6, 1)], equity: -0.120),
                TestRecords.Candidate(play: [new(8, 3), new(6, 1)], equity: -0.165)
            ]);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CheckerPlayDecisionData>(json, Options)!;

        Assert.Equal(original.Dice, restored.Dice);
        Assert.Equal(2, restored.Plays.Count);
        Assert.Equal("8/5 6/1", restored.Plays[0].Notation);
        Assert.Equal("3-ply", restored.Plays[0].Depth);
        Assert.Equal("3-ply", restored.Plays[1].Depth);
        // Rewritten: the loss is a ranking's derivation from the equities.
        Assert.Equal(-0.120 - -0.165, restored.RankedBy(PlayRanking.Equity).ForCandidate(1).Error);
    }

    [Fact]
    public void CubeDecisionData_RoundTrip()
    {
        // Rewritten from DecisionData_RoundTrip_CubeDecision.
        var original = TestRecords.CubeData(
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            winPctAfterNoDouble: 0.621,
            gammonPctAfterNoDouble: 0.183,
            bgPctAfterNoDouble: 0.012,
            loseGammonPctAfterNoDouble: 0.091,
            loseBgPctAfterNoDouble: 0.003,
            winPctAfterDoubleTake: 0.618,
            gammonPctAfterDoubleTake: 0.181,
            bgPctAfterDoubleTake: 0.011,
            loseGammonPctAfterDoubleTake: 0.093,
            loseBgPctAfterDoubleTake: 0.004,
            probOfOpponentErrorJustifyingDouble: 0.078);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

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
        // Rewritten: the match length left the category for the match
        // session (halheinrich/backgammon#273).
        var original = TestRecords.Descriptive(
            onRollName: "Mochy",
            opponentName: "Falafel",
            title: null,
            date: null,
            @event: null,
            isStandardStart: null);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;

        Assert.Equal(original.OnRollName, restored.OnRollName);
        Assert.Equal(original.OpponentName, restored.OpponentName);
        Assert.Null(restored.Title);
        Assert.Null(restored.Date);
        Assert.Null(restored.Event);
        Assert.Null(restored.IsStandardStart);
    }

    [Fact]
    public void DescriptiveData_RoundTrip_DateOnly()
    {
        var original = TestRecords.Descriptive(
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
    public void BgDecisionData_SourceFile_IsTheIdsFilename_AfterARoundTrip()
    {
        // Rewritten from DescriptiveData_RoundTrip_WithSourceFile: the source
        // file left the descriptive category. The Id stores it and the record
        // derives it, so the two cannot disagree and the wire states it once.
        var original = TestRecords.CheckerPlay(id: new XgDecisionId("mochy-falafel.xg", 1, 1, IsCube: false));
        var json = JsonSerializer.Serialize<BgDecisionData>(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        Assert.Equal("mochy-falafel.xg", original.SourceFile);
        Assert.Equal("mochy-falafel.xg", restored.SourceFile);
        Assert.DoesNotContain("SourceFile", json);
        Assert.Equal("session.xgp", TestRecords.Cube(id: new XgpDecisionId("session.xgp")).SourceFile);
    }

    [Fact]
    public void DescriptiveData_RetiredSourceFile_IsIgnored_TheIdsStands()
    {
        // Rewritten from DescriptiveData_SourceFile_AbsentReadsAsNull: there is
        // no SourceFile member to be absent, and every decision has one. A
        // document still stating it in the descriptive category reads with it
        // ignored, as every member that category does not have is: a stated
        // name cannot contradict the Id.
        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        document["Descriptive"]!["SourceFile"] = "other.xg";

        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal("match.xg", JsonSerializer.Deserialize<BgDecisionData>(document.ToJsonString(), options)!.SourceFile);
    }

    // -----------------------------------------------------------------------
    //  BgDecisionData — full records
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_RoundTrip()
    {
        // Rewritten onto the checker-play kind: the record reads back as the
        // same kind, with every category.
        var mop = new int[26];
        mop[6] = -5; mop[8] = -3; mop[13] = 5; mop[24] = 2;

        var original = TestRecords.CheckerPlay(
            position: TestRecords.Position(
                mop: new BoardPosition(mop),
                cubeSize: 4,
                cubeOwner: CubeOwner.Centered,
                session: TestRecords.MatchSession(length: 5, onRollNeeds: 2, opponentNeeds: 4)),
            decision: TestRecords.CheckerPlayData(
                dice: [6, 4],
                plays: [
                    TestRecords.Candidate(play: [new(24, 18), new(24, 20)], equity: 0.211),
                    TestRecords.Candidate(play: [new(24, 18), new(13, 9)],  equity: 0.198)
                ]),
            descriptive: TestRecords.Descriptive(
                onRollName: "Hal",
                opponentName: "Bot",
                title: "Opening Run",
                date: new DateOnly(2025, 3, 1),
                @event: "Test Match"));

        var restored = Assert.IsType<CheckerPlayDecision>(RoundTrip(original));

        Assert.Equal(original.Position.Mop, restored.Position.Mop);
        Assert.Equal(original.Position.CubeOwner, restored.Position.CubeOwner);
        Assert.Equal(5, Assert.IsType<MatchSession>(restored.Session).Length);
        Assert.Equal(original.Decision.Dice, restored.Decision.Dice);
        Assert.Equal(2, restored.Decision.Plays.Count);
        Assert.Equal(0.211 - 0.198, restored.Decision.RankedBy(PlayRanking.Equity).ForCandidate(1).Error);
        Assert.Equal(original.Descriptive.OnRollName, restored.Descriptive.OnRollName);
        Assert.Equal(original.Descriptive.Date, restored.Descriptive.Date);
        Assert.Equal(original.Descriptive.Event, restored.Descriptive.Event);
        Assert.Equal(original.SourceFile, restored.SourceFile);
    }

    // -----------------------------------------------------------------------
    //  The user's errors — the play's under a ranking, the cube's on its kind
    // -----------------------------------------------------------------------

    [Fact]
    public void CheckerPlayDecisionData_RoundTrip_WithTheUsersPlayError()
    {
        // Rewritten: the user's error is no longer stated beside the candidate
        // the user played — it is that candidate's loss against a ranking's
        // best, derived from the equities the wire carries, so it round-trips
        // as they do and is not written.
        var original = TestRecords.CheckerPlayData(
            dice: [3, 5],
            plays: [
                TestRecords.Candidate(play: [new(8, 5), new(6, 1)], equity: -0.120),
                TestRecords.Candidate(play: [new(8, 3), new(6, 1)], equity: -0.165)
            ],
            userPlayIndex: 1);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CheckerPlayDecisionData>(json, Options)!;

        Assert.All(Enum.GetValues<PlayRanking>(), ranking =>
            Assert.Equal(PlayerResult.Scored(-0.120 - -0.165), restored.RankedBy(ranking).PlayerResult));
        Assert.Equal(1, restored.UserPlayIndex);
        Assert.DoesNotContain("\"UserPlayError\"", json);
        Assert.DoesNotContain("UserDoubleError", json);
        Assert.DoesNotContain("UserTakeError", json);
    }

    [Fact]
    public void CubeDecisionData_RoundTrip_WithUserErrors()
    {
        // Rewritten: the cube errors are the stated actions' losses against
        // the best actions, derived from the equities — a double where no
        // double (+0.312) beats double/take (+0.287) loses 0.025, and taking
        // a double/take under 1 loses nothing — so nothing error-shaped is
        // written.
        var original = TestRecords.CubeData(
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            userDoublerAction: CubeAction.Double,
            userTakerAction: CubeAction.Take);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        Assert.Equal(0.025, restored.UserDoubleError!.Value, 12);
        Assert.Equal(0.0, restored.UserTakeError);
        Assert.DoesNotContain("UserDoubleError", json);
        Assert.DoesNotContain("UserTakeError", json);
        Assert.DoesNotContain("UserPlayError", json);
    }

    [Fact]
    public void UserErrors_NullOrAbsent_ReadAsNull_EachKind()
    {
        // Rewritten from DecisionData_UserErrors_NullOrAbsent_ReadAsNull: the
        // stored errors are the ones nothing determines (an unlisted play's,
        // an unstated action's); each is nullable, so a null round-trips and
        // an absent member reads as null, and with nothing stated to derive
        // from, the user's errors are null too.
        var play = JsonSerializer.Deserialize<CheckerPlayDecisionData>(
            JsonSerializer.Serialize(TestRecords.CheckerPlayData(userPlayIndex: null), Options), Options)!;
        var cube = JsonSerializer.Deserialize<CubeDecisionData>(
            JsonSerializer.Serialize(TestRecords.CubeData(userDoublerAction: null, userTakerAction: null), Options), Options)!;

        Assert.All(Enum.GetValues<PlayRanking>(), ranking => Assert.Equal(PlayerResult.NotRecorded, play.RankedBy(ranking).PlayerResult));
        Assert.Null(cube.UserDoubleError);
        Assert.Null(cube.UserTakeError);

        var playAbsent = ReadWithout(
            TestRecords.CheckerPlayData(userPlayIndex: null, unlistedPlayError: 0.1), "UnlistedPlayError");
        Assert.Null(playAbsent.UnlistedPlayError);
        Assert.All(Enum.GetValues<PlayRanking>(), ranking => Assert.Equal(PlayerResult.NotRecorded, playAbsent.RankedBy(ranking).PlayerResult));
        var cubeAbsent = ReadWithout(
            TestRecords.CubeData(userDoublerAction: null, userTakerAction: null,
                unstatedDoublerActionError: 0.2, unstatedTakerActionError: 0.3),
            "UnstatedDoublerActionError", "UnstatedTakerActionError");
        Assert.Null(cubeAbsent.UserDoubleError);
        Assert.Null(cubeAbsent.UserTakeError);
    }

    // -----------------------------------------------------------------------
    //  UserDoublerAction / UserTakerAction — played cube actions
    // -----------------------------------------------------------------------

    [Fact]
    public void CubeDecisionData_UserCubeActions_RoundTrip_DoubleWithResponse()
    {
        // Rewritten from DecisionData_UserCubeActions_RoundTrip_DoubleWithResponse.
        var original = TestRecords.CubeData(
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            userDoublerAction: CubeAction.Double,
            userTakerAction: CubeAction.Take);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        // String form via CubeAction's bundled converter — no options-level
        // registration.
        Assert.Contains("\"UserDoublerAction\":\"Double\"", json);
        Assert.Contains("\"UserTakerAction\":\"Take\"", json);
        Assert.Equal(CubeAction.Double, restored.UserDoublerAction);
        Assert.Equal(CubeAction.Take, restored.UserTakerAction);
    }

    [Fact]
    public void CubeDecisionData_UserCubeActions_RoundTrip_UndoubledGame()
    {
        // Rewritten from DecisionData_UserCubeActions_RoundTrip_UndoubledGame.
        // No double was offered, so no taker decision exists — the taker half
        // stays null by shape while the doubler half records the NoDouble.
        var original = TestRecords.CubeData(
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            userDoublerAction: CubeAction.NoDouble,
            userTakerAction: null);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        Assert.Equal(CubeAction.NoDouble, restored.UserDoublerAction);
        Assert.Null(restored.UserTakerAction);
    }

    [Fact]
    public void CubeDecisionData_UserCubeActions_NullRoundTrips()
    {
        // Rewritten from DecisionData_UserCubeActions_NullRoundTrips: "not
        // recorded" is the null a producer states, and it round-trips.
        var json = JsonSerializer.Serialize(
            TestRecords.CubeData(userDoublerAction: null, userTakerAction: null), Options);
        var d = JsonSerializer.Deserialize<CubeDecisionData>(json, Options)!;

        Assert.Contains("\"UserDoublerAction\":null", json);
        Assert.Null(d.UserDoublerAction);
        Assert.Null(d.UserTakerAction);
    }

    [Fact]
    public void CubeDecisionData_UserCubeActions_Absent_ReadAsNull()
    {
        // Rewritten from DecisionData_LegacyJsonWithoutUserCubeActions_DeserializesToNull:
        // a cube decision without the two reads both back null — played
        // action not recorded, never an error. The errors were derived from
        // the actions, so with the actions gone there is no error either.
        var restored = ReadWithout(
            TestRecords.CubeData(
                noDoubleEquity: 0.312, doubleTakeEquity: 0.287,
                userDoublerAction: CubeAction.Double, userTakerAction: CubeAction.Take),
            "UserDoublerAction", "UserTakerAction");

        Assert.Null(restored.UserDoubleError);
        Assert.Null(restored.UserDoublerAction);
        Assert.Null(restored.UserTakerAction);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — a record's view, built for a ranking. The
    //  members here do not depend on it; where the rankings differ is pinned
    //  in PlayRankingTests.
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CheckerPlay()
    {
        var mop = new int[26];
        mop[1] = 2; mop[6] = -5;

        // Rewritten: the view's away scores and Crawford flag are its match
        // session's.
        var session = TestRecords.MatchSession(onRollNeeds: 3, opponentNeeds: 5, isCrawford: false);
        IDecisionFilterData data = TestRecords.CheckerPlay(
            position: TestRecords.Position(mop: new BoardPosition(mop), session: session),
            decision: TestRecords.CheckerPlayData(
                plays: [TestRecords.Candidate(play: [new(1, 0), new(1, 0)])],
                userPlayIndex: null,
                unlistedPlayError: 0.034),
            descriptive: TestRecords.Descriptive(onRollName: "Hal")).ViewFor(PlayRanking.Equity);

        Assert.Equal("Hal", data.Player);
        Assert.Equal(DecisionKind.CheckerPlay, data.Kind);
        Assert.Same(session, data.Session);
        Assert.Equal(PlayerResult.Unstated(0.034), data.PlayerResult);
        Assert.Equal(new BoardPosition(mop), data.Board);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CubePlay_UserDoubleError()
    {
        // Rewritten: the errors are derived from the stated actions — no
        // double +1.025 against the cash doubles at a loss of 0.025, and
        // taking double/take +1.011 loses 0.011.
        IDecisionFilterData data = TestRecords.Cube(decision: TestRecords.CubeData(
            noDoubleEquity: 1.025,
            doubleTakeEquity: 1.011)).ViewFor(PlayRanking.Equity);

        Assert.Equal(DecisionKind.Cube, data.Kind);
        Assert.Equal(PlayerResultKind.Scored, data.PlayerResult.Kind);  // UserDoubleError takes precedence
        Assert.True(data.PlayerResult.TryGetError(out double error));
        Assert.Equal(0.025, error, 12);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CubePlay_UserTakeError()
    {
        // Rewritten: no doubler action stated, so no doubling error; the take
        // of double/take +1.011 loses 0.011.
        IDecisionFilterData data = TestRecords.Cube(decision: TestRecords.CubeData(
            noDoubleEquity: 1.025,
            doubleTakeEquity: 1.011,
            userDoublerAction: null)).ViewFor(PlayRanking.Equity);

        Assert.Equal(PlayerResultKind.Scored, data.PlayerResult.Kind);  // Falls through to UserTakeError
        Assert.True(data.PlayerResult.TryGetError(out double error));
        Assert.Equal(0.011, error, 12);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_Board_MatchesMop()
    {
        var mop = new int[26];
        mop[6] = -5; mop[13] = 5;

        IDecisionFilterData data = TestRecords.Cube(position: TestRecords.Position(mop: new BoardPosition(mop))).ViewFor(PlayRanking.Equity);

        Assert.Equal(new BoardPosition(mop), data.Board);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MatchLength()
    {
        // Rewritten: a match's length is its session's, which the view is.
        IDecisionFilterData data = TestRecords.CheckerPlay(
            position: TestRecords.Position(session: TestRecords.MatchSession(length: 11))).ViewFor(PlayRanking.Equity);

        Assert.Equal(11, Assert.IsType<MatchSession>(data.Session).Length);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MoneySession_IsItsKind()
    {
        // Rewritten from BgDecisionData_IDecisionFilterData_IsMoneyGame_MoneySession:
        // money is the session's kind, never a match length of 0 — there is
        // no IsMoneyGame left to derive from one.
        IDecisionFilterData data = TestRecords.CheckerPlay(
            position: TestRecords.Position(session: TestRecords.MoneySession())).ViewFor(PlayRanking.Equity);

        Assert.Equal(SessionKind.Money, data.Session.Kind);
        Assert.IsType<MoneySession>(data.Session);
    }

    [Theory]
    [InlineData(1)]  // shortest possible match
    [InlineData(11)]
    public void BgDecisionData_IDecisionFilterData_AMatchOfAnyLength_IsAMatch(int length)
    {
        // Rewritten from BgDecisionData_IDecisionFilterData_IsMoneyGame_FalseForAnyMatchLength.
        IDecisionFilterData data = TestRecords.CheckerPlay(
            position: TestRecords.Position(session: TestRecords.MatchSession(length: length, onRollNeeds: 1, opponentNeeds: 1)))
            .ViewFor(PlayRanking.Equity);

        Assert.Equal(SessionKind.Match, data.Session.Kind);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — the Jacoby rule, a money session's
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BgDecisionData_IDecisionFilterData_MoneySession_CarriesItsJacobyRule(bool isJacoby)
    {
        // Rewritten from BgDecisionData_IDecisionFilterData_IsJacoby_MoneyRecord_CarriesTheValue.
        IDecisionFilterData data = TestRecords.CheckerPlay(
            position: TestRecords.Position(session: TestRecords.MoneySession(isJacoby: isJacoby))).ViewFor(PlayRanking.Equity);

        Assert.Equal(isJacoby, Assert.IsType<MoneySession>(data.Session).IsJacoby);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_AMatch_HasNoJacobyRuleToRead()
    {
        // Rewritten from BgDecisionData_IDecisionFilterData_IsJacoby_MatchRecord_IsNull,
        // and in place of BgDecisionData_IDecisionFilterData_IsJacoby_ForwardsPositionVerbatim,
        // which pinned a stray stamp on a match passing through: a match
        // carries no Jacoby rule at all, so neither a null nor a stray stamp
        // is expressible — the member is not on the match's type.
        IDecisionFilterData data = TestRecords.CheckerPlay().ViewFor(PlayRanking.Equity);

        var match = Assert.IsType<MatchSession>(data.Session);
        Assert.Null(match.GetType().GetProperty("IsJacoby"));
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_AMoneySessionWithoutItsRule_CannotBeBuilt()
    {
        // Rewritten from BgDecisionData_IDecisionFilterData_IsJacoby_MoneyRecordUnstamped_IsNull:
        // the unknown rung is gone. A money session states its rule — the
        // member is required — so a money view under an unknown rule cannot
        // exist to be matched by neither money token.
        var isJacoby = typeof(MoneySession).GetProperty(nameof(MoneySession.IsJacoby))!;

        Assert.Equal(typeof(bool), isJacoby.PropertyType);
        Assert.True(isJacoby.IsDefined(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), inherit: false));
    }

    [Fact]
    public void BgDecisionData_TheSessionIsOnTheWire_InsideThePosition_AndNoMatchLengthStandsInForMoney()
    {
        // Rewritten from BgDecisionData_IsMoneyGame_NotSerialized_MatchLengthRemainsTheWire:
        // the wire states money as the session's kind, never as
        // Descriptive.MatchLength 0, and no predicate derived from a 0 is left.
        var data = TestRecords.CheckerPlay(position: TestRecords.Position(session: TestRecords.MoneySession()));
        var json = JsonSerializer.Serialize<BgDecisionData>(data, Options);

        Assert.DoesNotContain("IsMoneyGame", json);
        Assert.DoesNotContain("MatchLength", json);
        Assert.Contains("\"Session\":{\"Kind\":\"Money\",", json);

        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;
        Assert.IsType<MoneySession>(restored.Session);
        Assert.IsType<MoneySession>(restored.ViewFor(PlayRanking.Equity).Session);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — AnalysisMode / AnalysisLevel derivation
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CubeDecision_UseCubeAnalysis()
    {
        IDecisionFilterData data = TestRecords.Cube(decision: TestRecords.CubeData(
            analysisMode: AnalysisMode.Evaluation,
            analysisLevel: AnalysisLevel.XgRollerPlus)).ViewFor(PlayRanking.Equity);

        Assert.Equal(AnalysisMode.Evaluation, data.AnalysisMode);
        Assert.Equal(AnalysisLevel.XgRollerPlus, data.AnalysisLevel);
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CheckerPlay_UseBestPlayCandidate()
    {
        // The best deliberately not the first candidate, to pin that the
        // derivation reads the best rather than the first. Rewritten: the best
        // is a ranking's, no longer a stated index — here the deeper and the
        // higher equity alike, so both rankings name it.
        var record = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(
            plays: [
                TestRecords.Candidate(
                    play: [new(8, 5), new(6, 5)],
                    analysisMode: AnalysisMode.Evaluation,
                    analysisLevel: AnalysisLevel.Ply3,
                    equity: 0.1),
                TestRecords.Candidate(
                    play: [new(8, 4), new(6, 5)],
                    analysisMode: AnalysisMode.Rollout,
                    analysisLevel: AnalysisLevel.Ply1,
                    equity: 0.2)
            ]));

        foreach (var data in Enum.GetValues<PlayRanking>().Select(record.ViewFor))
        {
            Assert.Equal(AnalysisMode.Rollout, data.AnalysisMode);
            Assert.Equal(AnalysisLevel.Ply1, data.AnalysisLevel);
        }
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CheckerPlay_TheBestCannotBeStated()
    {
        // Rewritten from ..._OutOfRangeBestPlayIndex_CannotBeBuilt (itself
        // from ..._ReturnUnknown): the best play is a ranking's derivation
        // from the candidates, so there is no index to state out of range —
        // no member at all. A document stating one, or the retired player's
        // error, is refused like any member the category does not have, read
        // alone or inside a record (the umbrella's third-round ruling).
        Assert.Null(typeof(CheckerPlayDecisionData).GetProperty("BestPlayIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.Null(typeof(CheckerPlayDecisionData).GetProperty("UserPlayError", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

        foreach (var (member, stated) in new (string, JsonNode)[] { ("BestPlayIndex", 0), ("BestPlayIndex", 7), ("UserPlayError", 0.0) })
        {
            var category = WirePaths.Document(TestRecords.CheckerPlayData());
            category[member] = stated.DeepClone();
            WirePaths.AssertRefused<CheckerPlayDecisionData>(category.ToJsonString());

            var record = WirePaths.Document<BgDecisionData>(TestRecords.CheckerPlay());
            record["Decision"]![member] = stated.DeepClone();
            WirePaths.AssertRefused<BgDecisionData>(record.ToJsonString());
        }
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_CheckerPlay_EmptyPlays_CannotBeBuilt()
    {
        // Rewritten from ..._EmptyPlays_ReturnUnknown: an empty candidate list
        // was the cube decision's stand-in; a checker play has at least one.
        var ex = Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlayData(plays: []));

        Assert.Equal("Plays", ex.ParamName);
    }

    [Fact]
    public void BgDecisionData_AnalysisModeAndLevel_NoCandidates_CannotBeRead()
    {
        // Rewritten from BgDecisionData_AnalysisModeAndLevel_NoCandidates_AreUnknown:
        // the wire refuses the no-candidate document too, on both paths.
        var document = WirePaths.Document<BgDecisionData>(TestRecords.CheckerPlay());
        document["Decision"]!["Plays"] = new JsonArray();

        var ex = WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    // -----------------------------------------------------------------------
    //  Dice — the checker play's own
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_Dice_CheckerPlay_ForwardsFromDecision()
    {
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(
            dice: [6, 3], plays: [TestRecords.Candidate(play: [new(24, 18), new(24, 21)])]));

        Assert.Equal(new DiceRoll(6, 3), play.Dice);
        Assert.All(Enum.GetValues<PlayRanking>(), ranking => Assert.Equal(new DiceRoll(6, 3), play.ViewFor(ranking).Dice));
    }

    [Fact]
    public void BgDecisionData_Dice_RolledOrderCanonicalized()
    {
        // The XG parser stamps the dice in rolled order; the forwarding
        // property canonicalizes, so [1, 3] reads back high-first while the
        // stored pair keeps the rolled order.
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(dice: [1, 3]));

        Assert.Equal(new DiceRoll(3, 1), play.Dice);
        Assert.Equal(new[] { 1, 3 }, play.Decision.Dice);
    }

    [Fact]
    public void BgDecisionData_Dice_CubeDecision_IsNull()
    {
        // Rewritten: a cube decision has no dice to be [0, 0] — it has no Dice
        // member at all, and reads null through the filter interface.
        IDecisionFilterData data = TestRecords.Cube().ViewFor(PlayRanking.Equity);

        Assert.Null(data.Dice);
        Assert.Null(typeof(CubeDecision).GetProperty("Dice"));
        Assert.Null(typeof(CubeDecisionData).GetProperty("Dice"));
    }

    [Fact]
    public void BgDecisionData_Dice_MalformedStoredDice_CannotBeBuilt()
    {
        // Rewritten from BgDecisionData_Dice_MalformedStoredDice_Throws: the
        // {0, 0} that was a cube's dice is refused on a checker play at
        // construction, so the canonical derivation never meets it.
        Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.CheckerPlayData(dice: [0, 0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.CheckerPlayData(dice: [7, 1]));
        Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlayData(dice: [3]));
    }

    [Fact]
    public void BgDecisionData_Dice_NotSerialized_DecisionDiceRemainsTheWire()
    {
        var original = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(
            dice: [6, 3], plays: [TestRecords.Candidate(play: [new(24, 18), new(24, 21)])]));

        var json = JsonSerializer.Serialize<BgDecisionData>(original, Options);

        // The nested int-pair wire is unchanged; the derived canonical token
        // never appears.
        Assert.Contains("\"Dice\":[6,3]", json);
        Assert.DoesNotContain("\"Dice\":\"63\"", json);

        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;
        Assert.Equal(new DiceRoll(6, 3), restored.ViewFor(PlayRanking.Equity).Dice);
    }

    // -----------------------------------------------------------------------
    //  After-boards — derived by the checker play (see also
    //  AfterBoardDerivationTests); PlayOutcomeData is retired
    // -----------------------------------------------------------------------

    [Fact]
    public void CheckerPlayDecision_AfterBoards_SurviveARoundTrip_BothPaths()
    {
        // Rewritten from PlayOutcomeData_RoundTrip_Populated: the boards are
        // derived from the position and the candidates, so a round trip
        // re-derives the same two.
        var original = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 2));

        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = Assert.IsType<CheckerPlayDecision>(RoundTrip(original, options));
            foreach (var ranking in Enum.GetValues<PlayRanking>())
                Assert.Equal(original.AfterBoardOfBest(ranking), restored.AfterBoardOfBest(ranking));
            Assert.Equal(original.AfterPlayerBoard, restored.AfterPlayerBoard);
        }
    }

    [Fact]
    public void CheckerPlayDecision_AfterPlayerBoard_NoUserPlay_IsNull()
    {
        // Rewritten from PlayOutcomeData_AbsentBoards_AreNull: the best play's
        // board always exists now; the user's is null exactly when the user's
        // play is not among the candidates.
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: null));

        Assert.Null(play.AfterPlayerBoard);
        Assert.NotEqual(BoardPosition.Empty, play.AfterBoardOfBest(PlayRanking.Equity));
    }

    [Fact]
    public void CheckerPlayDecision_AfterBoards_AreNotOnTheWire()
    {
        // Rewritten from BgDecisionData_RoundTrip_Outcome: no stored copy of a
        // derivable value — neither board nor an Outcome is written.
        var json = JsonSerializer.Serialize<BgDecisionData>(TestRecords.CheckerPlay(), Options);

        Assert.DoesNotContain("AfterBestBoard", json);
        Assert.DoesNotContain("AfterPlayerBoard", json);
        Assert.DoesNotContain("Outcome", json);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_AfterBoards_ForwardTheDerivation()
    {
        // Rewritten from ..._AfterBoards_ForwardFromOutcome.
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 1));

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            var data = play.ViewFor(ranking);
            Assert.Equal(play.AfterBoardOfBest(ranking), data.AfterBestBoard);
            Assert.Equal(play.AfterPlayerBoard, data.AfterPlayerBoard);
            Assert.NotNull(data.AfterPlayerBoard);
        }
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_AfterBoards_Null_CubeDecision()
    {
        // Rewritten: a cube decision has no after-board members at all, and
        // reads null through the filter interface.
        IDecisionFilterData data = TestRecords.Cube().ViewFor(PlayRanking.Equity);

        Assert.Equal(DecisionKind.Cube, data.Kind);
        Assert.Null(data.AfterBestBoard);
        Assert.Null(data.AfterPlayerBoard);
        Assert.Null(typeof(CubeDecision).GetMember("AfterBestBoard").SingleOrDefault());
    }

    // -----------------------------------------------------------------------
    //  Game, MoveNumber and IsStandardStart — derived from the Id
    //  (halheinrich/backgammon#124) and DescriptiveData
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_GameAndMoveNumber_MatchPosition_KeepTheIdsNumbers_BothPaths()
    {
        // Rewritten from DescriptiveData_Game_RoundTrip and
        // DescriptiveData_MoveNumber_RoundTrip: the numbers are the Id's, and
        // round-trip with it.
        var original = TestRecords.CheckerPlay(id: new XgDecisionId("match.xg", Game: 4, MoveNumber: 17, IsCube: false));

        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = RoundTrip(original, options);
            Assert.Equal(4, restored.Game);
            Assert.Equal(17, restored.MoveNumber);
        }
    }

    [Fact]
    public void BgDecisionData_GameAndMoveNumber_StandalonePosition_AreNone_BothPaths()
    {
        // Rewritten from DescriptiveData_Game_AbsentIsRefused and
        // DescriptiveData_MoveNumber_AbsentIsRefused. A standalone position
        // belongs to no game: no game or move number, never a stamped 1
        // (halheinrich/backgammon#124) — and there is nowhere to stamp one.
        var original = TestRecords.CheckerPlay(id: new XgpDecisionId("position.xgp"));
        Assert.Null(original.Game);
        Assert.Null(original.MoveNumber);

        foreach (var (_, options) in WirePaths.Both)
        {
            var json = JsonSerializer.Serialize<BgDecisionData>(original, options);
            var restored = JsonSerializer.Deserialize<BgDecisionData>(json, options)!;
            Assert.Null(restored.Game);
            Assert.Null(restored.MoveNumber);
            Assert.DoesNotContain("\"Game\"", json);
            Assert.DoesNotContain("\"MoveNumber\"", json);
        }
    }

    [Fact]
    public void DescriptiveData_StoresNoGameOrMoveNumber()
    {
        // The stored copies are gone: the one stored place is the Id.
        Assert.Null(typeof(DescriptiveData).GetProperty("Game"));
        Assert.Null(typeof(DescriptiveData).GetProperty("MoveNumber"));
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
    public void DescriptiveData_IsStandardStart_AbsentReadsAsNone()
    {
        // Rewritten from DescriptiveData_IsStandardStart_AbsentIsRefused: the
        // member is nullable now — none for a standalone position
        // (halheinrich/backgammon#124) — so its absence reads as null; a
        // record holds the null to its Id.
        var d = ReadWithout(TestRecords.Descriptive(isStandardStart: true), "IsStandardStart");
        Assert.Null(d.IsStandardStart);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MoveNumberAndIsStandardStart()
    {
        IDecisionFilterData data = TestRecords.CheckerPlay(
            id: new XgDecisionId("match.xg", Game: 1, MoveNumber: 12, IsCube: false),
            descriptive: TestRecords.Descriptive(
                onRollName: "Hal",
                isStandardStart: true)).ViewFor(PlayRanking.Equity);

        Assert.Equal(12, data.MoveNumber);
        Assert.True(data.IsStandardStart);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MoveNumber_StandalonePosition_IsNone()
    {
        // Rewritten from ..._MoveNumberAndIsStandardStart_ForwardZeroAndFalse:
        // a standalone position has no move number and no start for the view
        // to forward (halheinrich/backgammon#124).
        IDecisionFilterData data = TestRecords.CheckerPlay(id: new XgpDecisionId("test.xgp")).ViewFor(PlayRanking.Equity);

        Assert.Null(data.MoveNumber);
        Assert.Null(data.IsStandardStart);
    }

    // -----------------------------------------------------------------------
    //  Id — persistent decision identifier
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_Id_RoundTrip_Xgp()
    {
        var original = TestRecords.CheckerPlay(id: new XgpDecisionId("match.xgp"));
        var json = JsonSerializer.Serialize<BgDecisionData>(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        Assert.Equal(new XgpDecisionId("match.xgp"), restored.Id);
        Assert.Contains("\"Id\":\"match.xgp\"", json);
    }

    [Fact]
    public void BgDecisionData_Id_RoundTrip_Xg()
    {
        var original = TestRecords.Cube(
            id: new XgDecisionId("match.xg", Game: 4, MoveNumber: 22, IsCube: true));
        var json = JsonSerializer.Serialize<BgDecisionData>(original, Options);
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
        // each at its own level of the record — the cubeless equities on the
        // cube decision's category, their one home.
        AssertAbsentIsRefused<BgDecisionData>(TestRecords.Cube(xgid: "XGID=x"), "Xgid");
        AssertAbsentIsRefused(TestRecords.Descriptive(comment: "note", flagged: true), "Flagged");
        // Rewritten: the comment's absence is "none recorded" now — null, its
        // one spelling — not a refusal.
        Assert.Null(ReadWithout(TestRecords.Descriptive(comment: "note"), "Comment").Comment);
        AssertAbsentIsRefused(
            TestRecords.CubeData(cubelessNoDoubleEquity: 0.2, cubelessDoubleTakeEquity: 0.3),
            "CubelessNoDoubleEquity", "CubelessDoubleTakeEquity");
    }

    [Fact]
    public void BgDecisionData_NewDecisionFields_RoundTrip()
    {
        var original = TestRecords.Cube(
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10",
            decision: TestRecords.CubeData(
                noDoubleEquity: 0.312,
                doubleTakeEquity: 0.287,
                cubelessNoDoubleEquity: 0.205,
                cubelessDoubleTakeEquity: 0.198),
            descriptive: TestRecords.Descriptive(
                onRollName: "Hal",
                opponentName: "Bot",
                comment: "Tricky double — too good?",
                flagged: true));

        var json = JsonSerializer.Serialize<BgDecisionData>(original, Options);
        var restored = Assert.IsType<CubeDecision>(JsonSerializer.Deserialize<BgDecisionData>(json, Options));

        // Xgid serializes at the top level, not inside Position.
        Assert.Contains("\"Xgid\":\"XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10\"", json);
        Assert.Equal(original.Xgid, restored.Xgid);

        Assert.Equal(original.Descriptive.Comment, restored.Descriptive.Comment);
        Assert.True(restored.Descriptive.Flagged);

        Assert.Equal(original.Decision.CubelessNoDoubleEquity, restored.Decision.CubelessNoDoubleEquity);
        Assert.Equal(original.Decision.CubelessDoubleTakeEquity, restored.Decision.CubelessDoubleTakeEquity);
    }

    // -----------------------------------------------------------------------
    //  Wire shape — the kind first, then the stored members; the
    //  IDecisionFilterData view is excluded (halheinrich/backgammon#14)
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_TopLevelJson_IsExactlyTheWireMembers_KindFirst()
    {
        // Rewritten from BgDecisionData_TopLevelJson_IsExactlyTheSixWireMembers.
        // The filter view forwards into the stored members, so serializing it
        // would write top-level duplicates with no read-back path. This pins
        // the top-level wire shape of each kind as exactly its stored members,
        // the kind first — a forwarding member added without [JsonIgnore]
        // fails here, and so does the retired Outcome.
        foreach (BgDecisionData record in new BgDecisionData[] { TestRecords.CheckerPlay(), TestRecords.Cube() })
        {
            foreach (var (_, options) in WirePaths.Both)
            {
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(record, options));
                var topLevelNames = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

                Assert.Equal(
                    ["Kind", "Id", "Xgid", "Position", "Descriptive", "Decision"],
                    topLevelNames);
            }
        }
    }
}
