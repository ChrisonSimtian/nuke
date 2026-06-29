using System;
using System.IO;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

// Guards the trigger's obsolete OptionalInputs/RequiredInputs arrays directly: the attribute path only
// sets Inputs, so the legacy fold is otherwise untested but still reachable via a GetTriggers() override.
public class GitHubActionsWorkflowDispatchTriggerSpecs
{
    private static string Render(GitHubActionsWorkflowDispatchTrigger trigger)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true);
        trigger.Write(new CustomFileWriter(writer, indentationFactor: 2, commentPrefix: "#"));
        writer.Flush();

        stream.Seek(offset: 0, SeekOrigin.Begin);
        return new StreamReader(stream).ReadToEnd();
    }

    // Guard: the obsolete legacy arrays still emit, untyped, with the correct required flags.
    [Fact]
    public void Legacy_arrays_emit_untyped_inputs()
    {
#pragma warning disable CS0618 // exercising the restored obsolete trigger API on purpose
        var yaml = Render(new GitHubActionsWorkflowDispatchTrigger
                          {
                              OptionalInputs = new[] { "Opt" },
                              RequiredInputs = new[] { "Req" }
                          });
#pragma warning restore CS0618

        yaml.Should().Contain("Opt:").And.Contain("required: false");
        yaml.Should().Contain("Req:").And.Contain("required: true");
        yaml.Should().NotContain("type:");
    }

    // Ordering guard: legacy arrays emit before typed inputs.
    [Fact]
    public void Legacy_arrays_emit_before_typed_inputs()
    {
#pragma warning disable CS0618
        var yaml = Render(new GitHubActionsWorkflowDispatchTrigger
                          {
                              OptionalInputs = new[] { "Legacy" },
                              Inputs = new[]
                                       {
                                           new GitHubActionsWorkflowDispatchInput
                                           { Name = "Typed", Type = GitHubActionsInputType.Boolean, Default = "true" }
                                       }
                          });
#pragma warning restore CS0618

        yaml.IndexOf("Legacy:", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("Typed:", StringComparison.Ordinal));
    }

    // Regression guard: the obsolete arrays and the new Inputs model emit byte-identical YAML for
    // equivalent inputs — migrating off the obsolete API must not change the output.
    [Fact]
    public void Legacy_arrays_and_typed_inputs_emit_identical_yaml()
    {
#pragma warning disable CS0618
        var legacy = Render(new GitHubActionsWorkflowDispatchTrigger
                            {
                                OptionalInputs = new[] { "Opt" },
                                RequiredInputs = new[] { "Req" }
                            });
#pragma warning restore CS0618

        var typed = Render(new GitHubActionsWorkflowDispatchTrigger
                           {
                               Inputs = new[]
                                        {
                                            new GitHubActionsWorkflowDispatchInput { Name = "Opt", Required = false },
                                            new GitHubActionsWorkflowDispatchInput { Name = "Req", Required = true }
                                        }
                           });

        typed.Should().Be(legacy);
    }
}
