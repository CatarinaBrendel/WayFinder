namespace WayFinder.DevTools.Application.Repositories.Git;

public sealed record GitCommit(
    string Hash,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset AuthoredAt,
    string Subject
);
