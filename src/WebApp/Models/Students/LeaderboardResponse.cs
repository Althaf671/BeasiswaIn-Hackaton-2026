namespace BeasiswaIn.Web.Models.Students;

public sealed record LeaderboardResponse
{
    public IReadOnlyList<LeaderboardStudentResponse> Students { get; init; } = [];
    public LeaderboardStudentResponse? CurrentStudent { get; init; }
    public int SubmissionsToTopThree { get; init; }
}

public sealed record LeaderboardStudentResponse
{
    public int Rank { get; init; }
    public string FullName { get; init; } = string.Empty;
    public int Points { get; init; }
    public int SubmissionCount { get; init; }
    public bool IsYou { get; init; }
}
