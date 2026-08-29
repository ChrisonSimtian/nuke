using System.Collections.Generic;
using System.Linq;
using Fallout.Common.CI;
using Fallout.Common.Git;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;

namespace Fallout.Common.Execution;

public class ArgumentsFromGitCommitMessageAttribute : BuildExtensionAttributeBase, IOnBuildCreated
{
    public string Prefix { get; set; } = "[nuke++]";

    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        if (BuildServerConfigurationGeneration.IsActive)
        {
            return;
        }

        var commit = GitRepository.GetCommitFromCI();
        if (commit == null)
        {
            return;
        }

        var git = ToolResolver.GetEnvironmentOrPathTool("git");
        var lastLine = git.Invoke($"show -s --format=%B {commit}", logInvocation: false, logOutput: false)
            .Select(x => x.Text)
            .LastOrDefault(x => !x.IsNullOrEmpty());

        if (!lastLine?.StartsWithOrdinalIgnoreCase(Prefix) ?? true)
        {
            return;
        }

        var arguments = lastLine[Prefix.Length..].TrimStart();
        ParameterService.Instance.ArgumentsFromCommitMessageService = new ArgumentParser(arguments);
    }
}
