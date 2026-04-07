namespace BgDataTypes_Lib;

public class DescriptiveData
{
    public int MatchLength { get; init; }
    public string OnRollName { get; init; } = string.Empty;
    public string OpponentName { get; init; } = string.Empty;
    public string? Title { get; init; }
    public DateOnly? Date { get; init; }
    public string? Event { get; init; }
}