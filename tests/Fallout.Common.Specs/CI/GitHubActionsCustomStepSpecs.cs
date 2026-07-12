using System.IO;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

// Renders a single custom step in isolation, like GitHubActionsWorkflowDispatchTriggerSpecs.
public class GitHubActionsCustomStepSpecs
{
    private static string Render(GitHubActionsCustomStep step)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true);
        step.Write(new CustomFileWriter(writer, indentationFactor: 2, commentPrefix: "#"));
        writer.Flush();

        stream.Seek(offset: 0, SeekOrigin.Begin);
        return new StreamReader(stream).ReadToEnd();
    }

    // Normalize to '\n': the renderer emits Environment.NewLine, so this would otherwise fail on the
    // Windows post-merge job. The trailing '\n' is the terminal newline a raw literal omits.
    private static void ShouldRenderAs(string actual, string expected)
        => actual.ReplaceLineEndings("\n").Should().Be(expected.ReplaceLineEndings("\n") + "\n");

    [Fact]
    public void Uses_step_with_with_block_renders_name_uses_with()
        => ShouldRenderAs(
            Render(new GitHubActionsCustomStep
                   {
                       Name = "Setup Node",
                       Uses = "actions/setup-node@v4",
                       With = new() { ["node-version"] = "20" }
                   }),
            """
            - name: Setup Node
              uses: actions/setup-node@v4
              with:
                node-version: 20
            """);

    [Fact]
    public void Uses_step_without_name_starts_with_uses()
        => ShouldRenderAs(
            Render(new GitHubActionsCustomStep { Uses = "actions/checkout@v4" }),
            "- uses: actions/checkout@v4");

    [Fact]
    public void Single_line_run_renders_inline()
        => ShouldRenderAs(
            Render(new GitHubActionsCustomStep { Name = "Echo", Run = new[] { "echo hi" } }),
            """
            - name: Echo
              run: echo hi
            """);

    [Fact]
    public void Multi_line_run_renders_block_scalar()
        => ShouldRenderAs(
            Render(new GitHubActionsCustomStep { Run = new[] { "echo one", "echo two" } }),
            """
            - run: |
                echo one
                echo two
            """);

    [Fact]
    public void All_optional_fields_render_in_fixed_order()
        => ShouldRenderAs(
            Render(new GitHubActionsCustomStep
                   {
                       Name = "Full",
                       Id = "full",
                       Uses = "some/action@v1",
                       With = new() { ["k"] = "v" },
                       Env = new() { ["E"] = "1" },
                       If = "success()",
                       ContinueOnError = true,
                       TimeoutMinutes = 5
                   }),
            """
            - name: Full
              id: full
              uses: some/action@v1
              with:
                k: v
              env:
                E: 1
              if: success()
              continue-on-error: true
              timeout-minutes: 5
            """);

    [Fact]
    public void Run_step_renders_shell_when_set()
        => ShouldRenderAs(
            Render(new GitHubActionsCustomStep { Run = new[] { "gci" }, Shell = "pwsh" }),
            """
            - run: gci
              shell: pwsh
            """);
}
