using System.Text.Json;
using System.Text.Json.Serialization;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class BgDecisionDataSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    // -----------------------------------------------------------------------
    //  Leaf types
    // -----------------------------------------------------------------------

    [Fact]
    public void PlayCandidate_RoundTrip_NullEquityLoss()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "8/5(2) 6/3(2)",
            Equity = -0.142,
            EquityLoss = null,
            IsUserPlay = false
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.MoveNotation, restored.MoveNotation);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Null(restored.EquityLoss);
        Assert.Equal(original.IsUserPlay, restored.IsUserPlay);
    }

    [Fact]
    public void PlayCandidate_RoundTrip_PopulatedEquityLoss()
    {
        var original = new PlayCandidate
        {
            MoveNotation = "13/8 13/11",
            Equity = -0.187,
            EquityLoss = 0.045,
            IsUserPlay = true
        };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<PlayCandidate>(json, Options)!;

        Assert.Equal(original.EquityLoss, restored.EquityLoss);
        Assert.True(restored.IsUserPlay);
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
                new PlayCandidate { MoveNotation = "8/5 6/1", Depth = "3-ply", Equity = -0.120, IsUserPlay = true },
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
                    new PlayCandidate { MoveNotation = "24/18 24/20", Depth = "3-ply", Equity = 0.211, IsUserPlay = false },
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
                new PlayCandidate { MoveNotation = "8/5 6/1", Equity = -0.120, IsUserPlay = false },
                new PlayCandidate { MoveNotation = "8/3 6/1", Equity = -0.165, EquityLoss = 0.045, IsUserPlay = true }
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
            Position = new PositionData { Mop = mop }
        };

        Assert.Equal(mop, data.Board);
    }

    [Fact]
    public void BgDecisionData_IDecisionFilterData_MatchLength()
    {
        IDecisionFilterData data = new BgDecisionData
        {
            Descriptive = new DescriptiveData { MatchLength = 11 }
        };

        Assert.Equal(11, data.MatchLength);
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
            Decision = new DecisionData { IsCube = true, UserDoubleError = 0.025 }
        };

        Assert.True(data.IsCube);
        Assert.Empty(data.AfterBestBoard);
        Assert.Empty(data.AfterPlayerBoard);
    }
}