namespace LabeledByAI.Services;

public record GetBestLabelRequestIssue(
    string Owner,
    string Repo,
    int Number);
