using System;
using System.IO;
using Fallout.Cli.Commands;
using Fallout.Common.IO;
using Fallout.Solutions;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Fallout.Cli.Specs.Commands;

public class UpdateCommandSpecs
{
    [Fact]
    public void Command_is_named_update()
        => new UpdateCommand(new FakeConsolePrompts(), new ConfigurationReader(), new BuildScaffolder()).Name.Should().Be("update");

    [Fact]
    public void Updating_the_build_project_retargets_it_to_net10()
    {
        // A previous Fallout could leave a stale <TargetFramework>; update must lift it to the
        // framework Fallout.Common ships for. Guards against the literal drifting off net10.0.
        var projectFile = (AbsolutePath)Path.Combine(
            Path.GetTempPath(), "fallout-update-" + Guid.NewGuid().ToString("N"), "_build.csproj");
        projectFile.Parent.CreateDirectory();
        projectFile.WriteAllText(
            """
            <Project>
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        try
        {
            var buildProject = ProjectModelTasks.ParseProject(projectFile);

            UpdateCommand.UpdateTargetFramework(buildProject);

            buildProject.GetPropertyValue("TargetFramework").Should().Be("net10.0");
        }
        finally
        {
            Directory.Delete(projectFile.Parent, recursive: true);
        }
    }

    [Fact]
    public async Task Declining_all_updates_without_a_build_script_completes_and_returns_zero()
    {
        var dir = (AbsolutePath)Path.Combine(Path.GetTempPath(), "fallout-update-" + Guid.NewGuid().ToString("N"));
        dir.CreateDirectory();
        var prompts = new FakeConsolePrompts { InvokeConfirmedActions = false };
        try
        {
            // No build script and every confirmation declined → no update steps run, but the command
            // still completes cleanly.
            (await new UpdateCommand(prompts, new ConfigurationReader(), new BuildScaffolder()).ExecuteAsync([], dir, buildScript: null)).Should().Be(0);

            prompts.Completions.Should().Contain("Updates");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
