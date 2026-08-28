// Generated from https://github.com/Fallout-build/Fallout/blob/develop/src/Fallout.Common/Tools/PackageGuard/PackageGuard.json

using Fallout.Common;
using Fallout.Common.Tooling;
using Fallout.Common.Tools;
using Fallout.Common.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Fallout.Common.Tools.PackageGuard;

/// <summary><p>PackageGuard scans the NuGet, npm, Yarn and pnpm dependencies of a solution or project against an allow- or deny-list, so you can control which open-source licenses and package versions are acceptable. It can also print a colored risk summary, generate HTML/SARIF risk reports, and produce a Software Bill of Materials.</p><p>For more details, visit the <a href="https://github.com/dennisdoomen/packageguard">official website</a>.</p></summary>
[ExcludeFromCodeCoverage]
[NuGetTool(Id = PackageId, Executable = PackageExecutable)]
public partial class PackageGuardTasks : ToolTasks, IRequireNuGetPackage
{
    public static string PackageGuardPath { get => new PackageGuardTasks().GetToolPathInternal(); set => new PackageGuardTasks().SetToolPath(value); }
    public const string PackageId = "PackageGuard";
    public const string PackageExecutable = "PackageGuard.dll|PackageGuard.exe";
    /// <summary><p>PackageGuard scans the NuGet, npm, Yarn and pnpm dependencies of a solution or project against an allow- or deny-list, so you can control which open-source licenses and package versions are acceptable. It can also print a colored risk summary, generate HTML/SARIF risk reports, and produce a Software Bill of Materials.</p><p>For more details, visit the <a href="https://github.com/dennisdoomen/packageguard">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> PackageGuard(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new PackageGuardTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>PackageGuard scans the NuGet, npm, Yarn and pnpm dependencies of a solution or project against an allow- or deny-list, so you can control which open-source licenses and package versions are acceptable. It can also print a colored risk summary, generate HTML/SARIF risk reports, and produce a Software Bill of Materials.</p><p>For more details, visit the <a href="https://github.com/dennisdoomen/packageguard">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/Fallout-build/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>&lt;projectPath&gt;</c> via <see cref="PackageGuardSettings.ProjectPath"/></li><li><c>--cache-file-path</c> via <see cref="PackageGuardSettings.CacheFilePath"/></li><li><c>--config-path</c> via <see cref="PackageGuardSettings.ConfigPath"/></li><li><c>--force-restore</c> via <see cref="PackageGuardSettings.ForceRestore"/></li><li><c>--github-api-key</c> via <see cref="PackageGuardSettings.GitHubApiKey"/></li><li><c>--ignore-violations</c> via <see cref="PackageGuardSettings.IgnoreViolations"/></li><li><c>--npm</c> via <see cref="PackageGuardSettings.NpmPackageManager"/></li><li><c>--npm-exe-path</c> via <see cref="PackageGuardSettings.NpmExePath"/></li><li><c>--nuget</c> via <see cref="PackageGuardSettings.ScanNuGet"/></li><li><c>--refresh-risk-cache</c> via <see cref="PackageGuardSettings.RefreshRiskCache"/></li><li><c>--report-risk</c> via <see cref="PackageGuardSettings.ReportRisk"/></li><li><c>--restore-interactive</c> via <see cref="PackageGuardSettings.Interactive"/></li><li><c>--risk-cache-max-age-hours</c> via <see cref="PackageGuardSettings.RiskCacheMaxAgeHours"/></li><li><c>--sbom</c> via <see cref="PackageGuardSettings.Sbom"/></li><li><c>--sbom-output</c> via <see cref="PackageGuardSettings.SbomOutput"/></li><li><c>--skip-restore</c> via <see cref="PackageGuardSettings.SkipRestore"/></li><li><c>--use-caching</c> via <see cref="PackageGuardSettings.UseCaching"/></li><li><c>--verbose</c> via <see cref="PackageGuardSettings.Verbose"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> PackageGuard(PackageGuardSettings options = null) => new PackageGuardTasks().Run<PackageGuardSettings>(options);
    /// <inheritdoc cref="PackageGuardTasks.PackageGuard(Fallout.Common.Tools.PackageGuard.PackageGuardSettings)"/>
    public static IReadOnlyCollection<Output> PackageGuard(Configure<PackageGuardSettings> configurator) => new PackageGuardTasks().Run<PackageGuardSettings>(configurator.Invoke(new PackageGuardSettings()));
    /// <inheritdoc cref="PackageGuardTasks.PackageGuard(Fallout.Common.Tools.PackageGuard.PackageGuardSettings)"/>
    public static IEnumerable<(PackageGuardSettings Settings, IReadOnlyCollection<Output> Output)> PackageGuard(CombinatorialConfigure<PackageGuardSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(PackageGuard, degreeOfParallelism, completeOnFailure);
}
#region PackageGuardSettings
/// <inheritdoc cref="PackageGuardTasks.PackageGuard(Fallout.Common.Tools.PackageGuard.PackageGuardSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(PackageGuardTasks), Command = nameof(PackageGuardTasks.PackageGuard))]
public partial class PackageGuardSettings : ToolOptions, IToolOptionsWithFramework
{
    /// <summary>The path to a directory containing a <c>.sln</c>/<c>.slnx</c> file and/or a <c>package.json</c>, a specific <c>.sln</c>/<c>.slnx</c> file, a specific <c>.csproj</c> file, or a specific <c>package.json</c>. Defaults to the current working directory.</summary>
    [Argument(Format = "{value}", Position = 1)] public string ProjectPath => Get<string>(() => ProjectPath);
    /// <summary>The path to the configuration file. Defaults to hierarchical discovery of <c>packageguard.config.json</c> or <c>.packageguard/config.json</c> files starting from the solution directory.</summary>
    [Argument(Format = "--config-path {value}")] public string ConfigPath => Get<string>(() => ConfigPath);
    /// <summary>Enables interactive mode for <c>dotnet restore</c>. Enabled by default.</summary>
    [Argument(Format = "--restore-interactive")] public bool? Interactive => Get<bool?>(() => Interactive);
    /// <summary>Don't fail the analysis if any violations are found.</summary>
    [Argument(Format = "--ignore-violations")] public bool? IgnoreViolations => Get<bool?>(() => IgnoreViolations);
    /// <summary>Force restoring the NuGet dependencies, even if the lockfile is up-to-date.</summary>
    [Argument(Format = "--force-restore")] public bool? ForceRestore => Get<bool?>(() => ForceRestore);
    /// <summary>Prevent the restore operation from running, even if the lock file is missing or out-of-date.</summary>
    [Argument(Format = "--skip-restore")] public bool? SkipRestore => Get<bool?>(() => SkipRestore);
    /// <summary>GitHub API key to use for fetching package licenses. If not specified, you may run into GitHub's rate limiting issues.</summary>
    [Argument(Format = "--github-api-key {value}", Secret = true)] public string GitHubApiKey => Get<string>(() => GitHubApiKey);
    /// <summary>Maintains a cache of the package information to speed up future analysis.</summary>
    [Argument(Format = "--use-caching")] public bool? UseCaching => Get<bool?>(() => UseCaching);
    /// <summary>Overrides the file path where analysis data is cached. Defaults to a <c>.packageguard/cache.bin</c> file relative to the working directory.</summary>
    [Argument(Format = "--cache-file-path {value}")] public string CacheFilePath => Get<string>(() => CacheFilePath);
    /// <summary>Force <c>--report-risk</c> to rebuild risk-related package data instead of reusing cached risk entries.</summary>
    [Argument(Format = "--refresh-risk-cache")] public bool? RefreshRiskCache => Get<bool?>(() => RefreshRiskCache);
    /// <summary>Maximum age in hours for cached risk-related package data before <c>--report-risk</c> refreshes it. Defaults to <c>24</c>.</summary>
    [Argument(Format = "--risk-cache-max-age-hours {value}")] public int? RiskCacheMaxAgeHours => Get<int?>(() => RiskCacheMaxAgeHours);
    /// <summary>Explicitly enable scanning for <c>.csproj</c>, <c>.sln</c> or <c>.slnx</c> files. Enabled by default.</summary>
    [Argument(Format = "--nuget")] public bool? ScanNuGet => Get<bool?>(() => ScanNuGet);
    /// <summary>Explicitly specify the package manager to use (<c>Npm</c>, <c>Yarn</c>, <c>Pnpm</c>), or <c>None</c> to disable npm scanning entirely. If not specified, it is detected automatically.</summary>
    [Argument(Format = "--npm {value}")] public NpmPackageManager NpmPackageManager => Get<NpmPackageManager>(() => NpmPackageManager);
    /// <summary>The path to the npm, yarn or pnpm executable. If not specified, the system PATH is used.</summary>
    [Argument(Format = "--npm-exe-path {value}")] public string NpmExePath => Get<string>(() => NpmExePath);
    /// <summary>Show a colored risk summary in the console and generate detailed HTML/SARIF risk reports.</summary>
    [Argument(Format = "--report-risk")] public bool? ReportRisk => Get<bool?>(() => ReportRisk);
    /// <summary>Enable verbose (debug-level) logging output. Combine with <c>--report-risk</c> to see individual HTTP calls to GitHub, OSV, and npm registries.</summary>
    [Argument(Format = "--verbose")] public bool? Verbose => Get<bool?>(() => Verbose);
    /// <summary>Generate a Software Bill of Materials for the resolved dependency graph, in the given format. Requires <c>--sbom-output</c>.</summary>
    [Argument(Format = "--sbom {value}")] public SbomFormat Sbom => Get<SbomFormat>(() => Sbom);
    /// <summary>The output file path for the generated SBOM. Required when <c>--sbom</c> is specified.</summary>
    [Argument(Format = "--sbom-output {value}")] public string SbomOutput => Get<string>(() => SbomOutput);
}
#endregion
#region PackageGuardSettingsExtensions
/// <inheritdoc cref="PackageGuardTasks.PackageGuard(Fallout.Common.Tools.PackageGuard.PackageGuardSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class PackageGuardSettingsExtensions
{
    #region ProjectPath
    /// <inheritdoc cref="PackageGuardSettings.ProjectPath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ProjectPath))]
    public static T SetProjectPath<T>(this T o, string v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ProjectPath, v));
    /// <inheritdoc cref="PackageGuardSettings.ProjectPath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ProjectPath))]
    public static T ResetProjectPath<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.ProjectPath));
    #endregion
    #region ConfigPath
    /// <inheritdoc cref="PackageGuardSettings.ConfigPath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ConfigPath))]
    public static T SetConfigPath<T>(this T o, string v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ConfigPath, v));
    /// <inheritdoc cref="PackageGuardSettings.ConfigPath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ConfigPath))]
    public static T ResetConfigPath<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.ConfigPath));
    #endregion
    #region Interactive
    /// <inheritdoc cref="PackageGuardSettings.Interactive"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Interactive))]
    public static T SetInteractive<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Interactive, v));
    /// <inheritdoc cref="PackageGuardSettings.Interactive"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Interactive))]
    public static T ResetInteractive<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.Interactive));
    /// <inheritdoc cref="PackageGuardSettings.Interactive"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Interactive))]
    public static T EnableInteractive<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Interactive, true));
    /// <inheritdoc cref="PackageGuardSettings.Interactive"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Interactive))]
    public static T DisableInteractive<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Interactive, false));
    /// <inheritdoc cref="PackageGuardSettings.Interactive"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Interactive))]
    public static T ToggleInteractive<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Interactive, !o.Interactive));
    #endregion
    #region IgnoreViolations
    /// <inheritdoc cref="PackageGuardSettings.IgnoreViolations"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.IgnoreViolations))]
    public static T SetIgnoreViolations<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.IgnoreViolations, v));
    /// <inheritdoc cref="PackageGuardSettings.IgnoreViolations"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.IgnoreViolations))]
    public static T ResetIgnoreViolations<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.IgnoreViolations));
    /// <inheritdoc cref="PackageGuardSettings.IgnoreViolations"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.IgnoreViolations))]
    public static T EnableIgnoreViolations<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.IgnoreViolations, true));
    /// <inheritdoc cref="PackageGuardSettings.IgnoreViolations"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.IgnoreViolations))]
    public static T DisableIgnoreViolations<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.IgnoreViolations, false));
    /// <inheritdoc cref="PackageGuardSettings.IgnoreViolations"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.IgnoreViolations))]
    public static T ToggleIgnoreViolations<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.IgnoreViolations, !o.IgnoreViolations));
    #endregion
    #region ForceRestore
    /// <inheritdoc cref="PackageGuardSettings.ForceRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ForceRestore))]
    public static T SetForceRestore<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ForceRestore, v));
    /// <inheritdoc cref="PackageGuardSettings.ForceRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ForceRestore))]
    public static T ResetForceRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.ForceRestore));
    /// <inheritdoc cref="PackageGuardSettings.ForceRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ForceRestore))]
    public static T EnableForceRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ForceRestore, true));
    /// <inheritdoc cref="PackageGuardSettings.ForceRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ForceRestore))]
    public static T DisableForceRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ForceRestore, false));
    /// <inheritdoc cref="PackageGuardSettings.ForceRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ForceRestore))]
    public static T ToggleForceRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ForceRestore, !o.ForceRestore));
    #endregion
    #region SkipRestore
    /// <inheritdoc cref="PackageGuardSettings.SkipRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.SkipRestore))]
    public static T SetSkipRestore<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.SkipRestore, v));
    /// <inheritdoc cref="PackageGuardSettings.SkipRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.SkipRestore))]
    public static T ResetSkipRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.SkipRestore));
    /// <inheritdoc cref="PackageGuardSettings.SkipRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.SkipRestore))]
    public static T EnableSkipRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.SkipRestore, true));
    /// <inheritdoc cref="PackageGuardSettings.SkipRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.SkipRestore))]
    public static T DisableSkipRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.SkipRestore, false));
    /// <inheritdoc cref="PackageGuardSettings.SkipRestore"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.SkipRestore))]
    public static T ToggleSkipRestore<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.SkipRestore, !o.SkipRestore));
    #endregion
    #region GitHubApiKey
    /// <inheritdoc cref="PackageGuardSettings.GitHubApiKey"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.GitHubApiKey))]
    public static T SetGitHubApiKey<T>(this T o, [Secret] string v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.GitHubApiKey, v));
    /// <inheritdoc cref="PackageGuardSettings.GitHubApiKey"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.GitHubApiKey))]
    public static T ResetGitHubApiKey<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.GitHubApiKey));
    #endregion
    #region UseCaching
    /// <inheritdoc cref="PackageGuardSettings.UseCaching"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.UseCaching))]
    public static T SetUseCaching<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.UseCaching, v));
    /// <inheritdoc cref="PackageGuardSettings.UseCaching"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.UseCaching))]
    public static T ResetUseCaching<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.UseCaching));
    /// <inheritdoc cref="PackageGuardSettings.UseCaching"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.UseCaching))]
    public static T EnableUseCaching<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.UseCaching, true));
    /// <inheritdoc cref="PackageGuardSettings.UseCaching"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.UseCaching))]
    public static T DisableUseCaching<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.UseCaching, false));
    /// <inheritdoc cref="PackageGuardSettings.UseCaching"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.UseCaching))]
    public static T ToggleUseCaching<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.UseCaching, !o.UseCaching));
    #endregion
    #region CacheFilePath
    /// <inheritdoc cref="PackageGuardSettings.CacheFilePath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.CacheFilePath))]
    public static T SetCacheFilePath<T>(this T o, string v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.CacheFilePath, v));
    /// <inheritdoc cref="PackageGuardSettings.CacheFilePath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.CacheFilePath))]
    public static T ResetCacheFilePath<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.CacheFilePath));
    #endregion
    #region RefreshRiskCache
    /// <inheritdoc cref="PackageGuardSettings.RefreshRiskCache"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.RefreshRiskCache))]
    public static T SetRefreshRiskCache<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.RefreshRiskCache, v));
    /// <inheritdoc cref="PackageGuardSettings.RefreshRiskCache"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.RefreshRiskCache))]
    public static T ResetRefreshRiskCache<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.RefreshRiskCache));
    /// <inheritdoc cref="PackageGuardSettings.RefreshRiskCache"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.RefreshRiskCache))]
    public static T EnableRefreshRiskCache<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.RefreshRiskCache, true));
    /// <inheritdoc cref="PackageGuardSettings.RefreshRiskCache"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.RefreshRiskCache))]
    public static T DisableRefreshRiskCache<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.RefreshRiskCache, false));
    /// <inheritdoc cref="PackageGuardSettings.RefreshRiskCache"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.RefreshRiskCache))]
    public static T ToggleRefreshRiskCache<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.RefreshRiskCache, !o.RefreshRiskCache));
    #endregion
    #region RiskCacheMaxAgeHours
    /// <inheritdoc cref="PackageGuardSettings.RiskCacheMaxAgeHours"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.RiskCacheMaxAgeHours))]
    public static T SetRiskCacheMaxAgeHours<T>(this T o, int? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.RiskCacheMaxAgeHours, v));
    /// <inheritdoc cref="PackageGuardSettings.RiskCacheMaxAgeHours"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.RiskCacheMaxAgeHours))]
    public static T ResetRiskCacheMaxAgeHours<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.RiskCacheMaxAgeHours));
    #endregion
    #region ScanNuGet
    /// <inheritdoc cref="PackageGuardSettings.ScanNuGet"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ScanNuGet))]
    public static T SetScanNuGet<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ScanNuGet, v));
    /// <inheritdoc cref="PackageGuardSettings.ScanNuGet"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ScanNuGet))]
    public static T ResetScanNuGet<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.ScanNuGet));
    /// <inheritdoc cref="PackageGuardSettings.ScanNuGet"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ScanNuGet))]
    public static T EnableScanNuGet<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ScanNuGet, true));
    /// <inheritdoc cref="PackageGuardSettings.ScanNuGet"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ScanNuGet))]
    public static T DisableScanNuGet<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ScanNuGet, false));
    /// <inheritdoc cref="PackageGuardSettings.ScanNuGet"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ScanNuGet))]
    public static T ToggleScanNuGet<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ScanNuGet, !o.ScanNuGet));
    #endregion
    #region NpmPackageManager
    /// <inheritdoc cref="PackageGuardSettings.NpmPackageManager"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.NpmPackageManager))]
    public static T SetNpmPackageManager<T>(this T o, NpmPackageManager v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.NpmPackageManager, v));
    /// <inheritdoc cref="PackageGuardSettings.NpmPackageManager"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.NpmPackageManager))]
    public static T ResetNpmPackageManager<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.NpmPackageManager));
    #endregion
    #region NpmExePath
    /// <inheritdoc cref="PackageGuardSettings.NpmExePath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.NpmExePath))]
    public static T SetNpmExePath<T>(this T o, string v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.NpmExePath, v));
    /// <inheritdoc cref="PackageGuardSettings.NpmExePath"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.NpmExePath))]
    public static T ResetNpmExePath<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.NpmExePath));
    #endregion
    #region ReportRisk
    /// <inheritdoc cref="PackageGuardSettings.ReportRisk"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ReportRisk))]
    public static T SetReportRisk<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ReportRisk, v));
    /// <inheritdoc cref="PackageGuardSettings.ReportRisk"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ReportRisk))]
    public static T ResetReportRisk<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.ReportRisk));
    /// <inheritdoc cref="PackageGuardSettings.ReportRisk"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ReportRisk))]
    public static T EnableReportRisk<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ReportRisk, true));
    /// <inheritdoc cref="PackageGuardSettings.ReportRisk"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ReportRisk))]
    public static T DisableReportRisk<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ReportRisk, false));
    /// <inheritdoc cref="PackageGuardSettings.ReportRisk"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.ReportRisk))]
    public static T ToggleReportRisk<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.ReportRisk, !o.ReportRisk));
    #endregion
    #region Verbose
    /// <inheritdoc cref="PackageGuardSettings.Verbose"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Verbose))]
    public static T SetVerbose<T>(this T o, bool? v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Verbose, v));
    /// <inheritdoc cref="PackageGuardSettings.Verbose"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Verbose))]
    public static T ResetVerbose<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.Verbose));
    /// <inheritdoc cref="PackageGuardSettings.Verbose"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Verbose))]
    public static T EnableVerbose<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Verbose, true));
    /// <inheritdoc cref="PackageGuardSettings.Verbose"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Verbose))]
    public static T DisableVerbose<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Verbose, false));
    /// <inheritdoc cref="PackageGuardSettings.Verbose"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Verbose))]
    public static T ToggleVerbose<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Verbose, !o.Verbose));
    #endregion
    #region Sbom
    /// <inheritdoc cref="PackageGuardSettings.Sbom"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Sbom))]
    public static T SetSbom<T>(this T o, SbomFormat v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.Sbom, v));
    /// <inheritdoc cref="PackageGuardSettings.Sbom"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.Sbom))]
    public static T ResetSbom<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.Sbom));
    #endregion
    #region SbomOutput
    /// <inheritdoc cref="PackageGuardSettings.SbomOutput"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.SbomOutput))]
    public static T SetSbomOutput<T>(this T o, string v) where T : PackageGuardSettings => o.Modify(b => b.Set(() => o.SbomOutput, v));
    /// <inheritdoc cref="PackageGuardSettings.SbomOutput"/>
    [Builder(Type = typeof(PackageGuardSettings), Property = nameof(PackageGuardSettings.SbomOutput))]
    public static T ResetSbomOutput<T>(this T o) where T : PackageGuardSettings => o.Modify(b => b.Remove(() => o.SbomOutput));
    #endregion
}
#endregion
#region NpmPackageManager
/// <summary>Used within <see cref="PackageGuardTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<NpmPackageManager>))]
public partial class NpmPackageManager : Enumeration
{
    public static NpmPackageManager None = (NpmPackageManager) "None";
    public static NpmPackageManager Npm = (NpmPackageManager) "Npm";
    public static NpmPackageManager Yarn = (NpmPackageManager) "Yarn";
    public static NpmPackageManager Pnpm = (NpmPackageManager) "Pnpm";
    public static implicit operator NpmPackageManager(string value)
    {
        return new NpmPackageManager { Value = value };
    }
}
#endregion
#region SbomFormat
/// <summary>Used within <see cref="PackageGuardTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<SbomFormat>))]
public partial class SbomFormat : Enumeration
{
    public static SbomFormat cyclonedx = (SbomFormat) "cyclonedx";
    public static SbomFormat spdx = (SbomFormat) "spdx";
    public static implicit operator SbomFormat(string value)
    {
        return new SbomFormat { Value = value };
    }
}
#endregion
