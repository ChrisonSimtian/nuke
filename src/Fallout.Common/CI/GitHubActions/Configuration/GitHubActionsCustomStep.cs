using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

/// <summary>
/// A user-constructed workflow step injected via <see cref="IConfigureGitHubActions"/>. A non-empty
/// <see cref="Uses"/> renders a marketplace/action step; a non-empty <see cref="Run"/> renders a shell
/// step (a single entry as <c>run: x</c>, multiple as a <c>run: |</c> block scalar). Exactly one of the
/// two must be set — enforced at generation time.
/// </summary>
public class GitHubActionsCustomStep : GitHubActionsStep
{
    public string Name { get; set; }
    public string Uses { get; set; }
    public Dictionary<string, string> With { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Env { get; set; } = new Dictionary<string, string>();
    public string If { get; set; }
    public string Shell { get; set; }
    public string[] Run { get; set; } = new string[0];
    public bool? ContinueOnError { get; set; }
    public int? TimeoutMinutes { get; set; }
    public string Id { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        // The first emitted key carries the '- ' list marker; every later key is a '  ' continuation.
        var written = false;

        if (!Name.IsNullOrWhiteSpace())
            Scalar("name", Name.SingleQuoteYaml());   // single-quoted so a ':' or apostrophe in the name stays valid YAML
        if (!Id.IsNullOrWhiteSpace())
            Scalar("id", Id);
        if (!Uses.IsNullOrWhiteSpace())
            Scalar("uses", Uses);
        if ((With?.Count ?? 0) > 0)
            MapBlock("with", With);
        if ((Env?.Count ?? 0) > 0)
            MapBlock("env", Env);

        var runCount = Run?.Length ?? 0;
        if (runCount == 1)
        {
            Scalar("run", Run[0]);
        }
        else if (runCount > 1)
        {
            writer.WriteLine((written ? "  " : "- ") + "run: |");
            written = true;
            using (writer.Indent())
                Run.ForEach(x => writer.WriteLine($"  {x}"));
        }

        if (!Shell.IsNullOrWhiteSpace())
            Scalar("shell", Shell);
        if (!If.IsNullOrWhiteSpace())
            Scalar("if", If);
        if (ContinueOnError.HasValue)
            Scalar("continue-on-error", ContinueOnError.Value ? "true" : "false");
        if (TimeoutMinutes.HasValue)
            Scalar("timeout-minutes", TimeoutMinutes.Value.ToString(CultureInfo.InvariantCulture));

        return;

        void Scalar(string key, string value)
        {
            writer.WriteLine((written ? "  " : "- ") + $"{key}: {value}");
            written = true;
        }

        void MapBlock(string key, Dictionary<string, string> map)
        {
            writer.WriteLine((written ? "  " : "- ") + $"{key}:");
            written = true;
            // Ordinal sort so multi-entry blocks render deterministically (Dictionary order isn't guaranteed).
            using (writer.Indent())
                map.OrderBy(x => x.Key, StringComparer.Ordinal).ForEach(x => writer.WriteLine($"  {x.Key}: {x.Value}"));
        }
    }
}
