using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LabeledByAI.Services;

public class GetBestLabelService(IChatClient chatClient, ILogger<GetBestLabelService> logger)
{
    public async Task<GetBestLabelResponse> ExecuteAsync(GetBestLabelRequest request, string githubToken)
    {
        var reqIssue = request.Issue;
        var reqLabels = request.Labels;

        // get github repository
        var github = new GitHub(githubToken);
        var repo = github.GetRepository(reqIssue.Owner, reqIssue.Repo);

        // load the issue details from the repository
        logger.LogInformation("Loading the issue details...");
        var issue = await repo.GetIssueAsync(reqIssue.Number);

        // load all labels from the repository
        logger.LogInformation("Loading all labels from the repo...");
        var labels = await repo.GetLabelsAsync(new(reqLabels.Names, reqLabels.Pattern));

        if (!IsValidRequest(issue, labels, out var errorResult))
            throw new InvalidOperationException(errorResult);

        logger.LogInformation("The new issue is a valid object.");

        var response = await GetBestLabelAsync(issue, labels);
        if (!IsValidResponse(issue, labels, response, out errorResult))
            throw new InvalidOperationException(errorResult);

        return response;
    }

    private bool IsValidRequest(
        [NotNullWhen(true)] GitHubIssue? issue,
        [NotNullWhen(true)] IList<GitHubLabel>? labels,
        [NotNullWhen(false)] out string? errorResult)
    {
        if (string.IsNullOrWhiteSpace(issue?.Id))
        {
            logger.LogError("Unable to load issue details from GitHub.");
            errorResult = "The issue could not be loaded.";
            return false;
        }

        if (labels is null || labels.Count == 0)
        {
            logger.LogError("Unable to load labels from GitHub.");
            errorResult = "The labels could not be loaded.";
            return false;
        }

        errorResult = null;
        return true;
    }

    private bool IsValidResponse(
        [NotNullWhen(true)] GitHubIssue? issue,
        [NotNullWhen(true)] IList<GitHubLabel>? labels,
        [NotNullWhen(true)] GetBestLabelResponse? response,
        [NotNullWhen(false)] out string? errorResult)
    {
        errorResult = null;
        return true;
    }

    public async Task<GetBestLabelResponse?> GetBestLabelAsync(GitHubIssue issue, IList<GitHubLabel> availableLabels)
    {
        logger?.LogInformation("Generating OpenAI request...");

        var systemPrompt = GetSystemPrompt(availableLabels);
        var assistantPrompt = GetIssuePrompt(issue);

        var responseJson = await chatClient.CompleteJsonAsync(systemPrompt, assistantPrompt, logger);

        var response = responseJson.Deserialize<GetBestLabelResponse>();

        return response;
    }

    private static string GetSystemPrompt(params IEnumerable<GitHubLabel> labels) =>
        $$"""
        You are an expert developer who is able to correctly and
        accurately assign labels to new issues that are opened.

        You are to pick from the following list of labels and
        assign just one of them. If none of the labels are
        correct, do not assign any labels. If no issue content 
        was provided or if there is not enough content to make
        a decision, do not assign any labels. If the label that
        you have selected is not in the list of labels, then
        do not assign any labels.

        If no labels match or can be assigned, then you are to
        reply with a null label and null reason. 
        The only labels that are valid for assignment are found
        between the "===== Available Labels =====" lines. Do not
        return a label if that label is not found in there.

        Some labels have an additional description that should be
        used in order to find the best match.

        You are to also provide a reason as to why that label was 
        selected to make sure that everyone knows why. Also, you
        need to make sure to mention other related labels and why
        they were not a good selection for the issue. Give a reason
        in 50 to 100 words.

        ===== Available Labels =====
        {{GetPromptLabelList(labels)}}
        ===== Available Labels =====
        
        Please reply in json with the format and only in this format:

        { 
            "label": "LABEL_NAME_HERE",
            "reason": "REASON_FOR_LABEL_HERE"
        }

        """;

    private static string GetPromptLabelList(IEnumerable<GitHubLabel> labels)
    {
        var sb = new StringBuilder();

        foreach (var label in labels)
        {
            sb.AppendLine($"- name: {label.Name}");
            if (!string.IsNullOrWhiteSpace(label.Description))
            {
                sb.AppendLine($"  description: {label.Description}");
            }
        }

        return sb.ToString();
    }

    private static string GetIssuePrompt(GitHubIssue issue) => $"""
        A new issue has arrived, please label it correctly and accurately.
        
        The issue title is:
        {issue.Title ?? "-"}
        
        The issue body is:
        {issue.Body}
        """;
}
