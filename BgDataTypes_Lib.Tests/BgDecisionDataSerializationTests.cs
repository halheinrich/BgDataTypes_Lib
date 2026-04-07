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
    public void AnalysisDepthEntry_RoundTrip()
    {
        var original = new AnalysisDepthEntry { Label = "Rollout: 1296 trials. 3-ply" };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<AnalysisDepthEntry>(json, Options)!;

        Assert.Equal(original.Label, restored.Label);
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
                new PlayCandidate { MoveNotation = "8/5 6/1", Equity = -0.120, IsUserPlay = true },
                new PlayCandidate { MoveNotation = "8/3 6/1", Equity = -0.165, EquityLoss = 0.045 }
            ],
            AnalysisDepths = [new AnalysisDepthEntry { Label = "3-ply" }],
            IsCube = false
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionData>(json, Options)!;

        Assert.Equal(original.Dice, restored.Dice);
        Assert.Equal(2, restored.Plays.Count);
        Assert.Equal("8/5 6/1", restored.Plays[0].MoveNotation);
        Assert.Equal(0.045, restored.Plays[1].EquityLoss);
        Assert.Single(restored.AnalysisDepths);
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
            Event = null
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DescriptiveData>(json, Options)!;

        Assert.Equal(original.MatchLength, restored.MatchLength);
        Assert.Equal(original.OnRollName, restored.OnRollName);
        Assert.Equal(original.OpponentName, restored.OpponentName);
        Assert.Null(restored.Title);
        Assert.Null(restored.Date);
        Assert.Null(restored.Event);
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
                    new PlayCandidate { MoveNotation = "24/18 24/20", Equity = 0.211, IsUserPlay = false },
                    new PlayCandidate { MoveNotation = "24/18 13/9",  Equity = 0.198, EquityLoss = 0.013 }
                ],
                AnalysisDepths = [new AnalysisDepthEntry { Label = "3-ply" }],
                IsCube = false
            },
            Descriptive = new DescriptiveData
            {
                MatchLength = 5,
                OnRollName = "Hal",
                OpponentName = "Bot",
                Title = "Opening Run",
                Date = new DateOnly(2025, 3, 1),
                Event = "Test Match"
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
    }
}