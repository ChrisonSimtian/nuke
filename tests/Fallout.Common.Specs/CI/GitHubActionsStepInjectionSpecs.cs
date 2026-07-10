using System;
using System.IO;
using System.Threading.Tasks;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Execution;
using FluentAssertions;
using VerifyXunit;
using Xunit;

namespace Fallout.Common.Specs.CI;

// Behavioural coverage of the step-injection pipeline: positions, robustness when cache/artifacts are
// absent, workflow scoping, ordering, and the no-op regression guarantee.
public class GitHubActionsStepInjectionSpecs
{
    // A build that implements the hook via a per-test delegate. Subclasses the shared TestBuild so all
    // targets exist; only snapshot-neutral because ConfigurationGenerationSpecs uses plain TestBuild.
    private sealed class InjectingBuild : ConfigurationGenerationSpecs.TestBuild, IConfigureGitHubActions
    {
        public Action<GitHubActionsStepPipeline> Hook { get; set; }
        public void ConfigureSteps(GitHubActionsStepPipeline pipeline) => Hook?.Invoke(pipeline);
    }

    private static string Render(
        Action<GitHubActionsStepPipeline> configure,
        Action<TestGitHubActionsAttribute> setup = null,
        ConfigurationGenerationSpecs.TestBuild build = null)
    {
        build ??= new InjectingBuild { Hook = configure };
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        var attribute = new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                        {
                            On = new[] { GitHubActionsTrigger.Push },
                            InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) }
                        };
        setup?.Invoke(attribute);

        var stream = new MemoryStream();
        ((ConfigurationAttributeBase)attribute).Build = build;
        attribute.Stream = new StreamWriter(stream, leaveOpen: true);
        attribute.Generate(relevantTargets);

        stream.Seek(offset: 0, SeekOrigin.Begin);
        return new StreamReader(stream).ReadToEnd();
    }

    private static GitHubActionsCustomStep Marker(string name)
        => new GitHubActionsCustomStep { Name = name, Run = new[] { "echo " + name } };

    [Fact]
    public void Post_checkout_step_lands_after_checkout_before_cache()
    {
        var yaml = Render(p => p.Insert(GitHubActionsStepPosition.PostCheckout, Marker("mark")));

        yaml.IndexOf("uses: actions/checkout", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: mark", StringComparison.Ordinal));
        yaml.IndexOf("name: mark", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("uses: actions/cache", StringComparison.Ordinal));
    }

    [Fact]
    public void Pre_run_step_lands_after_cache_before_setup_dotnet()
    {
        var yaml = Render(p => p.Insert(GitHubActionsStepPosition.PreRun, Marker("mark")));

        yaml.IndexOf("uses: actions/cache", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: mark", StringComparison.Ordinal));
        yaml.IndexOf("name: mark", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("Setup: .NET SDK", StringComparison.Ordinal));
    }

    [Fact]
    public void Post_run_step_lands_after_run_before_artifacts()
    {
        var yaml = Render(p => p.Insert(GitHubActionsStepPosition.PostRun, Marker("mark")));

        yaml.IndexOf("run: dotnet fallout", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: mark", StringComparison.Ordinal));
        yaml.IndexOf("name: mark", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("Publish:", StringComparison.Ordinal));
    }

    [Fact]
    public void Job_end_step_lands_after_artifacts()
    {
        var yaml = Render(p => p.Insert(GitHubActionsStepPosition.JobEnd, Marker("mark")));

        yaml.IndexOf("Publish:", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: mark", StringComparison.Ordinal));
    }

    [Fact]
    public void Positions_hold_when_cache_and_artifacts_absent()
    {
        var yaml = Render(
            p =>
            {
                p.Insert(GitHubActionsStepPosition.PostCheckout, Marker("post-checkout"));
                p.Insert(GitHubActionsStepPosition.PreRun, Marker("pre-run"));
                p.Insert(GitHubActionsStepPosition.PostRun, Marker("post-run"));
                p.Insert(GitHubActionsStepPosition.JobEnd, Marker("job-end"));
            },
            setup: a =>
            {
                a.CacheKeyFiles = new string[0];
                a.PublishArtifacts = false;
            });

        yaml.Should().NotContain("uses: actions/cache");
        yaml.Should().NotContain("Publish:");
        // All four land, in order, anchored to checkout + run block.
        yaml.IndexOf("uses: actions/checkout", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: post-checkout", StringComparison.Ordinal));
        yaml.IndexOf("name: post-checkout", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: pre-run", StringComparison.Ordinal));
        yaml.IndexOf("name: pre-run", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("Setup: .NET SDK", StringComparison.Ordinal));
        yaml.IndexOf("run: dotnet fallout", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: post-run", StringComparison.Ordinal));
        yaml.IndexOf("name: post-run", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: job-end", StringComparison.Ordinal));
    }

    [Fact]
    public void Multiple_inserts_at_one_position_render_in_call_order()
    {
        var yaml = Render(p =>
        {
            p.Insert(GitHubActionsStepPosition.PostRun, Marker("first"));
            p.Insert(GitHubActionsStepPosition.PostRun, Marker("second"));
        });

        yaml.IndexOf("name: first", StringComparison.Ordinal)
            .Should().BeLessThan(yaml.IndexOf("name: second", StringComparison.Ordinal));
    }

    [Fact]
    public void Steps_scope_to_the_named_workflow()
    {
        // The hook only inserts for a workflow named "other"; this job is "test", so nothing lands.
        var yaml = Render(p =>
        {
            if (p.WorkflowName == "other")
                p.Insert(GitHubActionsStepPosition.PostRun, Marker("scoped"));
        });

        yaml.Should().NotContain("name: scoped");
    }

    [Fact]
    public void Run_step_without_shell_omits_shell_under_default_shell()
    {
        var yaml = Render(
            p => p.Insert(GitHubActionsStepPosition.PostRun, new GitHubActionsCustomStep { Name = "noshell", Run = new[] { "echo hi" } }),
            setup: a => a.DefaultShell = "pwsh");

        // Workflow-level defaults.run.shell is emitted, but the injected run step carries no shell: of its own.
        yaml.Should().Contain("shell: pwsh");            // the workflow-level defaults block
        yaml.Should().Contain("name: noshell");
        yaml.Should().Contain("run: echo hi");
        // The only "shell:" occurrence is the workflow-level default (one match).
        System.Text.RegularExpressions.Regex.Matches(yaml, "shell:").Count.Should().Be(1);
    }

    [Fact]
    public void Empty_configure_is_byte_identical_to_no_interface()
    {
        var withEmptyHook = Render(configure: _ => { });
        var withoutInterface = Render(configure: null, setup: null, build: new ConfigurationGenerationSpecs.TestBuild());

        withEmptyHook.Should().Be(withoutInterface);
    }

    // Locks the exact rendered YAML for a rich multi-position injection (uses + run + optional fields).
    [Fact]
    public Task Rich_injection_renders_expected_yaml()
    {
        var yaml = Render(p =>
        {
            p.Insert(GitHubActionsStepPosition.PostCheckout, new GitHubActionsCustomStep
                                                             {
                                                                 Name = "Setup Node",
                                                                 Uses = "actions/setup-node@v4",
                                                                 With = new() { ["node-version"] = "20" }
                                                             });
            p.Insert(GitHubActionsStepPosition.PostRun, new GitHubActionsCustomStep
                                                        {
                                                            Name = "Perform CodeQL Analysis",
                                                            Uses = "github/codeql-action/analyze@v3",
                                                            If = "github.ref == 'refs/heads/main'"
                                                        });
            p.Insert(GitHubActionsStepPosition.JobEnd, new GitHubActionsCustomStep
                                                       {
                                                           Run = new[] { "echo done", "echo bye" }
                                                       });
        });

        return Verifier.Verify(yaml);
    }
}
