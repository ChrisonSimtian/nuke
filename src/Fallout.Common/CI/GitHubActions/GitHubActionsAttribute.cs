using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions;

/// <summary>
/// Interface according to the <a href="https://help.github.com/en/articles/workflow-syntax-for-github-actions">official website</a>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GitHubActionsAttribute : ConfigurationAttributeBase
{
    private readonly string _name;
    private readonly GitHubActionsImage[] _images;
    private GitHubActionsSubmodules? _submodules;
    private bool? _lfs;
    private uint? _fetchDepth;
    private bool? _progress;
    private string _filter;
    private string _ref;

    public GitHubActionsAttribute(
        string name,
        GitHubActionsImage image,
        params GitHubActionsImage[] images)
    {
        _name = name.Replace(oldChar: ' ', newChar: '_');
        _images = new[] { image }.Concat(images).ToArray();
    }

    public override string IdPostfix => _name;
    public override Type HostType => typeof(GitHubActions);
    public override AbsolutePath ConfigurationFile => Build.RootDirectory / ".github" / "workflows" / $"{_name}.yml";
    public override IEnumerable<AbsolutePath> GeneratedFiles => new[] { ConfigurationFile };

    public override IEnumerable<string> RelevantTargetNames => InvokedTargets;
    public override IEnumerable<string> IrrelevantTargetNames => new string[0];

    public GitHubActionsTrigger[] On { get; set; } = new GitHubActionsTrigger[0];
    public string[] OnPushBranches { get; set; } = new string[0];
    public string[] OnPushBranchesIgnore { get; set; } = new string[0];
    public string[] OnPushTags { get; set; } = new string[0];
    public string[] OnPushTagsIgnore { get; set; } = new string[0];
    public string[] OnPushIncludePaths { get; set; } = new string[0];
    public string[] OnPushExcludePaths { get; set; } = new string[0];
    public string[] OnPullRequestBranches { get; set; } = new string[0];
    public string[] OnPullRequestTags { get; set; } = new string[0];
    public string[] OnPullRequestIncludePaths { get; set; } = new string[0];
    public string[] OnPullRequestExcludePaths { get; set; } = new string[0];
    [Obsolete($"Use [{nameof(GitHubActionsInputAttribute)}] instead. Removed in 2027.x.x.")]
    public string[] OnWorkflowDispatchOptionalInputs { get; set; } = new string[0];
    [Obsolete($"Use [{nameof(GitHubActionsInputAttribute)}] instead. Removed in 2027.x.x.")]
    public string[] OnWorkflowDispatchRequiredInputs { get; set; } = new string[0];
    public string OnCronSchedule { get; set; }

    /// <summary>
    /// Workflow-level environment variables, each entry in <c>KEY: value</c> form. Emitted once as a
    /// top-level <c>env:</c> block (after <c>on:</c>) and inherited by every job and step — including
    /// non-run steps such as checkout, cache, and artifact upload, which per-step env can't reach.
    /// <para/>
    /// Named <c>Env</c> rather than <c>Environment</c> to avoid confusion with the deployment
    /// <c>environment:</c> keyword exposed via <see cref="EnvironmentName"/>.
    /// </summary>
    public string[] Env { get; set; } = new string[0];

    public string[] ImportSecrets { get; set; } = new string[0];
    public bool EnableGitHubToken { get; set; }
    public GitHubActionsPermissions[] WritePermissions { get; set; } = new GitHubActionsPermissions[0];
    public GitHubActionsPermissions[] ReadPermissions { get; set; } = new GitHubActionsPermissions[0];

    public string[] CacheIncludePatterns { get; set; } = { ".fallout/temp", "~/.nuget/packages" };
    public string[] CacheExcludePatterns { get; set; } = new string[0];
    public string[] CacheKeyFiles { get; set; } = { "**/global.json", "**/*.csproj", "**/Directory.Packages.props" };

    public bool PublishArtifacts { get; set; } = true;
    public string PublishCondition { get; set; }

    public int TimeoutMinutes { get; set; }

    public string EnvironmentName { get; set; }
    public string EnvironmentUrl { get; set; }

    public string ConcurrencyGroup { get; set; }
    public bool ConcurrencyCancelInProgress { get; set; }

    public string JobConcurrencyGroup { get; set; }
    public bool JobConcurrencyCancelInProgress { get; set; }

    /// <summary>
    /// Pins the shell for every <c>run:</c> step via a workflow-level <c>defaults.run.shell</c> block,
    /// so cross-platform matrix jobs use one consistent shell instead of the per-OS default (<c>bash</c>
    /// on Linux/macOS, <c>pwsh</c> on Windows). Accepts any value GitHub allows — a built-in (<c>bash</c>,
    /// <c>pwsh</c>, <c>sh</c>, <c>cmd</c>, <c>powershell</c>, <c>python</c>) or a custom <c>command {0}</c>
    /// template. Unset or whitespace-only emits no <c>defaults:</c> block.
    /// </summary>
    public string DefaultShell { get; set; }

    public string[] InvokedTargets { get; set; } = new string[0];

    /// <summary>
    /// Runner labels emitted verbatim as <c>runs-on: [label1, label2, ...]</c>, for selecting a
    /// self-hosted runner pool by OS/arch/capability (e.g. <c>["self-hosted", "linux", "x64"]</c>).
    /// <para/>
    /// When non-empty this replaces the <c>runs-on:</c> image for the job and requires exactly one
    /// image (no matrix). The constructor-mandated <see cref="GitHubActionsImage"/> is then ignored
    /// for <c>runs-on:</c> and only names the job.
    /// </summary>
    public string[] RunsOnLabels { get; set; } = new string[0];

    public GitHubActionsSubmodules Submodules
    {
        set => _submodules = value;
        get => throw new NotSupportedException();
    }

    public bool Lfs
    {
        set => _lfs = value;
        get => throw new NotSupportedException();
    }

    public uint FetchDepth
    {
        set => _fetchDepth = value;
        get => throw new NotSupportedException();
    }

    public bool Progress
    {
        set => _progress = value;
        get => throw new NotSupportedException();
    }

    public string Filter
    {
        set => _filter = value;
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// Forwarded to <c>actions/checkout</c>'s <c>ref</c> input. Set to
    /// <c>${{ github.head_ref }}</c> on PR-triggered workflows that need <c>.git/HEAD</c>
    /// to stay attached (e.g. ones that read the current branch via GitRepository).
    /// <para/>
    /// When set, the generator also emits <c>repository: ${{ github.event.pull_request.head.repo.full_name || github.repository }}</c>
    /// so cross-repo PRs (from forks) resolve the branch name on the fork instead of failing
    /// on origin. The fallback to <c>github.repository</c> keeps push-triggered workflows
    /// (where there's no PR context) working unchanged.
    /// </summary>
    public string CheckoutRef
    {
        set => _ref = value;
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// Extra <c>actions/checkout</c> inputs, emitted verbatim inside the step's <c>with:</c> block after
    /// the typed keys (<c>submodules</c>, <c>lfs</c>, <c>fetch-depth</c>, <c>progress</c>, <c>filter</c>,
    /// <c>ref</c>/<c>repository</c>). An escape hatch for inputs the typed knobs don't cover — <c>token</c>,
    /// <c>ssh-key</c>, <c>path</c>, <c>clean</c>, <c>persist-credentials</c>, <c>sparse-checkout</c>,
    /// <c>set-safe-directory</c>.
    /// <para/>
    /// Each entry is one raw line — passed through unvalidated, so the caller owns correct YAML. Multi-line
    /// block scalars work by supplying the key (e.g. <c>sparse-checkout: |</c>) and each continuation line
    /// as separate entries, with the caller's own indentation preserved. Empty emits nothing.
    /// </summary>
    public string[] CheckoutWith { get; set; } = new string[0];

    public override CustomFileWriter CreateWriter(StreamWriter streamWriter)
    {
        return new CustomFileWriter(streamWriter, indentationFactor: 2, commentPrefix: "#");
    }

    public override ConfigurationEntity GetConfiguration(IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        foreach (var variable in Env)
        {
            Assert.True(variable != null, $"'{nameof(Env)}' entries must not be null; expected 'KEY: value'");

            var separatorIndex = variable.IndexOf(':');
            Assert.True(separatorIndex > 0,
                $"'{nameof(Env)}' entry '{variable}' must be in 'KEY: value' form with a non-empty key");
            Assert.True(!variable.Substring(startIndex: 0, separatorIndex).Any(char.IsWhiteSpace),
                $"'{nameof(Env)}' entry '{variable}' has whitespace in its key; expected 'KEY: value'");
            Assert.True(separatorIndex == variable.Length - 1 || char.IsWhiteSpace(variable[separatorIndex + 1]),
                $"'{nameof(Env)}' entry '{variable}' must have a space after the key's colon; expected 'KEY: value'");
        }

        ValidateWorkflowDispatchInputs();

        var configuration = new GitHubActionsConfiguration
                            {
                                Name = _name,
                                ShortTriggers = On,
                                DetailedTriggers = GetTriggers().ToArray(),
                                Env = Env,
                                Permissions = WritePermissions.Select(x => (x, "write"))
                                    .Concat(ReadPermissions.Select(x => (x, "read"))).ToArray(),
                                ConcurrencyGroup = ConcurrencyGroup,
                                ConcurrencyCancelInProgress = ConcurrencyCancelInProgress,
                                DefaultShell = DefaultShell,
                                Jobs = _images.Select(x => GetJobs(x, relevantTargets)).ToArray()
                            };

        Assert.True(configuration.ShortTriggers.Length == 0 || configuration.DetailedTriggers.Length == 0,
            $"Workflows can only define either shorthand '{nameof(On)}' or '{nameof(On)}*' triggers");
        Assert.True(configuration.ShortTriggers.Length > 0 || configuration.DetailedTriggers.Length > 0,
            $"Workflows must define either shorthand '{nameof(On)}' or '{nameof(On)}*' triggers");
        Assert.True(RunsOnLabels.Length == 0 || _images.Length == 1,
            $"Cannot use '{nameof(RunsOnLabels)}' with multiple images; labels resolve a single job's runner");
        Assert.True(RunsOnLabels.All(x => !x.IsNullOrWhiteSpace()),
            $"'{nameof(RunsOnLabels)}' entries must not be null, empty, or whitespace");

        return configuration;
    }

    protected virtual GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        return new GitHubActionsJob
               {
                   Name = image.GetValue().Replace(".", "_"),
                   RunsOnLabels = RunsOnLabels,
                   EnvironmentName = EnvironmentName,
                   EnvironmentUrl = EnvironmentUrl,
                   Steps = GetSteps(relevantTargets).ToArray(),
                   Image = image,
                   TimeoutMinutes = TimeoutMinutes,
                   ConcurrencyGroup = JobConcurrencyGroup,
                   ConcurrencyCancelInProgress = JobConcurrencyCancelInProgress
               };
    }

    private IEnumerable<GitHubActionsStep> GetSteps(IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        yield return new GitHubActionsCheckoutStep
                     {
                         Submodules = _submodules,
                         Lfs = _lfs,
                         FetchDepth = _fetchDepth,
                         Progress = _progress,
                         Filter = _filter,
                         Ref = _ref,
                         CheckoutWith = CheckoutWith
                     };

        if (CacheKeyFiles.Any())
        {
            yield return new GitHubActionsCacheStep
                         {
                             IncludePatterns = CacheIncludePatterns,
                             ExcludePatterns = CacheExcludePatterns,
                             KeyFiles = CacheKeyFiles
                         };
        }

        yield return new GitHubActionsRunStep
                     {
                         InvokedTargets = InvokedTargets,
                         Imports = GetImports().ToDictionary(x => x.Key, x => x.Value)
                     };

        if (PublishArtifacts)
        {
            var artifacts = relevantTargets
                .SelectMany(x => x.ArtifactProducts)
                .Select(x => (AbsolutePath)x)
                // TODO: https://github.com/actions/upload-artifact/issues/11
                .Select(x => x.DescendantsAndSelf(y => y.Parent).FirstOrDefault(y => !y.ToString().ContainsOrdinalIgnoreCase("*")))
                .Distinct().ToList();

            foreach (var artifact in artifacts)
            {
                yield return new GitHubActionsArtifactStep
                             {
                                 Name = artifact.ToString().TrimStart(artifact.Parent.ToString()).TrimStart('/', '\\'),
                                 Path = Build.RootDirectory.GetUnixRelativePathTo(artifact),
                                 Condition = PublishCondition
                             };
            }
        }
    }

    protected virtual IEnumerable<(string Key, string Value)> GetImports()
    {
        foreach (var input in GetWorkflowDispatchInputs())
            yield return (input.Name, $"${{{{ github.event.inputs.{input.Name} }}}}");

        static string GetSecretValue(string secret)
            => $"${{{{ secrets.{secret.SplitCamelHumpsWithKnownWords().JoinUnderscore().ToUpperInvariant()} }}}}";

        foreach (var secret in ImportSecrets)
            yield return (secret, GetSecretValue(secret));

        if (EnableGitHubToken)
            yield return ("GITHUB_TOKEN", GetSecretValue("GITHUB_TOKEN"));
    }

    protected virtual IEnumerable<GitHubActionsDetailedTrigger> GetTriggers()
    {
        if (OnPushBranches.Length > 0 ||
            OnPushBranchesIgnore.Length > 0 ||
            OnPushTags.Length > 0 ||
            OnPushTagsIgnore.Length > 0 ||
            OnPushIncludePaths.Length > 0 ||
            OnPushExcludePaths.Length > 0)
        {
            Assert.True(
                OnPushBranches.Length == 0 && OnPushTags.Length == 0 || OnPushBranchesIgnore.Length == 0 && OnPushTagsIgnore.Length == 0,
                $"Cannot use {nameof(OnPushBranches)}/{nameof(OnPushTags)} and {nameof(OnPushBranchesIgnore)}/{nameof(OnPushTagsIgnore)} in combination");

            yield return new GitHubActionsVcsTrigger
                         {
                             Kind = GitHubActionsTrigger.Push,
                             Branches = OnPushBranches,
                             BranchesIgnore = OnPushBranchesIgnore,
                             Tags = OnPushTags,
                             TagsIgnore = OnPushTagsIgnore,
                             IncludePaths = OnPushIncludePaths,
                             ExcludePaths = OnPushExcludePaths
                         };
        }

        if (OnPullRequestBranches.Length > 0 ||
            OnPullRequestTags.Length > 0 ||
            OnPullRequestIncludePaths.Length > 0 ||
            OnPullRequestExcludePaths.Length > 0)
        {
            yield return new GitHubActionsVcsTrigger
                         {
                             Kind = GitHubActionsTrigger.PullRequest,
                             Branches = OnPullRequestBranches,
                             BranchesIgnore = new string[0],
                             Tags = OnPullRequestTags,
                             TagsIgnore = new string[0],
                             IncludePaths = OnPullRequestIncludePaths,
                             ExcludePaths = OnPullRequestExcludePaths
                         };
        }

        var dispatchInputs = GetWorkflowDispatchInputs().ToArray();
        if (dispatchInputs.Length > 0)
            yield return new GitHubActionsWorkflowDispatchTrigger { Inputs = dispatchInputs };

        if (OnCronSchedule != null)
            yield return new GitHubActionsScheduledTrigger { Cron = OnCronSchedule };
    }

    // The typed [GitHubActionsInput] attributes declared on the build class. Overridable so tests can
    // inject inputs without static class-level attributes (mirrors the GetJobs/GetImports/GetTriggers seams).
    protected virtual IEnumerable<GitHubActionsInputAttribute> DeclaredInputs
        => Build.GetType().GetCustomAttributes<GitHubActionsInputAttribute>();

    // The names of every [GitHubActions] workflow declared on the build class — the valid targets for an
    // input's Workflows scope. The current workflow is always among them in production.
    protected virtual ISet<string> DeclaredWorkflowNames
        => Build.GetType().GetCustomAttributes<GitHubActionsAttribute>().Select(x => x.IdPostfix).ToHashSet();

    private IEnumerable<GitHubActionsWorkflowDispatchInput> GetWorkflowDispatchInputs()
    {
        // legacy arrays first → untyped string inputs, preserving the existing ordering and output
#pragma warning disable CS0618 // deliberate bridge for the obsolete legacy arrays
        foreach (var input in OnWorkflowDispatchOptionalInputs)
            yield return new GitHubActionsWorkflowDispatchInput { Name = input, Required = false };
        foreach (var input in OnWorkflowDispatchRequiredInputs)
            yield return new GitHubActionsWorkflowDispatchInput { Name = input, Required = true };
#pragma warning restore CS0618

        foreach (var input in DeclaredInputs.Where(x => x.Workflows.Length == 0 || x.Workflows.Contains(_name)))
            yield return new GitHubActionsWorkflowDispatchInput
                         {
                             Name = input.Name,
                             Type = input.Type,
                             Required = input.Required,
                             Default = input.Default,
                             Options = input.Options,
                             Description = input.Description
                         };
    }

    private void ValidateWorkflowDispatchInputs()
    {
        var declaredWorkflows = DeclaredWorkflowNames;

        foreach (var input in DeclaredInputs)
        {
            if (input.Type == GitHubActionsInputType.Choice)
                Assert.True(input.Options.Length > 0,
                    $"'{input.Name}' is a choice input and requires non-empty '{nameof(GitHubActionsInputAttribute.Options)}'");
            else
                Assert.True(input.Options.Length == 0,
                    $"'{input.Name}' sets '{nameof(GitHubActionsInputAttribute.Options)}' but its type is not '{nameof(GitHubActionsInputType.Choice)}'");

            if (input.Default != null)
            {
                if (input.Type == GitHubActionsInputType.Choice)
                    Assert.True(input.Options.Contains(input.Default),
                        $"'{input.Name}' default '{input.Default}' is not one of its options");
                if (input.Type == GitHubActionsInputType.Number)
                    Assert.True(double.TryParse(input.Default, NumberStyles.Any, CultureInfo.InvariantCulture, out _),
                        $"'{input.Name}' default '{input.Default}' is not a valid number");
                if (input.Type == GitHubActionsInputType.Boolean)
                    Assert.True(input.Default is "true" or "false",
                        $"'{input.Name}' default '{input.Default}' must be 'true' or 'false'");
            }

            foreach (var workflow in input.Workflows)
                Assert.True(declaredWorkflows.Contains(workflow),
                    $"'{input.Name}' targets unknown workflow '{workflow}'");
        }

        var inputs = GetWorkflowDispatchInputs().ToList();
        foreach (var input in inputs)
            Assert.True(!input.Name.IsNullOrWhiteSpace(),
                $"workflow_dispatch input names must be non-empty in workflow '{_name}'");

        var names = inputs.Select(x => x.Name).ToList();
        Assert.True(names.Count == names.Distinct().Count(),
            $"Duplicate workflow_dispatch input names in workflow '{_name}'");
    }
}
