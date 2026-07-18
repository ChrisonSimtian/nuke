// Generated from https://github.com/Fallout-build/Fallout/blob/main/src/Fallout.Common/Tools/CMake/CMake.json

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

namespace Fallout.Common.Tools.CMake;

/// <summary><p>The cmake executable is the command-line interface of the cross-platform build system generator CMake.</p><p>For more details, visit the <a href="https://cmake.org/cmake/help/latest/manual/cmake.1.html">official website</a>.</p></summary>
[ExcludeFromCodeCoverage]
[PathTool(Executable = PathExecutable)]
public partial class CMakeTasks : ToolTasks, IRequirePathTool
{
    public static string CMakePath { get => new CMakeTasks().GetToolPathInternal(); set => new CMakeTasks().SetToolPath(value); }
    public const string PathExecutable = "cmake";
    /// <summary><p>The cmake executable is the command-line interface of the cross-platform build system generator CMake.</p><p>For more details, visit the <a href="https://cmake.org/cmake/help/latest/manual/cmake.1.html">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> CMake(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new CMakeTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>Generates a build project using cmake.</p><p>For more details, visit the <a href="https://cmake.org/cmake/help/latest/manual/cmake.1.html">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/Fallout-build/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>--debug-output</c> via <see cref="CMakeGenerateSettings.Debug"/></li><li><c>--fresh</c> via <see cref="CMakeGenerateSettings.Fresh"/></li><li><c>--install-prefix</c> via <see cref="CMakeGenerateSettings.InstallPrefix"/></li><li><c>--log-level</c> via <see cref="CMakeGenerateSettings.LogLevel"/></li><li><c>--trace</c> via <see cref="CMakeGenerateSettings.Trace"/></li><li><c>--trace-expand</c> via <see cref="CMakeGenerateSettings.TraceExpand"/></li><li><c>--warn-uninitialized</c> via <see cref="CMakeGenerateSettings.WarnUninitialized"/></li><li><c>--warn-unused-vars</c> via <see cref="CMakeGenerateSettings.WarnUnusedVars"/></li><li><c>-A</c> via <see cref="CMakeGenerateSettings.Platform"/></li><li><c>-B</c> via <see cref="CMakeGenerateSettings.OutputDirectory"/></li><li><c>-D</c> via <see cref="CMakeGenerateSettings.Define"/></li><li><c>-DCMAKE_TOOLCHAIN_FILE:FILEPATH</c> via <see cref="CMakeGenerateSettings.ToolChain"/></li><li><c>-G</c> via <see cref="CMakeGenerateSettings.Generator"/></li><li><c>-S</c> via <see cref="CMakeGenerateSettings.RootDirectory"/></li><li><c>-T</c> via <see cref="CMakeGenerateSettings.ToolsetSpec"/></li><li><c>-U</c> via <see cref="CMakeGenerateSettings.Undefine"/></li><li><c>-Wdev</c> via <see cref="CMakeGenerateSettings.WarnDev"/></li><li><c>-Wno-dev</c> via <see cref="CMakeGenerateSettings.NoWarnDev"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> CMakeGenerate(CMakeGenerateSettings options = null) => new CMakeTasks().Run<CMakeGenerateSettings>(options);
    /// <inheritdoc cref="CMakeTasks.CMakeGenerate(Fallout.Common.Tools.CMake.CMakeGenerateSettings)"/>
    public static IReadOnlyCollection<Output> CMakeGenerate(Configure<CMakeGenerateSettings> configurator) => new CMakeTasks().Run<CMakeGenerateSettings>(configurator.Invoke(new CMakeGenerateSettings()));
    /// <inheritdoc cref="CMakeTasks.CMakeGenerate(Fallout.Common.Tools.CMake.CMakeGenerateSettings)"/>
    public static IEnumerable<(CMakeGenerateSettings Settings, IReadOnlyCollection<Output> Output)> CMakeGenerate(CombinatorialConfigure<CMakeGenerateSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(CMakeGenerate, degreeOfParallelism, completeOnFailure);
    /// <summary><p>Runs the `cmake --build` command on a project generated with cmake.</p><p>For more details, visit the <a href="https://cmake.org/cmake/help/latest/manual/cmake.1.html">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/Fallout-build/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>&lt;outputDirectory&gt;</c> via <see cref="CMakeBuildSettings.OutputDirectory"/></li><li><c>--</c> via <see cref="CMakeBuildSettings.NativeToolOptions"/></li><li><c>--clean-first</c> via <see cref="CMakeBuildSettings.CleanFirst"/></li><li><c>--config</c> via <see cref="CMakeBuildSettings.Configuration"/></li><li><c>--parallel</c> via <see cref="CMakeBuildSettings.Parallel"/></li><li><c>--target</c> via <see cref="CMakeBuildSettings.Target"/></li><li><c>--verbose</c> via <see cref="CMakeBuildSettings.Verbose"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> CMakeBuild(CMakeBuildSettings options = null) => new CMakeTasks().Run<CMakeBuildSettings>(options);
    /// <inheritdoc cref="CMakeTasks.CMakeBuild(Fallout.Common.Tools.CMake.CMakeBuildSettings)"/>
    public static IReadOnlyCollection<Output> CMakeBuild(Configure<CMakeBuildSettings> configurator) => new CMakeTasks().Run<CMakeBuildSettings>(configurator.Invoke(new CMakeBuildSettings()));
    /// <inheritdoc cref="CMakeTasks.CMakeBuild(Fallout.Common.Tools.CMake.CMakeBuildSettings)"/>
    public static IEnumerable<(CMakeBuildSettings Settings, IReadOnlyCollection<Output> Output)> CMakeBuild(CombinatorialConfigure<CMakeBuildSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(CMakeBuild, degreeOfParallelism, completeOnFailure);
    /// <summary><p>Runs the `cmake --install` command on a project generated with cmake.</p><p>For more details, visit the <a href="https://cmake.org/cmake/help/latest/manual/cmake.1.html">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/Fallout-build/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>&lt;rootDirectory&gt;</c> via <see cref="CMakeInstallSettings.RootDirectory"/></li><li><c>--component</c> via <see cref="CMakeInstallSettings.Component"/></li><li><c>--config</c> via <see cref="CMakeInstallSettings.Configuration"/></li><li><c>--prefix</c> via <see cref="CMakeInstallSettings.OutputDirectory"/></li><li><c>--strip</c> via <see cref="CMakeInstallSettings.Strip"/></li><li><c>--verbose</c> via <see cref="CMakeInstallSettings.Verbose"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> CMakeInstall(CMakeInstallSettings options = null) => new CMakeTasks().Run<CMakeInstallSettings>(options);
    /// <inheritdoc cref="CMakeTasks.CMakeInstall(Fallout.Common.Tools.CMake.CMakeInstallSettings)"/>
    public static IReadOnlyCollection<Output> CMakeInstall(Configure<CMakeInstallSettings> configurator) => new CMakeTasks().Run<CMakeInstallSettings>(configurator.Invoke(new CMakeInstallSettings()));
    /// <inheritdoc cref="CMakeTasks.CMakeInstall(Fallout.Common.Tools.CMake.CMakeInstallSettings)"/>
    public static IEnumerable<(CMakeInstallSettings Settings, IReadOnlyCollection<Output> Output)> CMakeInstall(CombinatorialConfigure<CMakeInstallSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(CMakeInstall, degreeOfParallelism, completeOnFailure);
}
#region CMakeGenerateSettings
/// <inheritdoc cref="CMakeTasks.CMakeGenerate(Fallout.Common.Tools.CMake.CMakeGenerateSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(CMakeTasks), Command = nameof(CMakeTasks.CMakeGenerate))]
public partial class CMakeGenerateSettings : ToolOptions
{
    /// <summary>Specify the build system to generate.</summary>
    [Argument(Format = "-G {value}")] public CMakeGenerator Generator => Get<CMakeGenerator>(() => Generator);
    /// <summary>Specify the platform to build for.</summary>
    [Argument(Format = "-A {value}")] public CMakePlatform Platform => Get<CMakePlatform>(() => Platform);
    /// <summary>Specify the root directory of the 'CMakeLists.txt' file.</summary>
    [Argument(Format = "-S {value}")] public string RootDirectory => Get<string>(() => RootDirectory);
    /// <summary>Specify the configuration to use when running the install script.</summary>
    [Argument(Format = "-B {value}")] public string OutputDirectory => Get<string>(() => OutputDirectory);
    /// <summary>Specify the path to a toolchain file to use.</summary>
    [Argument(Format = "-DCMAKE_TOOLCHAIN_FILE:FILEPATH={value}")] public string ToolChain => Get<string>(() => ToolChain);
    /// <summary>Specify the toolset name if supported by the generator.</summary>
    [Argument(Format = "-T {value}")] public string ToolsetSpec => Get<string>(() => ToolsetSpec);
    /// <summary>Specify the installation prefix to be used in the generated project.</summary>
    [Argument(Format = "--install-prefix {value}")] public string InstallPrefix => Get<string>(() => InstallPrefix);
    /// <summary>Create or update a CMake cache entry, in the form <var>[:<type>]=<value>.</summary>
    [Argument(Format = "-D {value}")] public IReadOnlyList<string> Define => Get<List<string>>(() => Define);
    /// <summary>Remove matching entries from the CMake cache, given a globbing expression.</summary>
    [Argument(Format = "-U {value}")] public IReadOnlyList<string> Undefine => Get<List<string>>(() => Undefine);
    /// <summary>Perform a fresh configuration, removing any existing cache file.</summary>
    [Argument(Format = "--fresh")] public bool? Fresh => Get<bool?>(() => Fresh);
    /// <summary>Enable developer warnings.</summary>
    [Argument(Format = "-Wdev")] public bool? WarnDev => Get<bool?>(() => WarnDev);
    /// <summary>Suppress developer warnings.</summary>
    [Argument(Format = "-Wno-dev")] public bool? NoWarnDev => Get<bool?>(() => NoWarnDev);
    /// <summary>Warn about uninitialized values.</summary>
    [Argument(Format = "--warn-uninitialized")] public bool? WarnUninitialized => Get<bool?>(() => WarnUninitialized);
    /// <summary>Warn about unused variables.</summary>
    [Argument(Format = "--warn-unused-vars")] public bool? WarnUnusedVars => Get<bool?>(() => WarnUnusedVars);
    /// <summary>Set the log level to one of: ERROR, WARNING, NOTICE, STATUS, VERBOSE, DEBUG, TRACE.</summary>
    [Argument(Format = "--log-level={value}")] public CMakeLogLevel LogLevel => Get<CMakeLogLevel>(() => LogLevel);
    /// <summary>Put cmake in a debug mode.</summary>
    [Argument(Format = "--debug-output")] public bool? Debug => Get<bool?>(() => Debug);
    /// <summary>Put cmake in trace mode.</summary>
    [Argument(Format = "--trace")] public bool? Trace => Get<bool?>(() => Trace);
    /// <summary>Put cmake in trace mode, expanding variables.</summary>
    [Argument(Format = "--trace-expand")] public bool? TraceExpand => Get<bool?>(() => TraceExpand);
}
#endregion
#region CMakeBuildSettings
/// <inheritdoc cref="CMakeTasks.CMakeBuild(Fallout.Common.Tools.CMake.CMakeBuildSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(CMakeTasks), Command = nameof(CMakeTasks.CMakeBuild), Arguments = "--build")]
public partial class CMakeBuildSettings : ToolOptions
{
    /// <summary>Specify the project binary directory to build.</summary>
    [Argument(Format = "{value}")] public string OutputDirectory => Get<string>(() => OutputDirectory);
    /// <summary>Specify the build configuration for multi-config generators.</summary>
    [Argument(Format = "--config {value}")] public CMakeConfiguration Configuration => Get<CMakeConfiguration>(() => Configuration);
    /// <summary>Specify the targets to build instead of the default target.</summary>
    [Argument(Format = "--target {value}")] public IReadOnlyList<string> Target => Get<List<string>>(() => Target);
    /// <summary>Specify the maximum number of concurrent processes to use when building.</summary>
    [Argument(Format = "--parallel {value}")] public int? Parallel => Get<int?>(() => Parallel);
    /// <summary>Build target 'clean' first, then build.</summary>
    [Argument(Format = "--clean-first")] public bool? CleanFirst => Get<bool?>(() => CleanFirst);
    /// <summary>Enable verbose output - if supported - including the build commands to be executed.</summary>
    [Argument(Format = "--verbose")] public bool? Verbose => Get<bool?>(() => Verbose);
    /// <summary>Pass remaining options to the native build tool, placed after '--'.</summary>
    [Argument(Format = "-- {value}", Position = -1, Separator = " ")] public IReadOnlyList<string> NativeToolOptions => Get<List<string>>(() => NativeToolOptions);
}
#endregion
#region CMakeInstallSettings
/// <inheritdoc cref="CMakeTasks.CMakeInstall(Fallout.Common.Tools.CMake.CMakeInstallSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(CMakeTasks), Command = nameof(CMakeTasks.CMakeInstall), Arguments = "--install")]
public partial class CMakeInstallSettings : ToolOptions
{
    /// <summary>Specify the root directory of the cmake install script.</summary>
    [Argument(Format = "{value}")] public string RootDirectory => Get<string>(() => RootDirectory);
    /// <summary>Specify the root directory of the 'CMakeLists.txt' file.</summary>
    [Argument(Format = "--config {value}")] public CMakeConfiguration Configuration => Get<CMakeConfiguration>(() => Configuration);
    /// <summary>Specify the output directory of the install script.</summary>
    [Argument(Format = "--prefix {value}")] public string OutputDirectory => Get<string>(() => OutputDirectory);
    /// <summary>Specify the component to install.</summary>
    [Argument(Format = "--component {value}")] public string Component => Get<string>(() => Component);
    /// <summary>Strip before installing.</summary>
    [Argument(Format = "--strip")] public bool? Strip => Get<bool?>(() => Strip);
    /// <summary>Enable verbose output.</summary>
    [Argument(Format = "--verbose")] public bool? Verbose => Get<bool?>(() => Verbose);
}
#endregion
#region CMakeGenerateSettingsExtensions
/// <inheritdoc cref="CMakeTasks.CMakeGenerate(Fallout.Common.Tools.CMake.CMakeGenerateSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class CMakeGenerateSettingsExtensions
{
    #region Generator
    /// <inheritdoc cref="CMakeGenerateSettings.Generator"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Generator))]
    public static T SetGenerator<T>(this T o, CMakeGenerator v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Generator, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Generator"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Generator))]
    public static T ResetGenerator<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.Generator));
    #endregion
    #region Platform
    /// <inheritdoc cref="CMakeGenerateSettings.Platform"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Platform))]
    public static T SetPlatform<T>(this T o, CMakePlatform v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Platform, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Platform"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Platform))]
    public static T ResetPlatform<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.Platform));
    #endregion
    #region RootDirectory
    /// <inheritdoc cref="CMakeGenerateSettings.RootDirectory"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.RootDirectory))]
    public static T SetRootDirectory<T>(this T o, string v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.RootDirectory, v));
    /// <inheritdoc cref="CMakeGenerateSettings.RootDirectory"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.RootDirectory))]
    public static T ResetRootDirectory<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.RootDirectory));
    #endregion
    #region OutputDirectory
    /// <inheritdoc cref="CMakeGenerateSettings.OutputDirectory"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.OutputDirectory))]
    public static T SetOutputDirectory<T>(this T o, string v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.OutputDirectory, v));
    /// <inheritdoc cref="CMakeGenerateSettings.OutputDirectory"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.OutputDirectory))]
    public static T ResetOutputDirectory<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.OutputDirectory));
    #endregion
    #region ToolChain
    /// <inheritdoc cref="CMakeGenerateSettings.ToolChain"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.ToolChain))]
    public static T SetToolChain<T>(this T o, string v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.ToolChain, v));
    /// <inheritdoc cref="CMakeGenerateSettings.ToolChain"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.ToolChain))]
    public static T ResetToolChain<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.ToolChain));
    #endregion
    #region ToolsetSpec
    /// <inheritdoc cref="CMakeGenerateSettings.ToolsetSpec"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.ToolsetSpec))]
    public static T SetToolsetSpec<T>(this T o, string v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.ToolsetSpec, v));
    /// <inheritdoc cref="CMakeGenerateSettings.ToolsetSpec"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.ToolsetSpec))]
    public static T ResetToolsetSpec<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.ToolsetSpec));
    #endregion
    #region InstallPrefix
    /// <inheritdoc cref="CMakeGenerateSettings.InstallPrefix"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.InstallPrefix))]
    public static T SetInstallPrefix<T>(this T o, string v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.InstallPrefix, v));
    /// <inheritdoc cref="CMakeGenerateSettings.InstallPrefix"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.InstallPrefix))]
    public static T ResetInstallPrefix<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.InstallPrefix));
    #endregion
    #region Define
    /// <inheritdoc cref="CMakeGenerateSettings.Define"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Define))]
    public static T SetDefine<T>(this T o, params string[] v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Define, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Define"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Define))]
    public static T SetDefine<T>(this T o, IEnumerable<string> v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Define, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Define"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Define))]
    public static T AddDefine<T>(this T o, params string[] v) where T : CMakeGenerateSettings => o.Modify(b => b.AddCollection(() => o.Define, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Define"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Define))]
    public static T AddDefine<T>(this T o, IEnumerable<string> v) where T : CMakeGenerateSettings => o.Modify(b => b.AddCollection(() => o.Define, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Define"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Define))]
    public static T RemoveDefine<T>(this T o, params string[] v) where T : CMakeGenerateSettings => o.Modify(b => b.RemoveCollection(() => o.Define, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Define"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Define))]
    public static T RemoveDefine<T>(this T o, IEnumerable<string> v) where T : CMakeGenerateSettings => o.Modify(b => b.RemoveCollection(() => o.Define, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Define"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Define))]
    public static T ClearDefine<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.ClearCollection(() => o.Define));
    #endregion
    #region Undefine
    /// <inheritdoc cref="CMakeGenerateSettings.Undefine"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Undefine))]
    public static T SetUndefine<T>(this T o, params string[] v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Undefine, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Undefine"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Undefine))]
    public static T SetUndefine<T>(this T o, IEnumerable<string> v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Undefine, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Undefine"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Undefine))]
    public static T AddUndefine<T>(this T o, params string[] v) where T : CMakeGenerateSettings => o.Modify(b => b.AddCollection(() => o.Undefine, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Undefine"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Undefine))]
    public static T AddUndefine<T>(this T o, IEnumerable<string> v) where T : CMakeGenerateSettings => o.Modify(b => b.AddCollection(() => o.Undefine, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Undefine"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Undefine))]
    public static T RemoveUndefine<T>(this T o, params string[] v) where T : CMakeGenerateSettings => o.Modify(b => b.RemoveCollection(() => o.Undefine, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Undefine"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Undefine))]
    public static T RemoveUndefine<T>(this T o, IEnumerable<string> v) where T : CMakeGenerateSettings => o.Modify(b => b.RemoveCollection(() => o.Undefine, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Undefine"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Undefine))]
    public static T ClearUndefine<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.ClearCollection(() => o.Undefine));
    #endregion
    #region Fresh
    /// <inheritdoc cref="CMakeGenerateSettings.Fresh"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Fresh))]
    public static T SetFresh<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Fresh, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Fresh"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Fresh))]
    public static T ResetFresh<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.Fresh));
    /// <inheritdoc cref="CMakeGenerateSettings.Fresh"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Fresh))]
    public static T EnableFresh<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Fresh, true));
    /// <inheritdoc cref="CMakeGenerateSettings.Fresh"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Fresh))]
    public static T DisableFresh<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Fresh, false));
    /// <inheritdoc cref="CMakeGenerateSettings.Fresh"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Fresh))]
    public static T ToggleFresh<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Fresh, !o.Fresh));
    #endregion
    #region WarnDev
    /// <inheritdoc cref="CMakeGenerateSettings.WarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnDev))]
    public static T SetWarnDev<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnDev, v));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnDev))]
    public static T ResetWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.WarnDev));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnDev))]
    public static T EnableWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnDev, true));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnDev))]
    public static T DisableWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnDev, false));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnDev))]
    public static T ToggleWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnDev, !o.WarnDev));
    #endregion
    #region NoWarnDev
    /// <inheritdoc cref="CMakeGenerateSettings.NoWarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.NoWarnDev))]
    public static T SetNoWarnDev<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.NoWarnDev, v));
    /// <inheritdoc cref="CMakeGenerateSettings.NoWarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.NoWarnDev))]
    public static T ResetNoWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.NoWarnDev));
    /// <inheritdoc cref="CMakeGenerateSettings.NoWarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.NoWarnDev))]
    public static T EnableNoWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.NoWarnDev, true));
    /// <inheritdoc cref="CMakeGenerateSettings.NoWarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.NoWarnDev))]
    public static T DisableNoWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.NoWarnDev, false));
    /// <inheritdoc cref="CMakeGenerateSettings.NoWarnDev"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.NoWarnDev))]
    public static T ToggleNoWarnDev<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.NoWarnDev, !o.NoWarnDev));
    #endregion
    #region WarnUninitialized
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUninitialized"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUninitialized))]
    public static T SetWarnUninitialized<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUninitialized, v));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUninitialized"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUninitialized))]
    public static T ResetWarnUninitialized<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.WarnUninitialized));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUninitialized"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUninitialized))]
    public static T EnableWarnUninitialized<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUninitialized, true));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUninitialized"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUninitialized))]
    public static T DisableWarnUninitialized<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUninitialized, false));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUninitialized"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUninitialized))]
    public static T ToggleWarnUninitialized<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUninitialized, !o.WarnUninitialized));
    #endregion
    #region WarnUnusedVars
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUnusedVars"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUnusedVars))]
    public static T SetWarnUnusedVars<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUnusedVars, v));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUnusedVars"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUnusedVars))]
    public static T ResetWarnUnusedVars<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.WarnUnusedVars));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUnusedVars"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUnusedVars))]
    public static T EnableWarnUnusedVars<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUnusedVars, true));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUnusedVars"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUnusedVars))]
    public static T DisableWarnUnusedVars<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUnusedVars, false));
    /// <inheritdoc cref="CMakeGenerateSettings.WarnUnusedVars"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.WarnUnusedVars))]
    public static T ToggleWarnUnusedVars<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.WarnUnusedVars, !o.WarnUnusedVars));
    #endregion
    #region LogLevel
    /// <inheritdoc cref="CMakeGenerateSettings.LogLevel"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.LogLevel))]
    public static T SetLogLevel<T>(this T o, CMakeLogLevel v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.LogLevel, v));
    /// <inheritdoc cref="CMakeGenerateSettings.LogLevel"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.LogLevel))]
    public static T ResetLogLevel<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.LogLevel));
    #endregion
    #region Debug
    /// <inheritdoc cref="CMakeGenerateSettings.Debug"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Debug))]
    public static T SetDebug<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Debug, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Debug"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Debug))]
    public static T ResetDebug<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.Debug));
    /// <inheritdoc cref="CMakeGenerateSettings.Debug"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Debug))]
    public static T EnableDebug<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Debug, true));
    /// <inheritdoc cref="CMakeGenerateSettings.Debug"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Debug))]
    public static T DisableDebug<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Debug, false));
    /// <inheritdoc cref="CMakeGenerateSettings.Debug"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Debug))]
    public static T ToggleDebug<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Debug, !o.Debug));
    #endregion
    #region Trace
    /// <inheritdoc cref="CMakeGenerateSettings.Trace"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Trace))]
    public static T SetTrace<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Trace, v));
    /// <inheritdoc cref="CMakeGenerateSettings.Trace"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Trace))]
    public static T ResetTrace<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.Trace));
    /// <inheritdoc cref="CMakeGenerateSettings.Trace"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Trace))]
    public static T EnableTrace<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Trace, true));
    /// <inheritdoc cref="CMakeGenerateSettings.Trace"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Trace))]
    public static T DisableTrace<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Trace, false));
    /// <inheritdoc cref="CMakeGenerateSettings.Trace"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.Trace))]
    public static T ToggleTrace<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.Trace, !o.Trace));
    #endregion
    #region TraceExpand
    /// <inheritdoc cref="CMakeGenerateSettings.TraceExpand"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.TraceExpand))]
    public static T SetTraceExpand<T>(this T o, bool? v) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.TraceExpand, v));
    /// <inheritdoc cref="CMakeGenerateSettings.TraceExpand"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.TraceExpand))]
    public static T ResetTraceExpand<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Remove(() => o.TraceExpand));
    /// <inheritdoc cref="CMakeGenerateSettings.TraceExpand"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.TraceExpand))]
    public static T EnableTraceExpand<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.TraceExpand, true));
    /// <inheritdoc cref="CMakeGenerateSettings.TraceExpand"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.TraceExpand))]
    public static T DisableTraceExpand<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.TraceExpand, false));
    /// <inheritdoc cref="CMakeGenerateSettings.TraceExpand"/>
    [Builder(Type = typeof(CMakeGenerateSettings), Property = nameof(CMakeGenerateSettings.TraceExpand))]
    public static T ToggleTraceExpand<T>(this T o) where T : CMakeGenerateSettings => o.Modify(b => b.Set(() => o.TraceExpand, !o.TraceExpand));
    #endregion
}
#endregion
#region CMakeBuildSettingsExtensions
/// <inheritdoc cref="CMakeTasks.CMakeBuild(Fallout.Common.Tools.CMake.CMakeBuildSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class CMakeBuildSettingsExtensions
{
    #region OutputDirectory
    /// <inheritdoc cref="CMakeBuildSettings.OutputDirectory"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.OutputDirectory))]
    public static T SetOutputDirectory<T>(this T o, string v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.OutputDirectory, v));
    /// <inheritdoc cref="CMakeBuildSettings.OutputDirectory"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.OutputDirectory))]
    public static T ResetOutputDirectory<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Remove(() => o.OutputDirectory));
    #endregion
    #region Configuration
    /// <inheritdoc cref="CMakeBuildSettings.Configuration"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Configuration))]
    public static T SetConfiguration<T>(this T o, CMakeConfiguration v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Configuration, v));
    /// <inheritdoc cref="CMakeBuildSettings.Configuration"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Configuration))]
    public static T ResetConfiguration<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Remove(() => o.Configuration));
    #endregion
    #region Target
    /// <inheritdoc cref="CMakeBuildSettings.Target"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Target))]
    public static T SetTarget<T>(this T o, params string[] v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Target, v));
    /// <inheritdoc cref="CMakeBuildSettings.Target"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Target))]
    public static T SetTarget<T>(this T o, IEnumerable<string> v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Target, v));
    /// <inheritdoc cref="CMakeBuildSettings.Target"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Target))]
    public static T AddTarget<T>(this T o, params string[] v) where T : CMakeBuildSettings => o.Modify(b => b.AddCollection(() => o.Target, v));
    /// <inheritdoc cref="CMakeBuildSettings.Target"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Target))]
    public static T AddTarget<T>(this T o, IEnumerable<string> v) where T : CMakeBuildSettings => o.Modify(b => b.AddCollection(() => o.Target, v));
    /// <inheritdoc cref="CMakeBuildSettings.Target"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Target))]
    public static T RemoveTarget<T>(this T o, params string[] v) where T : CMakeBuildSettings => o.Modify(b => b.RemoveCollection(() => o.Target, v));
    /// <inheritdoc cref="CMakeBuildSettings.Target"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Target))]
    public static T RemoveTarget<T>(this T o, IEnumerable<string> v) where T : CMakeBuildSettings => o.Modify(b => b.RemoveCollection(() => o.Target, v));
    /// <inheritdoc cref="CMakeBuildSettings.Target"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Target))]
    public static T ClearTarget<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.ClearCollection(() => o.Target));
    #endregion
    #region Parallel
    /// <inheritdoc cref="CMakeBuildSettings.Parallel"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Parallel))]
    public static T SetParallel<T>(this T o, int? v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Parallel, v));
    /// <inheritdoc cref="CMakeBuildSettings.Parallel"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Parallel))]
    public static T ResetParallel<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Remove(() => o.Parallel));
    #endregion
    #region CleanFirst
    /// <inheritdoc cref="CMakeBuildSettings.CleanFirst"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.CleanFirst))]
    public static T SetCleanFirst<T>(this T o, bool? v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.CleanFirst, v));
    /// <inheritdoc cref="CMakeBuildSettings.CleanFirst"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.CleanFirst))]
    public static T ResetCleanFirst<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Remove(() => o.CleanFirst));
    /// <inheritdoc cref="CMakeBuildSettings.CleanFirst"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.CleanFirst))]
    public static T EnableCleanFirst<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.CleanFirst, true));
    /// <inheritdoc cref="CMakeBuildSettings.CleanFirst"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.CleanFirst))]
    public static T DisableCleanFirst<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.CleanFirst, false));
    /// <inheritdoc cref="CMakeBuildSettings.CleanFirst"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.CleanFirst))]
    public static T ToggleCleanFirst<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.CleanFirst, !o.CleanFirst));
    #endregion
    #region Verbose
    /// <inheritdoc cref="CMakeBuildSettings.Verbose"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Verbose))]
    public static T SetVerbose<T>(this T o, bool? v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Verbose, v));
    /// <inheritdoc cref="CMakeBuildSettings.Verbose"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Verbose))]
    public static T ResetVerbose<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Remove(() => o.Verbose));
    /// <inheritdoc cref="CMakeBuildSettings.Verbose"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Verbose))]
    public static T EnableVerbose<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Verbose, true));
    /// <inheritdoc cref="CMakeBuildSettings.Verbose"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Verbose))]
    public static T DisableVerbose<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Verbose, false));
    /// <inheritdoc cref="CMakeBuildSettings.Verbose"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.Verbose))]
    public static T ToggleVerbose<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.Verbose, !o.Verbose));
    #endregion
    #region NativeToolOptions
    /// <inheritdoc cref="CMakeBuildSettings.NativeToolOptions"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.NativeToolOptions))]
    public static T SetNativeToolOptions<T>(this T o, params string[] v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.NativeToolOptions, v));
    /// <inheritdoc cref="CMakeBuildSettings.NativeToolOptions"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.NativeToolOptions))]
    public static T SetNativeToolOptions<T>(this T o, IEnumerable<string> v) where T : CMakeBuildSettings => o.Modify(b => b.Set(() => o.NativeToolOptions, v));
    /// <inheritdoc cref="CMakeBuildSettings.NativeToolOptions"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.NativeToolOptions))]
    public static T AddNativeToolOptions<T>(this T o, params string[] v) where T : CMakeBuildSettings => o.Modify(b => b.AddCollection(() => o.NativeToolOptions, v));
    /// <inheritdoc cref="CMakeBuildSettings.NativeToolOptions"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.NativeToolOptions))]
    public static T AddNativeToolOptions<T>(this T o, IEnumerable<string> v) where T : CMakeBuildSettings => o.Modify(b => b.AddCollection(() => o.NativeToolOptions, v));
    /// <inheritdoc cref="CMakeBuildSettings.NativeToolOptions"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.NativeToolOptions))]
    public static T RemoveNativeToolOptions<T>(this T o, params string[] v) where T : CMakeBuildSettings => o.Modify(b => b.RemoveCollection(() => o.NativeToolOptions, v));
    /// <inheritdoc cref="CMakeBuildSettings.NativeToolOptions"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.NativeToolOptions))]
    public static T RemoveNativeToolOptions<T>(this T o, IEnumerable<string> v) where T : CMakeBuildSettings => o.Modify(b => b.RemoveCollection(() => o.NativeToolOptions, v));
    /// <inheritdoc cref="CMakeBuildSettings.NativeToolOptions"/>
    [Builder(Type = typeof(CMakeBuildSettings), Property = nameof(CMakeBuildSettings.NativeToolOptions))]
    public static T ClearNativeToolOptions<T>(this T o) where T : CMakeBuildSettings => o.Modify(b => b.ClearCollection(() => o.NativeToolOptions));
    #endregion
}
#endregion
#region CMakeInstallSettingsExtensions
/// <inheritdoc cref="CMakeTasks.CMakeInstall(Fallout.Common.Tools.CMake.CMakeInstallSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class CMakeInstallSettingsExtensions
{
    #region RootDirectory
    /// <inheritdoc cref="CMakeInstallSettings.RootDirectory"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.RootDirectory))]
    public static T SetRootDirectory<T>(this T o, string v) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.RootDirectory, v));
    /// <inheritdoc cref="CMakeInstallSettings.RootDirectory"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.RootDirectory))]
    public static T ResetRootDirectory<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Remove(() => o.RootDirectory));
    #endregion
    #region Configuration
    /// <inheritdoc cref="CMakeInstallSettings.Configuration"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Configuration))]
    public static T SetConfiguration<T>(this T o, CMakeConfiguration v) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Configuration, v));
    /// <inheritdoc cref="CMakeInstallSettings.Configuration"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Configuration))]
    public static T ResetConfiguration<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Remove(() => o.Configuration));
    #endregion
    #region OutputDirectory
    /// <inheritdoc cref="CMakeInstallSettings.OutputDirectory"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.OutputDirectory))]
    public static T SetOutputDirectory<T>(this T o, string v) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.OutputDirectory, v));
    /// <inheritdoc cref="CMakeInstallSettings.OutputDirectory"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.OutputDirectory))]
    public static T ResetOutputDirectory<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Remove(() => o.OutputDirectory));
    #endregion
    #region Component
    /// <inheritdoc cref="CMakeInstallSettings.Component"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Component))]
    public static T SetComponent<T>(this T o, string v) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Component, v));
    /// <inheritdoc cref="CMakeInstallSettings.Component"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Component))]
    public static T ResetComponent<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Remove(() => o.Component));
    #endregion
    #region Strip
    /// <inheritdoc cref="CMakeInstallSettings.Strip"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Strip))]
    public static T SetStrip<T>(this T o, bool? v) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Strip, v));
    /// <inheritdoc cref="CMakeInstallSettings.Strip"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Strip))]
    public static T ResetStrip<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Remove(() => o.Strip));
    /// <inheritdoc cref="CMakeInstallSettings.Strip"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Strip))]
    public static T EnableStrip<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Strip, true));
    /// <inheritdoc cref="CMakeInstallSettings.Strip"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Strip))]
    public static T DisableStrip<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Strip, false));
    /// <inheritdoc cref="CMakeInstallSettings.Strip"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Strip))]
    public static T ToggleStrip<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Strip, !o.Strip));
    #endregion
    #region Verbose
    /// <inheritdoc cref="CMakeInstallSettings.Verbose"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Verbose))]
    public static T SetVerbose<T>(this T o, bool? v) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Verbose, v));
    /// <inheritdoc cref="CMakeInstallSettings.Verbose"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Verbose))]
    public static T ResetVerbose<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Remove(() => o.Verbose));
    /// <inheritdoc cref="CMakeInstallSettings.Verbose"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Verbose))]
    public static T EnableVerbose<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Verbose, true));
    /// <inheritdoc cref="CMakeInstallSettings.Verbose"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Verbose))]
    public static T DisableVerbose<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Verbose, false));
    /// <inheritdoc cref="CMakeInstallSettings.Verbose"/>
    [Builder(Type = typeof(CMakeInstallSettings), Property = nameof(CMakeInstallSettings.Verbose))]
    public static T ToggleVerbose<T>(this T o) where T : CMakeInstallSettings => o.Modify(b => b.Set(() => o.Verbose, !o.Verbose));
    #endregion
}
#endregion
#region CMakeGenerator
/// <summary>Used within <see cref="CMakeTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<CMakeGenerator>))]
public partial class CMakeGenerator : Enumeration
{
    public static CMakeGenerator Visual_Studio_15_2017 = (CMakeGenerator) "Visual Studio 15 2017";
    public static CMakeGenerator Visual_Studio_16_2019 = (CMakeGenerator) "Visual Studio 16 2019";
    public static CMakeGenerator Visual_Studio_17_2022 = (CMakeGenerator) "Visual Studio 17 2022";
    public static CMakeGenerator Visual_Studio_18_2026 = (CMakeGenerator) "Visual Studio 18 2026";
    public static CMakeGenerator Ninja = (CMakeGenerator) "Ninja";
    public static CMakeGenerator Xcode = (CMakeGenerator) "Xcode";
    public static implicit operator CMakeGenerator(string value)
    {
        return new CMakeGenerator { Value = value };
    }
}
#endregion
#region CMakePlatform
/// <summary>Used within <see cref="CMakeTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<CMakePlatform>))]
public partial class CMakePlatform : Enumeration
{
    public static CMakePlatform Win32 = (CMakePlatform) "Win32";
    public static CMakePlatform x64 = (CMakePlatform) "x64";
    public static CMakePlatform ARM = (CMakePlatform) "ARM";
    public static CMakePlatform ARM64 = (CMakePlatform) "ARM64";
    public static implicit operator CMakePlatform(string value)
    {
        return new CMakePlatform { Value = value };
    }
}
#endregion
#region CMakeConfiguration
/// <summary>Used within <see cref="CMakeTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<CMakeConfiguration>))]
public partial class CMakeConfiguration : Enumeration
{
    public static CMakeConfiguration Debug = (CMakeConfiguration) "Debug";
    public static CMakeConfiguration RelWithDebInfo = (CMakeConfiguration) "RelWithDebInfo";
    public static CMakeConfiguration Release = (CMakeConfiguration) "Release";
    public static CMakeConfiguration MinSizeRel = (CMakeConfiguration) "MinSizeRel";
    public static implicit operator CMakeConfiguration(string value)
    {
        return new CMakeConfiguration { Value = value };
    }
}
#endregion
#region CMakeLogLevel
/// <summary>Used within <see cref="CMakeTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<CMakeLogLevel>))]
public partial class CMakeLogLevel : Enumeration
{
    public static CMakeLogLevel Error = (CMakeLogLevel) "Error";
    public static CMakeLogLevel Warning = (CMakeLogLevel) "Warning";
    public static CMakeLogLevel Notice = (CMakeLogLevel) "Notice";
    public static CMakeLogLevel Status = (CMakeLogLevel) "Status";
    public static CMakeLogLevel Verbose = (CMakeLogLevel) "Verbose";
    public static CMakeLogLevel Debug = (CMakeLogLevel) "Debug";
    public static CMakeLogLevel Trace = (CMakeLogLevel) "Trace";
    public static implicit operator CMakeLogLevel(string value)
    {
        return new CMakeLogLevel { Value = value };
    }
}
#endregion
