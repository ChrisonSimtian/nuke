using System.IO;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

// Unit-renders a single custom step in isolation (base indent 0), mirroring the Render helper
// in GitHubActionsWorkflowDispatchTriggerSpecs.
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

    [Fact]
    public void Uses_step_with_with_block_renders_name_uses_with()
    {
        var yaml = Render(new GitHubActionsCustomStep
                          {
                              Name = "Setup Node",
                              Uses = "actions/setup-node@v4",
                              With = new() { ["node-version"] = "20" }
                          });

        yaml.Should().Be(
            "- name: Setup Node\n" +
            "  uses: actions/setup-node@v4\n" +
            "  with:\n" +
            "    node-version: 20\n");
    }

    [Fact]
    public void Uses_step_without_name_starts_with_uses()
    {
        var yaml = Render(new GitHubActionsCustomStep { Uses = "actions/checkout@v4" });

        yaml.Should().Be("- uses: actions/checkout@v4\n");
    }

    [Fact]
    public void Single_line_run_renders_inline()
    {
        var yaml = Render(new GitHubActionsCustomStep { Name = "Echo", Run = new[] { "echo hi" } });

        yaml.Should().Be(
            "- name: Echo\n" +
            "  run: echo hi\n");
    }

    [Fact]
    public void Multi_line_run_renders_block_scalar()
    {
        var yaml = Render(new GitHubActionsCustomStep { Run = new[] { "echo one", "echo two" } });

        yaml.Should().Be(
            "- run: |\n" +
            "    echo one\n" +
            "    echo two\n");
    }

    [Fact]
    public void All_optional_fields_render_in_fixed_order()
    {
        var yaml = Render(new GitHubActionsCustomStep
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

        yaml.Should().Be(
            "- name: Full\n" +
            "  id: full\n" +
            "  uses: some/action@v1\n" +
            "  with:\n" +
            "    k: v\n" +
            "  env:\n" +
            "    E: 1\n" +
            "  if: success()\n" +
            "  continue-on-error: true\n" +
            "  timeout-minutes: 5\n");
    }

    [Fact]
    public void Run_step_renders_shell_when_set()
    {
        var yaml = Render(new GitHubActionsCustomStep { Run = new[] { "gci" }, Shell = "pwsh" });

        yaml.Should().Be(
            "- run: gci\n" +
            "  shell: pwsh\n");
    }
}
