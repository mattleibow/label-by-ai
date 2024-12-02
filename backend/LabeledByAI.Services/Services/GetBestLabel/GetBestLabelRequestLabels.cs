namespace LabeledByAI.Services;

public record GetBestLabelRequestLabels(
    string[]? Names,
    string? Pattern);
