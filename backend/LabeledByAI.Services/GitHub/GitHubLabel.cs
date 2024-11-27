namespace LabeledByAI.Services;

public record GitHubLabel(
    string Id,
    string Name,
    string Description,
    int TotalIssues);
