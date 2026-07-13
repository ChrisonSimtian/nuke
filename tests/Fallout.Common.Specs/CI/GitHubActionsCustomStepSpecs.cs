using System.IO;
using System.Threading.Tasks;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Utilities;
using VerifyXunit;
using Xunit;

namespace Fallout.Common.Specs.CI;

// Renders a single custom step in isolation, like GitHubActionsWorkflowDispatchTriggerSpecs. Output is
// snapshotted with Verify (line endings normalized), matching the generator testing strategy elsewhere.
public class GitHubActionsCustomStepSpecs
{
    private static Task Verify(GitHubActionsCustomStep step)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true);
        step.Write(new CustomFileWriter(writer, indentationFactor: 2, commentPrefix: "#"));
        writer.Flush();

        stream.Seek(offset: 0, SeekOrigin.Begin);
        return Verifier.Verify(new StreamReader(stream).ReadToEnd());
    }

    [Fact]
    public Task Uses_step_with_with_block()
        => Verify(new GitHubActionsCustomStep
                  {
                      Name = "Setup Node",
                      Uses = "actions/setup-node@v4",
                      With = new() { ["node-version"] = "20" }
                  });

    [Fact]
    public Task Uses_step_without_name_starts_with_uses()
        => Verify(new GitHubActionsCustomStep { Uses = "actions/checkout@v4" });

    [Fact]
    public Task Single_line_run_renders_inline()
        => Verify(new GitHubActionsCustomStep { Name = "Echo", Run = new[] { "echo hi" } });

    // A name with a colon-space would be invalid YAML unquoted; it must be single-quoted like the built-in steps.
    [Fact]
    public Task Name_with_a_colon_is_quoted()
        => Verify(new GitHubActionsCustomStep { Name = "Deploy: prod", Run = new[] { "echo hi" } });

    // An embedded apostrophe must be YAML-escaped by doubling (''), not the backslash the shared
    // SingleQuote() helper would produce (which is invalid inside a YAML single-quoted scalar).
    [Fact]
    public Task Name_with_an_apostrophe_is_yaml_escaped()
        => Verify(new GitHubActionsCustomStep { Name = "Bob's step", Run = new[] { "echo hi" } });

    // Multi-entry with:/env: must render in a deterministic (ordinal) order regardless of insertion order.
    [Fact]
    public Task Multi_entry_with_renders_in_ordinal_order()
        => Verify(new GitHubActionsCustomStep
                  {
                      Uses = "some/action@v1",
                      With = new() { ["beta"] = "2", ["alpha"] = "1" }
                  });

    [Fact]
    public Task Multi_line_run_renders_block_scalar()
        => Verify(new GitHubActionsCustomStep { Run = new[] { "echo one", "echo two" } });

    [Fact]
    public Task All_optional_fields_render_in_fixed_order()
        => Verify(new GitHubActionsCustomStep
                  {
                      Name = "Full",
                      Id = "full",
                      Uses = "some/action@v1",
                      With = new() { ["k"] = "v" },
                      Env = new() { ["E"] = "1" },
                      If = "success()",
                      ContinueOnError = true,
                      TimeoutMinutes = 5
                  });

    [Fact]
    public Task Run_step_renders_shell_when_set()
        => Verify(new GitHubActionsCustomStep { Run = new[] { "gci" }, Shell = "pwsh" });
}
