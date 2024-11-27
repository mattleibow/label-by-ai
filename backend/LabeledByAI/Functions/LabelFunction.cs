using LabeledByAI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace LabeledByAI;

public class LabelFunction(GitHubBestLabelAIChatClient chatClient, ILogger<LabelFunction> logger)
    : BaseFunction<LabelFunction.LabelRequest>(logger)
{
    [Function("label")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request)
    {
        logger.LogInformation("Starting to process a new issue...");

        var labelRequest = await ParseRequestAsync(request);
        if (!ValidateRequest(labelRequest, out var errorResult))
        {
            return errorResult;
        }

        var (issue, labels) = await FetchGitHubObjects(request, labelRequest);
        if (!ValidateRequestIssue(issue, labels, out errorResult))
        {
            return errorResult;
        }

        logger.LogInformation("The new issue is a valid object.");

        var responseJson = await chatClient.GetBestLabelAsync(issue, labels);

        return new OkObjectResult(responseJson);
    }

    private async Task<(GitHubIssue? Issue, IList<GitHubLabel>? Labels)> FetchGitHubObjects(HttpRequest request, LabelRequest labelRequest)
    {
        var github = GetGitHubRepository(
            request,
            labelRequest.Issue.Owner,
            labelRequest.Issue.Repo);

        // load all labels from the repository
        logger.LogInformation("Loading all labels from the repo...");
        var labels = await github.GetLabelsAsync(new(labelRequest.Labels.Names, labelRequest.Labels.Pattern));

        // load issue details from the repository
        logger.LogInformation("Loading the issue details...");
        var issue = await github.GetIssueAsync(labelRequest.Issue.Number);

        return (issue, labels);
    }

    private bool ValidateRequestIssue([NotNullWhen(true)] GitHubIssue? issue, [NotNullWhen(true)] IList<GitHubLabel>? labels, [NotNullWhen(false)] out IActionResult? errorResult)
    {
        if (string.IsNullOrWhiteSpace(issue?.Id))
        {
            logger.LogError("Unable to load issue details from GitHub.");
            errorResult = new BadRequestObjectResult("The issue could not be loaded.");
            return false;
        }

        if (labels is null || labels.Count == 0)
        {
            logger.LogError("Unable to load labels from GitHub.");
            errorResult = new BadRequestObjectResult("The labels could not be loaded.");
            return false;
        }

        errorResult = null;
        return true;
    }

    public record LabelRequest(
        int Version,
        LabelRequestIssue Issue,
        LabelRequestLabels Labels);

    public record LabelRequestLabels(
        string[]? Names,
        string? Pattern);

    public record LabelRequestIssue(
        string Owner,
        string Repo,
        int Number);
}
