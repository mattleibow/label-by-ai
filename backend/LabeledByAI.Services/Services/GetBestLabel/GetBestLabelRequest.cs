namespace LabeledByAI.Services;

public record GetBestLabelRequest(
    int Version,
    GetBestLabelRequestIssue Issue,
    GetBestLabelRequestLabels Labels);
