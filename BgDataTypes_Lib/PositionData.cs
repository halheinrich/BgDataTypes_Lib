namespace BgDataTypes_Lib;

public class PositionData
{
    /// <summary>
    /// Men on Point — 26-element board array.
    /// [0]    = opponent's bar  (value always &lt;= 0)
    /// [1-24] = points 1-24 from on-roll player's perspective
    /// [25]   = on-roll player's bar (value always &gt;= 0)
    /// Positive = on-roll player's checkers; negative = opponent's.
    /// </summary>
    public IReadOnlyList<int> Mop { get; init; } = new int[26];

    public int OnRollNeeds { get; init; }
    public int OpponentNeeds { get; init; }

    public int OnRollPipCount { get; init; }
    public int OpponentPipCount { get; init; }

    public int CubeSize { get; init; } = 1;
    public CubeOwner CubeOwner { get; init; }
    public bool IsCrawford { get; init; }
}