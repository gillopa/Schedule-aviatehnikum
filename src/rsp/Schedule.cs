using System.Diagnostics.CodeAnalysis;

namespace Schedule;

public class Schedule
{

    public required DateOnly Date { get; init; }
    public required string Group { get; init; }
    public required string Url { get; init; }
    [SetsRequiredMembers]
    public Schedule(DateOnly dateOnly, string group, string url)
    {
        Date = dateOnly;
        Group = group;
        Url = url;
    }
}
