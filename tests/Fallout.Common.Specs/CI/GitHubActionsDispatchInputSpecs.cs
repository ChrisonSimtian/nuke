using System.IO;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Execution;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

// End-to-end behaviour of workflow_dispatch inputs through the attribute → generator → YAML pipeline,
// across the obsolete and new APIs.
public class GitHubActionsDispatchInputSpecs
{
    private static string Render(TestGitHubActionsAttribute attribute)
    {
        var build = new ConfigurationGenerationSpecs.TestBuild();
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        var stream = new MemoryStream();
        ((ConfigurationAttributeBase)attribute).Build = build;
        attribute.Stream = new StreamWriter(stream, leaveOpen: true);
        attribute.Generate(relevantTargets);

        stream.Seek(offset: 0, SeekOrigin.Begin);
        return new StreamReader(stream).ReadToEnd();
    }

    // Regression guard: the obsolete OnWorkflowDispatch*Inputs arrays and equivalent typed String inputs
    // generate byte-identical YAML — migrating off the obsolete API is a no-op on the output.
    [Fact]
    public void Legacy_arrays_and_typed_string_inputs_emit_identical_yaml()
    {
#pragma warning disable CS0618 // comparing the obsolete API against its typed replacement on purpose
        var legacy = Render(new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                            {
                                InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) },
                                OnWorkflowDispatchOptionalInputs = new[] { "Opt" },
                                OnWorkflowDispatchRequiredInputs = new[] { "Req" }
                            });
#pragma warning restore CS0618

        var typed = Render(new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                           {
                               InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) },
                               Inputs = new[]
                                        {
                                            new GitHubActionsInputAttribute("Opt"),
                                            new GitHubActionsInputAttribute("Req") { Required = true }
                                        }
                           });

        typed.Should().Be(legacy);
    }

    // Regression guard: a workflow name with spaces is normalized to underscores; an input scoped to the
    // same spelled name must still resolve (not silently drop, not throw "unknown workflow").
    [Fact]
    public void Input_scoped_to_a_spaced_workflow_name_is_included()
    {
        var attribute = new TestGitHubActionsAttribute("My Workflow", GitHubActionsImage.UbuntuLatest)
                        {
                            InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) },
                            Inputs = new[]
                                     {
                                         new GitHubActionsInputAttribute("Scoped") { Workflows = new[] { "My Workflow" } }
                                     }
                        };

        var render = () => Render(attribute);

        render.Should().NotThrow();
        render().Should().Contain("Scoped:");
    }
}
