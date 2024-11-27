using System.Text.Json.Serialization;

namespace LabeledByAI.Services;

public record GitHubIssue(
    string Id,
    int Number,
    string Author,
    string Title,
    string Body,
    int TotalComments,
    int TotalReactions,
    DateTimeOffset LastActivityOn,
    DateTimeOffset CreatedOn,
    IReadOnlyList<string> Labels)
{
    public IReadOnlyList<GitHubComment>? Comments { get; internal set; }

    [JsonIgnore]
    public IEnumerable<GitHubComment>? UserComments =>
        Comments?.Where(c => c.IsUser);

    public int TotalUserComments =>
        UserComments?.Count() ?? 0;

    public int TotalCommentReactions =>
        Comments?.Sum(c => c.TotalReactions) ?? 0;

    public IEnumerable<string> UserContributors =>
        UserComments?.Select(c => c.Author)?.Union([Author]).Distinct() ?? [Author];

    public int TotalUserContributors =>
        UserContributors.Count();

    [JsonIgnore]
    public TimeSpan Age =>
        DateTimeOffset.UtcNow - CreatedOn;

    [JsonIgnore]
    public TimeSpan TimeSinceLastActivity =>
        DateTimeOffset.UtcNow - LastActivityOn;
}

public record GitHubComment(
    string Id,
    string Author,
    string AuthorType,
    string Body,
    DateTimeOffset CreatedOn,
    int TotalReactions)
{
    public bool IsUser =>
        !AuthorType.StartsWith("/apps/");

    [JsonIgnore]
    public TimeSpan Age =>
        DateTimeOffset.UtcNow - CreatedOn;
}
