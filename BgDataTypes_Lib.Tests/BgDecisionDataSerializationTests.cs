using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class BgDecisionDataSerializationTests
{
    // No explicit JsonStringEnumConverter registration: CubeOwner bundles its
    // own [JsonConverter(typeof(JsonStringEnumConverter))] attribute. The test
    // relies on the attribute alone so that removing it from the type would
    // fail this suite loudly (rather than silently passing because an
    // option-level registration covered for it).
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
    };

    // -----------------------------------------------------------------------
    //  Leaf types
    // -----------------------------------------------------------------------

    [Fact]
    public void PlayCandidate_EquityLoss_DefaultsToZero()
    {
        // Best plays carry EquityLoss = 0.0; the test for "is this a best play"
        // is EquityLoss == 0.0 (or membership-by-equity equivalence). Identifying
        // a canonical best uses DecisionData.BestPlayIndex.
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            Equity = -0.142
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.MoveNotation, restored.MoveNotation);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(0.0, restored.EquityLoss);
    }

    [Fact]
    public void PlayCandidate_RoundTrip_PopulatedEquityLoss()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "13/8 13/11",
            Equity = -0.187,
            EquityLoss = 0.045
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.EquityLoss, restored.EquityLoss);
    }

    [Fact]
    public void PlayCandidate_RoundTrip_Probabilities_AllPopulated()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            Equity = -0.142,
            WinPct = 0.481,
            WinGammonPct = 0.112,
            WinBgPct = 0.004,
            LosePct = 0.519,
            LoseGammonPct = 0.143,
            LoseBgPct = 0.006
        };
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
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            Equity = -0.142
        };
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
        var original = new PlayCandidate
        {
            MoveNotation = "13/8 13/11",
            Equity = -0.187,
            WinPct = 0.476,
            LosePct = 0.524
            // gammon/bg fields left null — partial evaluation
        };
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
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            Depth = "Rollout: 1296 trials. 3-ply",
            Equity = -0.142
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.Depth, restored.Depth);
        Assert.Equal(original.MoveNotation, restored.MoveNotation);
        Assert.Equal(original.Equity, restored.Equity);
    }

    [Fact]
    public void PlayCandidate_Depth_DefaultsToEmpty()
    {
        var p = new PlayCandidate { MoveNotation = "8/5 6/1" };
        Assert.Equal(string.Empty, p.Depth);
    }

    [Fact]
    public void PlayCandidate_DepthAbbreviation_RoundTrip()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            DepthAbbreviation = "3p1296",
            Equity = -0.142
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.DepthAbbreviation, restored.DepthAbbreviation);
        Assert.Equal(original.MoveNotation, restored.MoveNotation);
        Assert.Equal(original.Equity, restored.Equity);
    }

    [Fact]
    public void PlayCandidate_DepthAbbreviation_DefaultsToEmpty()
    {
        var p = new PlayCandidate { MoveNotation = "8/5 6/1" };
        Assert.Equal(string.Empty, p.DepthAbbreviation);
    }

    [Fact]
    public void PlayCandidate_DepthRank_RoundTrip()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            DepthRank = 7,
            Equity = -0.142
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(7, restored.DepthRank);
        Assert.Equal(original.MoveNotation, restored.MoveNotation);
    }

    [Fact]
    public void PlayCandidate_DepthRank_DefaultsToZero()
    {
        var p = new PlayCandidate { MoveNotation = "8/5 6/1" };
        Assert.Equal(0, p.DepthRank);
    }

    [Fact]
    public void PlayCandidate_DepthClass_RoundTrip()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            DepthClass = AnalysisDepthClass.XgRollerPlusPlus,
            Equity = -0.142
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        // String form via the enum's bundled converter — no options-level registration.
        Assert.Contains("\"DepthClass\":\"XgRollerPlusPlus\"", json);
        Assert.Equal(AnalysisDepthClass.XgRollerPlusPlus, restored.DepthClass);
    }

    [Fact]
    public void PlayCandidate_DepthClass_DefaultsToUnknown()
    {
        var p = new PlayCandidate { MoveNotation = "8/5 6/1" };
        Assert.Equal(AnalysisDepthClass.Unknown, p.DepthClass);
    }

    [Fact]
    public void PlayCandidate_DepthClass_MissingInLegacyJson_DeserializesToUnknown()
    {
        // JSON written before DepthClass existed carries no such field.
        var json = "{\"MoveNotation\":\"8/5 6/1\",\"Depth\":\"3-ply\",\"Equity\":-0.12}";
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(AnalysisDepthClass.Unknown, restored.DepthClass);
        Assert.Equal("3-ply", restored.Depth);
    }

    [Fact]
    public void PlayCandidate_Play_DefaultsToEmpty()
    {
        var p = new PlayCandidate { MoveNotation = "8/5 6/1" };
        Assert.Equal(0, p.Play.Count);
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_Empty()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "8/5 6/1",
            Equity = -0.142
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(0, restored.Play.Count);
        Assert.Contains("\"Play\":[]", json);
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_Populated()
    {
        var play = new Play();
        play.Add(new Move(13, 7));
        play.Add(new Move(8, 5));

        var original = new PlayCandidate
        {
            MoveNotation = "13/7 8/5",
            Equity = -0.118,
            Play = play
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(2, restored.Play.Count);
        Assert.Equal(new Move(13, 7), restored.Play[0]);
        Assert.Equal(new Move(8, 5), restored.Play[1]);
        Assert.True(restored.Play.Equals(original.Play));
    }

    [Fact]
    public void PlayCandidate_Play_RoundTrip_PreservesHitEncoding()
    {
        var play = new Play();
        play.Add(new Move(13, -7));   // hit on the 7-point

        var original = new PlayCandidate
        {
            MoveNotation = "13/7*",
            Equity = 0.05,
            Play = play
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(1, restored.Play.Count);
        Assert.Equal(13, restored.Play[0].FrPt);
        Assert.Equal(-7, restored.Play[0].ToPt);
    }

    [Fact]
    public void PlayCandidate_Play_NestedInBgDecisionData_RoundTrip()
    {
        var play = new Play();
        play.Add(new Move(24, 18));
        play.Add(new Move(13, 9));

        var original = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData
            {
                Dice = [6, 4],
                Plays =
                [
                    new PlayCandidate
                    {
                        MoveNotation = "24/18 13/9",
                        Equity = 0.211,
                        Play = play
                    }
                ],
                IsCube = false
            }
        };

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
        var original = new DecisionData
        {
            Dice = [0, 0],
            IsCube = true,
            CubeDepth = "Rollout: 1296 trials. 3-ply",
            NoDoubleEquity = 0.312,
            DoubleTakeEquity = 0.287
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal("Rollout: 1296 trials. 3-ply", restored.CubeDepth);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeDepth_DefaultsToEmpty()
    {
        var d = new DecisionData();
        Assert.Equal(string.Empty, d.CubeDepth);
    }

    [Fact]
    public void DecisionData_CubeDepthAbbreviation_RoundTrip()
    {
        var original = new DecisionData
        {
            Dice = [0, 0],
            IsCube = true,
            CubeDepthAbbreviation = "3p1296",
            NoDoubleEquity = 0.312,
            DoubleTakeEquity = 0.287
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal("3p1296", restored.CubeDepthAbbreviation);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeDepthAbbreviation_DefaultsToEmpty()
    {
        var d = new DecisionData();
        Assert.Equal(string.Empty, d.CubeDepthAbbreviation);
    }

    [Fact]
    public void DecisionData_CubeDepthRank_RoundTrip()
    {
        var original = new DecisionData
        {
            Dice = [0, 0],
            IsCube = true,
            CubeDepthRank = 7,
            NoDoubleEquity = 0.312
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(7, restored.CubeDepthRank);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeDepthRank_DefaultsToZero()
    {
        var d = new DecisionData();
        Assert.Equal(0, d.CubeDepthRank);
    }

    [Fact]
    public void DecisionData_CubeDepthClass_RoundTrip()
    {
        var original = new DecisionData
        {
            Dice = [0, 0],
            IsCube = true,
            CubeDepthClass = AnalysisDepthClass.Rollout,
            NoDoubleEquity = 0.312,
            DoubleTakeEquity = 0.287
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Contains("\"CubeDepthClass\":\"Rollout\"", json);
        Assert.Equal(AnalysisDepthClass.Rollout, restored.CubeDepthClass);
        Assert.True(restored.IsCube);
    }

    [Fact]
    public void DecisionData_CubeDepthClass_DefaultsToUnknown()
    {
        var d = new DecisionData();
        Assert.Equal(AnalysisDepthClass.Unknown, d.CubeDepthClass);
    }

    [Fact]
    public void DecisionData_CubeDepthClass_MissingInLegacyJson_DeserializesToUnknown()
    {
        var json = "{\"Dice\":[0,0],\"IsCube\":true,\"CubeDepth\":\"3-ply\",\"NoDoubleEquity\":0.312}";
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(AnalysisDepthClass.Unknown, restored.CubeDepthClass);
        Assert.Equal("3-ply", restored.CubeDepth);
    }

    [Fact]
    public void CubeOwner_Serializes_AsString()
    {
        var data = new PositionData { CubeOwner = CubeOwner.Opponent };
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

        var original = new PositionData
        {
            Mop = mop,
            OnRollNeeds = 3,
            OpponentNeeds = 5,
            CubeSize = 2,
            CubeOwner = CubeOwner.OnRoll,
            IsCrawford = false
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PositionData>(json, Options)!;

        Assert.Equal(original.Mop, restored.Mop);
        Assert.Equal(original.OnRollNeeds, restored.OnRollNeeds);
        Assert.Equal(original.OpponentNeeds, restored.OpponentNeeds);
        Assert.Equal(original.CubeSize, restored.CubeSize);
        Assert.Equal(original.CubeOwner, restored.CubeOwner);
        Assert.Equal(original.IsCrawford, restored.IsCrawford);
    }

    // -----------------------------------------------------------------------
    //  DecisionData
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionData_RoundTrip_PlayDecision()
    {
        var original = new DecisionData
        {
            Dice = [3, 5],
            Plays =
            [
                new PlayCandidate { MoveNotation = "8/5 6/1", Depth = "3-ply", Equity = -0.120 },
                new PlayCandidate { MoveNotation = "8/3 6/1", Depth = "3-ply", Equity = -0.165, EquityLoss = 0.045 }
            ],
            IsCube = false
        };

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
        var original = new DecisionData
        {
            Dice = [0, 0],
            IsCube = true,
            NoDoubleEquity = 0.312,
            DoubleTakeEquity = 0.287,
            WinPctAfterNoDouble = 0.621,
            GammonPctAfterNoDouble = 0.183,
            BgPctAfterNoDouble = 0.012,
            LosePctAfterNoDouble = 0.379,
            LoseGammonPctAfterNoDouble = 0.091,
            LoseBgPctAfterNoDouble = 0.003,
            WinPctAfterDoubleTake = 0.618,
            GammonPctAfterDoubleTake = 0.181,
            BgPctAfterDoubleTake = 0.011,
            LosePctAfterDoubleTake = 0.382,
            LoseGammonPctAfterDoubleTake = 0.093,
            LoseBgPctAfterDoubleTake = 0.004,
            ProbOfOpponentErrorJustifyingDouble = 0.078
        };

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
        var original = new DescriptiveData
        {
            MatchLength = 11,
            OnRollName = "Mochy",
            OpponentName = "Falafel",
            Title = null,
            Date = null,
            Event = null,
            SourceFile = null
        };

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
        var original = new DescriptiveData
        {
            MatchLength = 7,
            OnRollName = "Player A",
            OpponentName = "Player B",
            Date = new DateOnly(2024, 11, 15),
            Event = "Monte Carlo 2024"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;

        Assert.Equal(original.Date, restored.Date);
        Assert.Equal(original.Event, restored.Event);
    }

    [Fact]
    public void DescriptiveData_RoundTrip_WithSourceFile()
    {
        var original = new DescriptiveData
        {
            MatchLength = 7,
            OnRollName = "Mochy",
            OpponentName = "Falafel",
            SourceFile = "mochy-falafel.xg"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;

        Assert.Equal("mochy-falafel.xg", restored.SourceFile);
    }

    [Fact]
    public void DescriptiveData_SourceFile_DefaultsToNull()
    {
        var d = new DescriptiveData { OnRollName = "A", OpponentName = "B" };
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

        var original = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = new PositionData
            {
                Mop = mop,
                OnRollNeeds = 2,
                OpponentNeeds = 4,
                CubeSize = 4,
                CubeOwner = CubeOwner.Centered,
                IsCrawford = false
            },
            Decision = new DecisionData
            {
                Dice = [6, 4],
                Plays =
                [
                    new PlayCandidate { MoveNotation = "24/18 24/20", Depth = "3-ply", Equity = 0.211 },
                    new PlayCandidate { MoveNotation = "24/18 13/9",  Depth = "3-ply", Equity = 0.198, EquityLoss = 0.013 }
                ],
                IsCube = false
            },
            Descriptive = new DescriptiveData
            {
                MatchLength = 5,
                OnRollName = "Hal",
                OpponentName = "Bot",
                Title = "Opening Run",
                Date = new DateOnly(2025, 3, 1),
                Event = "Test Match",
                SourceFile = "hal-bot.xg"
            }
        };

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
        var original = new DecisionData
        {
            Dice = [3, 5],
            Plays =
            [
                new PlayCandidate { MoveNotation = "8/5 6/1", Equity = -0.120 },
                new PlayCandidate { MoveNotation = "8/3 6/1", Equity = -0.165, EquityLoss = 0.045 }
            ],
            IsCube = false,
            UserPlayIndex = 1,
            UserPlayError = 0.045
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(0.045, restored.UserPlayError);
        Assert.Null(restored.UserDoubleError);
        Assert.Null(restored.UserTakeError);
    }

    [Fact]
    public void DecisionData_RoundTrip_CubeDecision_WithUserErrors()
    {
        var original = new DecisionData
        {
            Dice = [0, 0],
            IsCube = true,
            NoDoubleEquity = 0.312,
            DoubleTakeEquity = 0.287,
            UserDoubleError = 0.025,
            UserTakeError = 0.011
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(0.025, restored.UserDoubleError);
        Assert.Equal(0.011, restored.UserTakeError);
        Assert.Null(restored.UserPlayError);
    }

    [Fact]
    public void DecisionData_UserErrors_DefaultToNull()
    {
        var original = new DecisionData
        {
            Dice = [6, 4],
            IsCube = false
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Null(restored.UserPlayError);
        Assert.Null(restored.UserDoubleError);
        Assert.Null(restored.UserTakeError);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — BgDecisionData
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CheckerPlay()
    {
        var mop = new int[26];
        mop[1] = 2; mop[6] = -5;

        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = new PositionData
            {
                Mop = mop,
                OnRollNeeds = 3,
                OpponentNeeds = 5,
                IsCrawford = false
            },
            Decision = new DecisionData
            {
                IsCube = false,
                UserPlayError = 0.034
            },
            Descriptive = new DescriptiveData { OnRollName = "Hal" }
        };

        Assert.Equal("Hal", data.Player);
        Assert.False(data.IsCube);
        Assert.Equal(3, data.OnRollNeeds);
        Assert.Equal(5, data.OpponentNeeds);
        Assert.False(data.IsCrawford);
        Assert.Equal(0.034, data.FilterError);
        Assert.Equal(mop, data.Board);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CubePlay_UserDoubleError()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData
            {
                IsCube = true,
                UserDoubleError = 0.025,
                UserTakeError = 0.011
            }
        };

        Assert.True(data.IsCube);
        Assert.Equal(0.025, data.FilterError);  // UserDoubleError takes precedence
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_CubePlay_UserTakeError()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData
            {
                IsCube = true,
                UserDoubleError = null,
                UserTakeError = 0.011
            }
        };

        Assert.Equal(0.011, data.FilterError);  // Falls through to UserTakeError
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_Board_MatchesMop()
    {
        var mop = new int[26];
        mop[6] = -5; mop[13] = 5;

        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = new PositionData { Mop = mop }
        };

        Assert.Equal(mop, data.Board);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MatchLength()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Descriptive = new DescriptiveData { MatchLength = 11 }
        };

        Assert.Equal(11, data.MatchLength);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — AnalysisDepthClass derivation
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_AnalysisDepthClass_CubeDecision_UsesCubeDepthClass()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData
            {
                IsCube = true,
                CubeDepthClass = AnalysisDepthClass.XgRollerPlus
            }
        };

        Assert.Equal(AnalysisDepthClass.XgRollerPlus, data.AnalysisDepthClass);
    }

    [Fact]
    public void BgDecisionData_AnalysisDepthClass_CheckerPlay_UsesBestPlayCandidate()
    {
        // BestPlayIndex deliberately not 0, to pin that derivation indexes by
        // it rather than taking the first candidate.
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData
            {
                IsCube = false,
                BestPlayIndex = 1,
                Plays =
                [
                    new PlayCandidate { MoveNotation = "8/5 6/1", DepthClass = AnalysisDepthClass.Ply3 },
                    new PlayCandidate { MoveNotation = "8/3 6/1", DepthClass = AnalysisDepthClass.Rollout }
                ]
            }
        };

        Assert.Equal(AnalysisDepthClass.Rollout, data.AnalysisDepthClass);
    }

    [Fact]
    public void BgDecisionData_AnalysisDepthClass_CheckerPlay_OutOfRangeBestPlayIndex_ReturnsUnknown()
    {
        // Malformed or legacy data can carry a BestPlayIndex that doesn't
        // identify a candidate; the derivation degrades to Unknown rather
        // than throwing from a property getter that runs on every filter
        // pass and serialization.
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData
            {
                IsCube = false,
                BestPlayIndex = 2,
                Plays = [new PlayCandidate { MoveNotation = "8/5 6/1", DepthClass = AnalysisDepthClass.Ply3 }]
            }
        };

        Assert.Equal(AnalysisDepthClass.Unknown, data.AnalysisDepthClass);
    }

    [Fact]
    public void BgDecisionData_AnalysisDepthClass_CheckerPlay_EmptyPlays_ReturnsUnknown()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData { IsCube = false }
        };

        Assert.Equal(AnalysisDepthClass.Unknown, data.AnalysisDepthClass);
    }

    [Fact]
    public void BgDecisionData_AnalysisDepthClass_DefaultsToUnknown()
    {
        IDecisionFilterData data = new BgDecisionData { Id = new XgpDecisionId("test.xgp") };

        Assert.Equal(AnalysisDepthClass.Unknown, data.AnalysisDepthClass);
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

        var original = new PlayOutcomeData
        {
            AfterBestBoard = best,
            AfterPlayerBoard = player
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayOutcomeData>(json, Options)!;

        Assert.Equal(original.AfterBestBoard, restored.AfterBestBoard);
        Assert.Equal(original.AfterPlayerBoard, restored.AfterPlayerBoard);
    }

    [Fact]
    public void PlayOutcomeData_DefaultsToEmptyBoards()
    {
        var data = new PlayOutcomeData();
        Assert.Empty(data.AfterBestBoard);
        Assert.Empty(data.AfterPlayerBoard);
    }

    [Fact]
    public void BgDecisionData_RoundTrip_Outcome()
    {
        var best = new int[26];
        best[4] = 2; best[6] = -5; best[20] = -2;
        var player = new int[26];
        player[5] = 2; player[6] = -5; player[20] = -2;

        var original = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData { IsCube = false, UserPlayError = 0.018 },
            Outcome = new PlayOutcomeData
            {
                AfterBestBoard = best,
                AfterPlayerBoard = player
            }
        };

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

        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData { IsCube = false },
            Outcome = new PlayOutcomeData
            {
                AfterBestBoard = best,
                AfterPlayerBoard = player
            }
        };

        Assert.Equal(best, data.AfterBestBoard);
        Assert.Equal(player, data.AfterPlayerBoard);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_AfterBoards_EmptyByDefault_CubeDecision()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = new DecisionData { IsCube = true, UserDoubleError = 0.025 }
        };

        Assert.True(data.IsCube);
        Assert.Empty(data.AfterBestBoard);
        Assert.Empty(data.AfterPlayerBoard);
    }

    // -----------------------------------------------------------------------
    //  Game, MoveNumber and IsStandardStart — DescriptiveData and BgDecisionData
    // -----------------------------------------------------------------------

    [Fact]
    public void DescriptiveData_Game_RoundTrip()
    {
        var original = new DescriptiveData
        {
            OnRollName = "Mochy",
            OpponentName = "Falafel",
            Game = 4
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;
        Assert.Equal(4, restored.Game);
    }

    [Fact]
    public void DescriptiveData_Game_DefaultsToZero()
    {
        var d = new DescriptiveData();
        Assert.Equal(0, d.Game);
    }

    [Fact]
    public void DescriptiveData_MoveNumber_RoundTrip()
    {
        var original = new DescriptiveData
        {
            OnRollName = "Mochy",
            OpponentName = "Falafel",
            MoveNumber = 17
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;
        Assert.Equal(17, restored.MoveNumber);
    }

    [Fact]
    public void DescriptiveData_MoveNumber_DefaultsToZero()
    {
        var d = new DescriptiveData();
        Assert.Equal(0, d.MoveNumber);
    }

    [Fact]
    public void DescriptiveData_IsStandardStart_RoundTrip()
    {
        var original = new DescriptiveData
        {
            OnRollName = "Mochy",
            OpponentName = "Falafel",
            IsStandardStart = true
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;
        Assert.True(restored.IsStandardStart);
    }

    [Fact]
    public void DescriptiveData_IsStandardStart_DefaultsToFalse()
    {
        var d = new DescriptiveData();
        Assert.False(d.IsStandardStart);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MoveNumberAndIsStandardStart()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Descriptive = new DescriptiveData
            {
                OnRollName = "Hal",
                MoveNumber = 12,
                IsStandardStart = true
            }
        };

        Assert.Equal(12, data.MoveNumber);
        Assert.True(data.IsStandardStart);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MoveNumberAndIsStandardStart_Defaults()
    {
        IDecisionFilterData data = new BgDecisionData { Id = new XgpDecisionId("test.xgp") };

        Assert.Equal(0, data.MoveNumber);
        Assert.False(data.IsStandardStart);
    }

    // -----------------------------------------------------------------------
    //  Id — persistent decision identifier
    // -----------------------------------------------------------------------

    [Fact]
    public void BgDecisionData_Id_RoundTrip_Xgp()
    {
        var original = new BgDecisionData { Id = new XgpDecisionId("match.xgp") };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json, Options)!;

        Assert.Equal(new XgpDecisionId("match.xgp"), restored.Id);
        Assert.Contains("\"Id\":\"match.xgp\"", json);
    }

    [Fact]
    public void BgDecisionData_Id_RoundTrip_Xg()
    {
        var original = new BgDecisionData
        {
            Id = new XgDecisionId("match.xg", Game: 4, MoveNumber: 22, IsCube: true)
        };
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
    public void BgDecisionData_NewDecisionFields_DefaultToEmptyAndZero()
    {
        var data = new BgDecisionData { Id = new XgpDecisionId("test.xgp") };

        Assert.Equal(string.Empty, data.Xgid);
        Assert.Equal(string.Empty, data.Descriptive.Comment);
        Assert.False(data.Descriptive.Flagged);
        Assert.Equal(0.0, data.Decision.CubelessNoDoubleEquity);
        Assert.Equal(0.0, data.Decision.CubelessDoubleTakeEquity);
    }

    [Fact]
    public void BgDecisionData_NewDecisionFields_RoundTrip()
    {
        var original = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10",
            Decision = new DecisionData
            {
                Dice = [0, 0],
                IsCube = true,
                NoDoubleEquity = 0.312,
                DoubleTakeEquity = 0.287,
                CubelessNoDoubleEquity = 0.205,
                CubelessDoubleTakeEquity = 0.198
            },
            Descriptive = new DescriptiveData
            {
                OnRollName = "Hal",
                OpponentName = "Bot",
                Comment = "Tricky double — too good?",
                Flagged = true
            }
        };

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
}