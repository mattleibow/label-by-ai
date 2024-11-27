using LabeledByAI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Octokit.GraphQL;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace LabeledByAI;

public abstract class BaseFunction<TRequestBody>(ILogger<BaseFunction<TRequestBody>> logger)
{
    private const string GitHubTokenHeaderName = "X-GitHub-Token";
    private const string ApplicationName = "labeled-by-ai";

    protected async Task<TRequestBody?> ParseRequestAsync(HttpRequest request)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<TRequestBody>(request.Body, SerializerOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize the request body.");
            return default;
        }
    }

    protected virtual bool ValidateRequest([NotNullWhen(true)] TRequestBody? request, [NotNullWhen(false)] out IActionResult? errorResult)
    {
        if (request is null)
        {
            logger.LogError("The request is null.");
            errorResult = new BadRequestObjectResult("The request is null.");
            return false;
        }

        errorResult = null;
        return true;
    }

    protected GitHubRepository GetGitHubRepository(HttpRequest request, string owner, string repo)
    {
        var githubToken = request.Headers[GitHubTokenHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(githubToken))
        {
            logger.LogError("No GitHub token was provided in the request headers.");
            return null;
        }

        var githubConnection = new Connection(
            new ProductHeaderValue(ApplicationName), githubToken);

        var github = new GitHubRepository(
            githubConnection,
            owner,
            repo);

        return github;
    }

    protected static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
}
