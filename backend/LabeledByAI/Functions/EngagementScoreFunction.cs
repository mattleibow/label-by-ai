using LabeledByAI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Octokit.GraphQL;

namespace LabeledByAI;

public class EngagementScoreFunction(ILogger<EngagementScoreFunction> logger)
    : BaseFunction<EngagementScoreFunction.EngagementScoreRequest>(logger)
{
    [Function("engagement-score")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request)
    {
        logger.LogInformation("Starting to process a new issue...");

        var esRequest = await ParseRequestAsync(request);

        var issues = await FetchGitHubObjectsAsync(request, esRequest);

        var score = issues.Select(CalculateScore).ToList();

        return new OkObjectResult(new { score, issues });
    }

    private async Task<IList<GitHubIssue>?> FetchGitHubObjectsAsync(HttpRequest request, EngagementScoreRequest esRequest)
    {
        // TODO:
        // - download the github issue and all the comments and reactions
        //   - issues touched in the last [7] days
        //   - download linked PRs
        //   - merge duplicated issues

        var github = GetGitHubRepository(
            request,
            esRequest.Issue.Owner,
            esRequest.Issue.Repo);

        if (esRequest.Issue.Number is int number)
        {
            logger.LogInformation("Loading the issue details...");
            var issue = await github.GetIssueDetailedAsync(number);
            return [issue];
        }

        // TODO: download all issues and process them

        logger.LogError("No issue number was provided in the request body.");
        return null;
    }

    private int CalculateScore(GitHubIssue issue)
    {
        // Components:
        //  - Number of Comments       => Indicates discussion and interest
        //  - Number of Reactions      => Shows emotional engagement
        //  - Number of Contributors   => Reflects the diversity of input
        //  - Time Since Last Activity => More recent activity indicates higher engagement
        //  - Issue Age                => Older issues might need more attention
        //  - Number of Linked PRs     => Shows active work on the issue
        var totalComments = issue.TotalUserComments;
        var totalReactions = issue.TotalReactions + issue.TotalCommentReactions;
        var contributors = issue.TotalUserContributors;
        var lastActivity = Math.Max(1, (int)issue.TimeSinceLastActivity.TotalDays);
        var issueAge = Math.Max(1, (int)issue.Age.TotalDays);
        var linkedPullRequests = 0;// issue.LinkedPullRequests.Count;

        // Weights:
        const int CommentsWeight = 3;
        const int ReactionsWeight = 1;
        const int ContributorsWeight = 2;
        const int LastActivityWeight = 1;
        const int IssueAgeWeight = 1;
        const int LinkedPullRequestsWeight = 2;

        return
            (CommentsWeight * totalComments) +
            (ReactionsWeight * totalReactions) +
            (ContributorsWeight * contributors) +
            (LastActivityWeight * (1 / lastActivity)) +
            (IssueAgeWeight * (1 / issueAge)) +
            (LinkedPullRequestsWeight * linkedPullRequests);
    }

    public record EngagementScoreRequest(
        int Version,
        EngagementScoreIssue Issue);

    public record EngagementScoreIssue(
        string Owner,
        string Repo,
        int? Number);
}
