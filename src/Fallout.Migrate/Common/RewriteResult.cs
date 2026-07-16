namespace Fallout.Migrate.Common;

/// <summary>
/// The result of rewriting a file's content: the new <paramref name="Content"/> and how many
/// individual edits were made.
/// </summary>
/// <param name="Content">The rewritten file content.</param>
/// <param name="EditCount">The number of edits applied; zero means the content is unchanged.</param>
internal readonly record struct RewriteResult(string Content, int EditCount);
