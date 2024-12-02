using Octokit.GraphQL;

namespace LabeledByAI.Services;

public class GitHub(Connection connection)
{
    private readonly Dictionary<(string Owner, string Repo), GitHubRepository> _repositories = [];

    public GitHub(string githubToken, string applicationName = "labeled-by-ai")
        : this(new Connection(new ProductHeaderValue(applicationName), githubToken))
    {
    }

    public GitHubRepository GetRepository(string owner, string repo)
    {
        if (!_repositories.TryGetValue((owner, repo), out var instance))
        {
            instance = new GitHubRepository(connection, owner, repo);
            _repositories[(owner, repo)] = instance;
        }

        return instance;
    }
}
