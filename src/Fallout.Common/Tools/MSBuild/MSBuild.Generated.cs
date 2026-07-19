// Generated from https://github.com/Fallout-build/Fallout/blob/main/src/Fallout.Common/Tools/MSBuild/MSBuild.json

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

namespace Fallout.Common.Tools.MSBuild;

/// <summary><p>The Microsoft Build Engine is a platform for building applications. This engine, which is also known as MSBuild, provides an XML schema for a project file that controls how the build platform processes and builds software. Visual Studio uses MSBuild, but it doesn't depend on Visual Studio. By invoking msbuild.exe on your project or solution file, you can orchestrate and build products in environments where Visual Studio isn't installed. Visual Studio uses MSBuild to load and build managed projects. The project files in Visual Studio (.csproj,.vbproj, vcxproj, and others) contain MSBuild XML code that executes when you build a project by using the IDE. Visual Studio projects import all the necessary settings and build processes to do typical development work, but you can extend or modify them from within Visual Studio or by using an XML editor.</p><p>For more details, visit the <a href="https://msdn.microsoft.com/en-us/library/ms164311.aspx">official website</a>.</p></summary>
[ExcludeFromCodeCoverage]
public partial class MSBuildTasks : ToolTasks
{
    public static string MSBuildPath { get => new MSBuildTasks().GetToolPathInternal(); set => new MSBuildTasks().SetToolPath(value); }
    /// <summary><p>The Microsoft Build Engine is a platform for building applications. This engine, which is also known as MSBuild, provides an XML schema for a project file that controls how the build platform processes and builds software. Visual Studio uses MSBuild, but it doesn't depend on Visual Studio. By invoking msbuild.exe on your project or solution file, you can orchestrate and build products in environments where Visual Studio isn't installed. Visual Studio uses MSBuild to load and build managed projects. The project files in Visual Studio (.csproj,.vbproj, vcxproj, and others) contain MSBuild XML code that executes when you build a project by using the IDE. Visual Studio projects import all the necessary settings and build processes to do typical development work, but you can extend or modify them from within Visual Studio or by using an XML editor.</p><p>For more details, visit the <a href="https://msdn.microsoft.com/en-us/library/ms164311.aspx">official website</a>.</p></summary>
    public static IReadOnlyCollection<Output> MSBuild(ArgumentStringHandler arguments, string workingDirectory = null, IReadOnlyDictionary<string, string> environmentVariables = null, int? timeout = null, bool? logOutput = null, bool? logInvocation = null, Action<OutputType, string> logger = null, Func<IProcess, object> exitHandler = null) => new MSBuildTasks().Run(arguments, workingDirectory, environmentVariables, timeout, logOutput, logInvocation, logger, exitHandler);
    /// <summary><p>The Microsoft Build Engine is a platform for building applications. This engine, which is also known as MSBuild, provides an XML schema for a project file that controls how the build platform processes and builds software. Visual Studio uses MSBuild, but it doesn't depend on Visual Studio. By invoking msbuild.exe on your project or solution file, you can orchestrate and build products in environments where Visual Studio isn't installed. Visual Studio uses MSBuild to load and build managed projects. The project files in Visual Studio (.csproj,.vbproj, vcxproj, and others) contain MSBuild XML code that executes when you build a project by using the IDE. Visual Studio projects import all the necessary settings and build processes to do typical development work, but you can extend or modify them from within Visual Studio or by using an XML editor.</p><p>For more details, visit the <a href="https://msdn.microsoft.com/en-us/library/ms164311.aspx">official website</a>.</p></summary>
    /// <remarks><p>This is a <a href="https://github.com/Fallout-build/Fallout">CLI wrapper with fluent API</a> that allows to modify the following arguments:</p><ul><li><c>&lt;targetPath&gt;</c> via <see cref="MSBuildSettings.TargetPath"/></li><li><c>/detailedsummary</c> via <see cref="MSBuildSettings.DetailedSummary"/></li><li><c>/logger</c> via <see cref="MSBuildSettings.Loggers"/></li><li><c>/maxcpucount</c> via <see cref="MSBuildSettings.MaxCpuCount"/></li><li><c>/noconsolelogger</c> via <see cref="MSBuildSettings.NoConsoleLogger"/></li><li><c>/nodeReuse</c> via <see cref="MSBuildSettings.NodeReuse"/></li><li><c>/nologo</c> via <see cref="MSBuildSettings.NoLogo"/></li><li><c>/p</c> via <see cref="MSBuildSettings.Properties"/></li><li><c>/p:Platform</c> via <see cref="MSBuildSettings.TargetPlatform"/></li><li><c>/restore</c> via <see cref="MSBuildSettings.Restore"/></li><li><c>/target</c> via <see cref="MSBuildSettings.Targets"/></li><li><c>/toolsversion</c> via <see cref="MSBuildSettings.ToolsVersion"/></li><li><c>/verbosity</c> via <see cref="MSBuildSettings.Verbosity"/></li></ul></remarks>
    public static IReadOnlyCollection<Output> MSBuild(MSBuildSettings options = null) => new MSBuildTasks().Run<MSBuildSettings>(options);
    /// <inheritdoc cref="MSBuildTasks.MSBuild(Fallout.Common.Tools.MSBuild.MSBuildSettings)"/>
    public static IReadOnlyCollection<Output> MSBuild(Configure<MSBuildSettings> configurator) => new MSBuildTasks().Run<MSBuildSettings>(configurator.Invoke(new MSBuildSettings()));
    /// <inheritdoc cref="MSBuildTasks.MSBuild(Fallout.Common.Tools.MSBuild.MSBuildSettings)"/>
    public static IEnumerable<(MSBuildSettings Settings, IReadOnlyCollection<Output> Output)> MSBuild(CombinatorialConfigure<MSBuildSettings> configurator, int degreeOfParallelism = 1, bool completeOnFailure = false) => configurator.Invoke(MSBuild, degreeOfParallelism, completeOnFailure);
}
#region MSBuildSettings
/// <inheritdoc cref="MSBuildTasks.MSBuild(Fallout.Common.Tools.MSBuild.MSBuildSettings)"/>
[ExcludeFromCodeCoverage]
[Command(Type = typeof(MSBuildTasks), Command = nameof(MSBuildTasks.MSBuild))]
public partial class MSBuildSettings : ToolOptions
{
    /// <summary>The solution or project file on which MSBuild is executed.</summary>
    [Argument(Format = "{value}", Position = 1)] public string TargetPath => Get<string>(() => TargetPath);
    /// <summary>Show detailed information at the end of the build log about the configurations that were built and how they were scheduled to nodes.</summary>
    [Argument(Format = "/detailedsummary")] public bool? DetailedSummary => Get<bool?>(() => DetailedSummary);
    /// <summary><p>Specifies the maximum number of concurrent processes to use when building. If you don't include this switch, the default value is 1. If you include this switch without specifying a value, MSBuild will use up to the number of processors in the computer. For more information, see <a href="https://msdn.microsoft.com/en-us/library/bb651793.aspx">Building Multiple Projects in Parallel</a>.</p><p>The following example instructs MSBuild to build using three MSBuild processes, which allows three projects to build at the same time:</p><p><c>msbuild myproject.proj /maxcpucount:3</c></p></summary>
    [Argument(Format = "/maxcpucount:{value}")] public int? MaxCpuCount => Get<int?>(() => MaxCpuCount);
    /// <summary><p>Enable or disable the re-use of MSBuild nodes. You can specify the following values: <ul><li><c>true</c>: Nodes remain after the build finishes so that subsequent builds can use them (default).</li><li><c>false</c>. Nodes don't remain after the build completes.</li></ul></p><p>A node corresponds to a project that's executing. If you include the <c>/maxcpucount</c> switch, multiple nodes can execute concurrently.</p></summary>
    [Argument(Format = "/nodeReuse:{value}")] public bool? NodeReuse => Get<bool?>(() => NodeReuse);
    /// <summary>Don't display the startup banner or the copyright message.</summary>
    [Argument(Format = "/nologo")] public bool? NoLogo => Get<bool?>(() => NoLogo);
    /// <summary>The target platform for which the project is built to run on.</summary>
    [Argument(Format = "/p:Platform={value}", FormatterMethod = nameof(FormatPlatform))] public MSBuildTargetPlatform TargetPlatform => Get<MSBuildTargetPlatform>(() => TargetPlatform);
    /// <summary><p>Set or override the specified project-level properties, where name is the property name and value is the property value. Specify each property separately, or use a semicolon or comma to separate multiple properties, as the following example shows:</p><p><c>/property:WarningLevel=2;OutDir=bin\Debug</c></p></summary>
    [Argument(Format = "/p:{key}={value}")] public IReadOnlyDictionary<string, object> Properties => Get<Dictionary<string, object>>(() => Properties);
    /// <summary>Runs the <c>Restore</c> target prior to building the actual targets.</summary>
    [Argument(Format = "/restore")] public bool? Restore => Get<bool?>(() => Restore);
    /// <summary><p>Build the specified targets in the project. Specify each target separately, or use a semicolon or comma to separate multiple targets, as the following example shows:<br/><c>/target:Resources;Compile</c></p><p>If you specify any targets by using this switch, they are run instead of any targets in the DefaultTargets attribute in the project file. For more information, see <a href="https://msdn.microsoft.com/en-us/library/ee216359.aspx">Target Build Order</a> and <a href="https://msdn.microsoft.com/en-us/library/ms171463.aspx">How to: Specify Which Target to Build First</a>.</p><p>A target is a group of tasks. For more information, see <a href="https://msdn.microsoft.com/en-us/library/ms171462.aspx">Targets</a>.</p></summary>
    [Argument(Format = "/target:{value}", Separator = ";")] public IReadOnlyList<string> Targets => Get<List<string>>(() => Targets);
    /// <summary><p>Specifies the version of the Toolset to use to build the project, as the following example shows: <c>/toolsversion:3.5</c></p><p>By using this switch, you can build a project and specify a version that differs from the version that's specified in the <a href="https://msdn.microsoft.com/en-us/library/bcxfsh87.aspx">Project Element (MSBuild)</a>. For more information, see <a href="https://msdn.microsoft.com/en-us/library/bb383985.aspx">Overriding ToolsVersion Settings</a>.</p><p>For MSBuild 4.5, you can specify the following values for version: 2.0, 3.5, and 4.0. If you specify 4.0, the VisualStudioVersion build property specifies which sub-toolset to use. For more information, see the Sub-toolsets section of <a href="https://msdn.microsoft.com/en-us/library/bb383796.aspx">Toolset (ToolsVersion)</a>.</p><p>A Toolset consists of tasks, targets, and tools that are used to build an application. The tools include compilers such as csc.exe and vbc.exe. For more information about Toolsets, see <a href="https://msdn.microsoft.com/en-us/library/bb383796.aspx">Toolset (ToolsVersion)</a>, <a href="https://msdn.microsoft.com/en-us/library/bb397428.aspx">Standard and Custom Toolset Configurations</a>, and <a href="https://msdn.microsoft.com/en-us/library/hh264223.aspx">Multitargeting</a>. Note: The toolset version isn't the same as the target framework, which is the version of the .NET Framework on which a project is built to run. For more information, see <a href="https://msdn.microsoft.com/en-us/library/hh264221.aspx">Target Framework and Target Platform</a>.</p></summary>
    [Argument(Format = "/toolsversion:{value}")] public MSBuildToolsVersion ToolsVersion => Get<MSBuildToolsVersion>(() => ToolsVersion);
    /// <summary>Specifies the version of MSBuild to use.</summary>
    public MSBuildVersion? MSBuildVersion => Get<MSBuildVersion?>(() => MSBuildVersion);
    /// <summary><p>Specifies the amount of information to display in the build log. Each logger displays events based on the verbosity level that you set for that logger.</p><p>You can specify the following verbosity levels: <c>q[uiet]</c>, <c>m[inimal]</c>, <c>n[ormal]</c>, <c>d[etailed]</c>, and <c>diag[nostic]</c>.</p><p>The following setting is an example: <c>/verbosity:quiet</c></p></summary>
    [Argument(Format = "/verbosity:{value}")] public MSBuildVerbosity Verbosity => Get<MSBuildVerbosity>(() => Verbosity);
    /// <summary>Specifies the MSBuild platform (e.g. x86, x64).</summary>
    public MSBuildPlatform? MSBuildPlatform => Get<MSBuildPlatform?>(() => MSBuildPlatform);
    /// <summary>Specifies the loggers to use to log events from MSBuild.</summary>
    [Argument(Format = "/logger:{value}")] public IReadOnlyList<string> Loggers => Get<List<string>>(() => Loggers);
    /// <summary>Disable the default console logger, and don't log events to the console.</summary>
    [Argument(Format = "/noconsolelogger")] public bool? NoConsoleLogger => Get<bool?>(() => NoConsoleLogger);
}
#endregion
#region MSBuildSettingsExtensions
/// <inheritdoc cref="MSBuildTasks.MSBuild(Fallout.Common.Tools.MSBuild.MSBuildSettings)"/>
[ExcludeFromCodeCoverage]
public static partial class MSBuildSettingsExtensions
{
    #region TargetPath
    /// <inheritdoc cref="MSBuildSettings.TargetPath"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.TargetPath))]
    public static T SetTargetPath<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.TargetPath, v));
    /// <inheritdoc cref="MSBuildSettings.TargetPath"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.TargetPath))]
    public static T ResetTargetPath<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.TargetPath));
    #endregion
    #region DetailedSummary
    /// <inheritdoc cref="MSBuildSettings.DetailedSummary"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.DetailedSummary))]
    public static T SetDetailedSummary<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.DetailedSummary, v));
    /// <inheritdoc cref="MSBuildSettings.DetailedSummary"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.DetailedSummary))]
    public static T ResetDetailedSummary<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.DetailedSummary));
    /// <inheritdoc cref="MSBuildSettings.DetailedSummary"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.DetailedSummary))]
    public static T EnableDetailedSummary<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.DetailedSummary, true));
    /// <inheritdoc cref="MSBuildSettings.DetailedSummary"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.DetailedSummary))]
    public static T DisableDetailedSummary<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.DetailedSummary, false));
    /// <inheritdoc cref="MSBuildSettings.DetailedSummary"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.DetailedSummary))]
    public static T ToggleDetailedSummary<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.DetailedSummary, !o.DetailedSummary));
    #endregion
    #region MaxCpuCount
    /// <inheritdoc cref="MSBuildSettings.MaxCpuCount"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.MaxCpuCount))]
    public static T SetMaxCpuCount<T>(this T o, int? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.MaxCpuCount, v));
    /// <inheritdoc cref="MSBuildSettings.MaxCpuCount"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.MaxCpuCount))]
    public static T ResetMaxCpuCount<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.MaxCpuCount));
    #endregion
    #region NodeReuse
    /// <inheritdoc cref="MSBuildSettings.NodeReuse"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NodeReuse))]
    public static T SetNodeReuse<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NodeReuse, v));
    /// <inheritdoc cref="MSBuildSettings.NodeReuse"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NodeReuse))]
    public static T ResetNodeReuse<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.NodeReuse));
    /// <inheritdoc cref="MSBuildSettings.NodeReuse"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NodeReuse))]
    public static T EnableNodeReuse<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NodeReuse, true));
    /// <inheritdoc cref="MSBuildSettings.NodeReuse"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NodeReuse))]
    public static T DisableNodeReuse<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NodeReuse, false));
    /// <inheritdoc cref="MSBuildSettings.NodeReuse"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NodeReuse))]
    public static T ToggleNodeReuse<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NodeReuse, !o.NodeReuse));
    #endregion
    #region NoLogo
    /// <inheritdoc cref="MSBuildSettings.NoLogo"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoLogo))]
    public static T SetNoLogo<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoLogo, v));
    /// <inheritdoc cref="MSBuildSettings.NoLogo"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoLogo))]
    public static T ResetNoLogo<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.NoLogo));
    /// <inheritdoc cref="MSBuildSettings.NoLogo"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoLogo))]
    public static T EnableNoLogo<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoLogo, true));
    /// <inheritdoc cref="MSBuildSettings.NoLogo"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoLogo))]
    public static T DisableNoLogo<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoLogo, false));
    /// <inheritdoc cref="MSBuildSettings.NoLogo"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoLogo))]
    public static T ToggleNoLogo<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoLogo, !o.NoLogo));
    #endregion
    #region TargetPlatform
    /// <inheritdoc cref="MSBuildSettings.TargetPlatform"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.TargetPlatform))]
    public static T SetTargetPlatform<T>(this T o, MSBuildTargetPlatform v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.TargetPlatform, v));
    /// <inheritdoc cref="MSBuildSettings.TargetPlatform"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.TargetPlatform))]
    public static T ResetTargetPlatform<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.TargetPlatform));
    #endregion
    #region Properties
    /// <inheritdoc cref="MSBuildSettings.Properties"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetProperties<T>(this T o, IDictionary<string, object> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, v.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase)));
    /// <inheritdoc cref="MSBuildSettings.Properties"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetProperty<T>(this T o, string k, object v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, k, v));
    /// <inheritdoc cref="MSBuildSettings.Properties"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddProperty<T>(this T o, string k, object v) where T : MSBuildSettings => o.Modify(b => b.AddDictionary(() => o.Properties, k, v));
    /// <inheritdoc cref="MSBuildSettings.Properties"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveProperty<T>(this T o, string k) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, k));
    /// <inheritdoc cref="MSBuildSettings.Properties"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ClearProperties<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.ClearDictionary(() => o.Properties));
    #region OutDir
    /// <summary>Specifies the output directory for the project.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetOutDir<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "OutDir", v));
    /// <summary>Specifies the output directory for the project.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetOutDir<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "OutDir"));
    #endregion
    #region RunCodeAnalysis
    /// <summary>Enables or disables code analysis.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRunCodeAnalysis<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RunCodeAnalysis", v));
    /// <summary>Enables or disables code analysis.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRunCodeAnalysis<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RunCodeAnalysis"));
    /// <summary>Enables or disables code analysis.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableRunCodeAnalysis<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RunCodeAnalysis", true));
    /// <summary>Enables or disables code analysis.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableRunCodeAnalysis<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RunCodeAnalysis", false));
    /// <summary>Enables or disables code analysis.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleRunCodeAnalysis<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "RunCodeAnalysis")));
    #endregion
    #region NoWarn
    /// <summary>Suppresses the specified warnings.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetNoWarns<T>(this T o, params int[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "NoWarn", v, ";")));
    /// <summary>Suppresses the specified warnings.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetNoWarns<T>(this T o, IEnumerable<int> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "NoWarn", v, ";")));
    /// <summary>Suppresses the specified warnings.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddNoWarns<T>(this T o, params int[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "NoWarn", v, ";")));
    /// <summary>Suppresses the specified warnings.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddNoWarns<T>(this T o, IEnumerable<int> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "NoWarn", v, ";")));
    /// <summary>Suppresses the specified warnings.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveNoWarns<T>(this T o, params int[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "NoWarn", v, ";")));
    /// <summary>Suppresses the specified warnings.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveNoWarns<T>(this T o, IEnumerable<int> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "NoWarn", v, ";")));
    /// <summary>Suppresses the specified warnings.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetNoWarn<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "NoWarn"));
    #endregion
    #region WarningsAsErrors
    /// <summary>Treats the specified warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetWarningsAsErrors<T>(this T o, params int[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "WarningsAsErrors", v, ";")));
    /// <summary>Treats the specified warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetWarningsAsErrors<T>(this T o, IEnumerable<int> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "WarningsAsErrors", v, ";")));
    /// <summary>Treats the specified warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddWarningsAsErrors<T>(this T o, params int[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "WarningsAsErrors", v, ";")));
    /// <summary>Treats the specified warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddWarningsAsErrors<T>(this T o, IEnumerable<int> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "WarningsAsErrors", v, ";")));
    /// <summary>Treats the specified warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveWarningsAsErrors<T>(this T o, params int[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "WarningsAsErrors", v, ";")));
    /// <summary>Treats the specified warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveWarningsAsErrors<T>(this T o, IEnumerable<int> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "WarningsAsErrors", v, ";")));
    /// <summary>Treats the specified warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetWarningsAsErrors<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "WarningsAsErrors"));
    #endregion
    #region WarningLevel
    /// <summary>Specifies the warning level (0-4).</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetWarningLevel<T>(this T o, int? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "WarningLevel", v));
    /// <summary>Specifies the warning level (0-4).</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetWarningLevel<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "WarningLevel"));
    #endregion
    #region Configuration
    /// <summary>Specifies the build configuration (e.g., Debug or Release).</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetConfiguration<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Configuration", v));
    /// <summary>Specifies the build configuration (e.g., Debug or Release).</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetConfiguration<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "Configuration"));
    #endregion
    #region TreatWarningsAsErrors
    /// <summary>Treats all warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetTreatWarningsAsErrors<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "TreatWarningsAsErrors", v));
    /// <summary>Treats all warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetTreatWarningsAsErrors<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "TreatWarningsAsErrors"));
    /// <summary>Treats all warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableTreatWarningsAsErrors<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "TreatWarningsAsErrors", true));
    /// <summary>Treats all warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableTreatWarningsAsErrors<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "TreatWarningsAsErrors", false));
    /// <summary>Treats all warnings as errors.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleTreatWarningsAsErrors<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "TreatWarningsAsErrors")));
    #endregion
    #region AssemblyVersion
    /// <summary>Specifies the version of the assembly.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetAssemblyVersion<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "AssemblyVersion", v));
    /// <summary>Specifies the version of the assembly.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetAssemblyVersion<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "AssemblyVersion"));
    #endregion
    #region FileVersion
    /// <summary>Specifies the version of the file.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetFileVersion<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "FileVersion", v));
    /// <summary>Specifies the version of the file.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetFileVersion<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "FileVersion"));
    #endregion
    #region InformationalVersion
    /// <summary>Specifies the informational version of the assembly.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetInformationalVersion<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "InformationalVersion", v));
    /// <summary>Specifies the informational version of the assembly.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetInformationalVersion<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "InformationalVersion"));
    #endregion
    #region PackageOutputPath
    /// <summary>Specifies the output path for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageOutputPath<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageOutputPath", v));
    /// <summary>Specifies the output path for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageOutputPath<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageOutputPath"));
    #endregion
    #region IncludeSymbols
    /// <summary>Specifies whether to include symbols in the package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetIncludeSymbols<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "IncludeSymbols", v));
    /// <summary>Specifies whether to include symbols in the package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetIncludeSymbols<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "IncludeSymbols"));
    /// <summary>Specifies whether to include symbols in the package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableIncludeSymbols<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "IncludeSymbols", true));
    /// <summary>Specifies whether to include symbols in the package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableIncludeSymbols<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "IncludeSymbols", false));
    /// <summary>Specifies whether to include symbols in the package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleIncludeSymbols<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "IncludeSymbols")));
    #endregion
    #region PackageId
    /// <summary>Specifies the ID of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageId<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageId", v));
    /// <summary>Specifies the ID of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageId<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageId"));
    #endregion
    #region PackageVersion
    /// <summary>Specifies the version of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageVersion<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageVersion", v));
    /// <summary>Specifies the version of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageVersion<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageVersion"));
    #endregion
    #region PackageVersionPrefix
    /// <summary>Specifies the prefix for the NuGet package version.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageVersionPrefix<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageVersionPrefix", v));
    /// <summary>Specifies the prefix for the NuGet package version.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageVersionPrefix<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageVersionPrefix"));
    #endregion
    #region PackageVersionSuffix
    /// <summary>Specifies the suffix for the NuGet package version.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageVersionSuffix<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageVersionSuffix", v));
    /// <summary>Specifies the suffix for the NuGet package version.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageVersionSuffix<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageVersionSuffix"));
    #endregion
    #region Authors
    /// <summary>Specifies the authors of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetAuthors<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "Authors", v, ",")));
    /// <summary>Specifies the authors of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetAuthors<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "Authors", v, ",")));
    /// <summary>Specifies the authors of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddAuthors<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "Authors", v, ",")));
    /// <summary>Specifies the authors of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddAuthors<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "Authors", v, ",")));
    /// <summary>Specifies the authors of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveAuthors<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "Authors", v, ",")));
    /// <summary>Specifies the authors of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveAuthors<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "Authors", v, ",")));
    /// <summary>Specifies the authors of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetAuthors<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "Authors"));
    #endregion
    #region Title
    /// <summary>Specifies the title of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetTitle<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Title", v));
    /// <summary>Specifies the title of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetTitle<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "Title"));
    #endregion
    #region Description
    /// <summary>Specifies the description of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetDescription<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Description", v));
    /// <summary>Specifies the description of the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetDescription<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "Description"));
    #endregion
    #region Copyright
    /// <summary>Specifies the copyright notice for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetCopyright<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Copyright", v));
    /// <summary>Specifies the copyright notice for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetCopyright<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "Copyright"));
    #endregion
    #region PackageRequireLicenseAcceptance
    /// <summary>Specifies whether the package requires license acceptance.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageRequireLicenseAcceptance<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageRequireLicenseAcceptance", v));
    /// <summary>Specifies whether the package requires license acceptance.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageRequireLicenseAcceptance<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageRequireLicenseAcceptance"));
    /// <summary>Specifies whether the package requires license acceptance.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnablePackageRequireLicenseAcceptance<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageRequireLicenseAcceptance", true));
    /// <summary>Specifies whether the package requires license acceptance.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisablePackageRequireLicenseAcceptance<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageRequireLicenseAcceptance", false));
    /// <summary>Specifies whether the package requires license acceptance.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T TogglePackageRequireLicenseAcceptance<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "PackageRequireLicenseAcceptance")));
    #endregion
    #region PackageLicenseUrl
    /// <summary>Specifies the URL for the package's license.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageLicenseUrl<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageLicenseUrl", v));
    /// <summary>Specifies the URL for the package's license.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageLicenseUrl<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageLicenseUrl"));
    #endregion
    #region PackageProjectUrl
    /// <summary>Specifies the URL for the package's project page.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageProjectUrl<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageProjectUrl", v));
    /// <summary>Specifies the URL for the package's project page.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageProjectUrl<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageProjectUrl"));
    #endregion
    #region PackageIconUrl
    /// <summary>Specifies the URL for the package's icon.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageIconUrl<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageIconUrl", v));
    /// <summary>Specifies the URL for the package's icon.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageIconUrl<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageIconUrl"));
    #endregion
    #region PackageTags
    /// <summary>Specifies the tags for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageTags<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "PackageTags", v, " ")));
    /// <summary>Specifies the tags for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageTags<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "PackageTags", v, " ")));
    /// <summary>Specifies the tags for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddPackageTags<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "PackageTags", v, " ")));
    /// <summary>Specifies the tags for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddPackageTags<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "PackageTags", v, " ")));
    /// <summary>Specifies the tags for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemovePackageTags<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "PackageTags", v, " ")));
    /// <summary>Specifies the tags for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemovePackageTags<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "PackageTags", v, " ")));
    /// <summary>Specifies the tags for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageTags<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageTags"));
    #endregion
    #region PackageReleaseNotes
    /// <summary>Specifies the release notes for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPackageReleaseNotes<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PackageReleaseNotes", v));
    /// <summary>Specifies the release notes for the NuGet package.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPackageReleaseNotes<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PackageReleaseNotes"));
    #endregion
    #region RepositoryUrl
    /// <summary>Specifies the URL for the package's repository.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRepositoryUrl<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RepositoryUrl", v));
    /// <summary>Specifies the URL for the package's repository.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRepositoryUrl<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RepositoryUrl"));
    #endregion
    #region RepositoryType
    /// <summary>Specifies the type of the package's repository.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRepositoryType<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RepositoryType", v));
    /// <summary>Specifies the type of the package's repository.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRepositoryType<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RepositoryType"));
    #endregion
    #region RestoreSources
    /// <summary>List of package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreSources<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "RestoreSources", v, ";")));
    /// <summary>List of package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreSources<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "RestoreSources", v, ";")));
    /// <summary>List of package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddRestoreSources<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "RestoreSources", v, ";")));
    /// <summary>List of package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddRestoreSources<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "RestoreSources", v, ";")));
    /// <summary>List of package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveRestoreSources<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "RestoreSources", v, ";")));
    /// <summary>List of package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveRestoreSources<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "RestoreSources", v, ";")));
    /// <summary>List of package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreSources<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreSources"));
    #endregion
    #region RestorePackagesPath
    /// <summary>User packages folder path.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestorePackagesPath<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestorePackagesPath", v));
    /// <summary>User packages folder path.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestorePackagesPath<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestorePackagesPath"));
    #endregion
    #region RestoreDisableParallel
    /// <summary>Limit downloads to one at a time.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreDisableParallel<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreDisableParallel", v));
    /// <summary>Limit downloads to one at a time.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreDisableParallel<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreDisableParallel"));
    /// <summary>Limit downloads to one at a time.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableRestoreDisableParallel<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreDisableParallel", true));
    /// <summary>Limit downloads to one at a time.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableRestoreDisableParallel<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreDisableParallel", false));
    /// <summary>Limit downloads to one at a time.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleRestoreDisableParallel<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "RestoreDisableParallel")));
    #endregion
    #region RestoreConfigFile
    /// <summary>Path to a Nuget.Config file to apply.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreConfigFile<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreConfigFile", v));
    /// <summary>Path to a Nuget.Config file to apply.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreConfigFile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreConfigFile"));
    #endregion
    #region RestoreNoCache
    /// <summary>If true, avoids using the web cache.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreNoCache<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreNoCache", v));
    /// <summary>If true, avoids using the web cache.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreNoCache<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreNoCache"));
    /// <summary>If true, avoids using the web cache.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableRestoreNoCache<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreNoCache", true));
    /// <summary>If true, avoids using the web cache.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableRestoreNoCache<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreNoCache", false));
    /// <summary>If true, avoids using the web cache.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleRestoreNoCache<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "RestoreNoCache")));
    #endregion
    #region RestoreIgnoreFailedSources
    /// <summary>If true, ignores failing or missing package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreIgnoreFailedSources<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreIgnoreFailedSources", v));
    /// <summary>If true, ignores failing or missing package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreIgnoreFailedSources<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreIgnoreFailedSources"));
    /// <summary>If true, ignores failing or missing package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableRestoreIgnoreFailedSources<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreIgnoreFailedSources", true));
    /// <summary>If true, ignores failing or missing package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableRestoreIgnoreFailedSources<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreIgnoreFailedSources", false));
    /// <summary>If true, ignores failing or missing package sources.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleRestoreIgnoreFailedSources<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "RestoreIgnoreFailedSources")));
    #endregion
    #region RestoreTaskAssemblyFile
    /// <summary>Path to <c>NuGet.Build.Tasks.dll</c>.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreTaskAssemblyFile<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreTaskAssemblyFile", v));
    /// <summary>Path to <c>NuGet.Build.Tasks.dll</c>.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreTaskAssemblyFile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreTaskAssemblyFile"));
    #endregion
    #region RestoreGraphProjectInput
    /// <summary>Semicolon-delimited list of projects to restore, which should contain absolute paths.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreGraphProjectInputs<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "RestoreGraphProjectInput", v, ";")));
    /// <summary>Semicolon-delimited list of projects to restore, which should contain absolute paths.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreGraphProjectInputs<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.SetCollection(o.Properties, "RestoreGraphProjectInput", v, ";")));
    /// <summary>Semicolon-delimited list of projects to restore, which should contain absolute paths.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddRestoreGraphProjectInputs<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "RestoreGraphProjectInput", v, ";")));
    /// <summary>Semicolon-delimited list of projects to restore, which should contain absolute paths.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T AddRestoreGraphProjectInputs<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.AddCollection(o.Properties, "RestoreGraphProjectInput", v, ";")));
    /// <summary>Semicolon-delimited list of projects to restore, which should contain absolute paths.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveRestoreGraphProjectInputs<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "RestoreGraphProjectInput", v, ";")));
    /// <summary>Semicolon-delimited list of projects to restore, which should contain absolute paths.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T RemoveRestoreGraphProjectInputs<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.RemoveCollection(o.Properties, "RestoreGraphProjectInput", v, ";")));
    /// <summary>Semicolon-delimited list of projects to restore, which should contain absolute paths.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreGraphProjectInput<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreGraphProjectInput"));
    #endregion
    #region RestoreOutputPath
    /// <summary>Output folder, defaulting to the obj folder.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRestoreOutputPath<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RestoreOutputPath", v));
    /// <summary>Output folder, defaulting to the obj folder.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRestoreOutputPath<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RestoreOutputPath"));
    #endregion
    #region SymbolPackageFormat
    /// <summary>Format for packaging symbols.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetSymbolPackageFormat<T>(this T o, MSBuildSymbolPackageFormat v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SymbolPackageFormat", v));
    /// <summary>Format for packaging symbols.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetSymbolPackageFormat<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "SymbolPackageFormat"));
    #endregion
    #region PublishProfile
    /// <summary>Name of ClickOnce publication .pubxml file found in project properties folder.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPublishProfile<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishProfile", v));
    /// <summary>Name of ClickOnce publication .pubxml file found in project properties folder.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPublishProfile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PublishProfile"));
    #endregion
    #region PublishDir
    /// <summary>Path of publication directory.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPublishDir<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishDir", v));
    /// <summary>Path of publication directory.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPublishDir<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PublishDir"));
    #endregion
    #region RuntimeIdentifiers
    /// <summary><p>RuntimeIdentifiers of RFID see <a href="https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#using-rids">.NET RID Catalog</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetRuntimeIdentifiers<T>(this T o, RuntimeIdentifiers v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "RuntimeIdentifiers", v));
    /// <summary><p>RuntimeIdentifiers of RFID see <a href="https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#using-rids">.NET RID Catalog</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetRuntimeIdentifiers<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "RuntimeIdentifiers"));
    #endregion
    #region UpdateUrl
    /// <summary>Optional. Set its value if different from InstallURL.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetUpdateUrl<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "UpdateUrl", v));
    /// <summary>Optional. Set its value if different from InstallURL.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetUpdateUrl<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "UpdateUrl"));
    #endregion
    #region BootstrapperEnabled
    /// <summary><p>Determines whether to generate the setup.exe bootstrapper. <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-clickonce-applications-from-the-command-line?view=vs-2022">Build ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetBootstrapperEnabled<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "BootstrapperEnabled", v));
    /// <summary><p>Determines whether to generate the setup.exe bootstrapper. <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-clickonce-applications-from-the-command-line?view=vs-2022">Build ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetBootstrapperEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "BootstrapperEnabled"));
    /// <summary><p>Determines whether to generate the setup.exe bootstrapper. <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-clickonce-applications-from-the-command-line?view=vs-2022">Build ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableBootstrapperEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "BootstrapperEnabled", true));
    /// <summary><p>Determines whether to generate the setup.exe bootstrapper. <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-clickonce-applications-from-the-command-line?view=vs-2022">Build ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableBootstrapperEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "BootstrapperEnabled", false));
    /// <summary><p>Determines whether to generate the setup.exe bootstrapper. <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-clickonce-applications-from-the-command-line?view=vs-2022">Build ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleBootstrapperEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "BootstrapperEnabled")));
    #endregion
    #region GenerateManifests
    /// <summary>GenerateManifests and SignManifests must be true to work.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetGenerateManifests<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "GenerateManifests", v));
    /// <summary>GenerateManifests and SignManifests must be true to work.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetGenerateManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "GenerateManifests"));
    /// <summary>GenerateManifests and SignManifests must be true to work.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableGenerateManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "GenerateManifests", true));
    /// <summary>GenerateManifests and SignManifests must be true to work.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableGenerateManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "GenerateManifests", false));
    /// <summary>GenerateManifests and SignManifests must be true to work.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleGenerateManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "GenerateManifests")));
    #endregion
    #region Platform
    /// <summary><p>Output assembly platform <a href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/output">C# Compiler Options that control compiler output</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPlatform<T>(this T o, Platform v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Platform", v));
    /// <summary><p>Output assembly platform <a href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/output">C# Compiler Options that control compiler output</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPlatform<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "Platform"));
    #endregion
    #region TargetFramework
    /// <summary><p>Set TargetFramework of output assembly. <a href="https://learn.microsoft.com/en-us/dotnet/standard/frameworks">Target frameworks in SDK-style projects</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetTargetFramework<T>(this T o, TargetFramework v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "TargetFramework", v));
    /// <summary><p>Set TargetFramework of output assembly. <a href="https://learn.microsoft.com/en-us/dotnet/standard/frameworks">Target frameworks in SDK-style projects</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetTargetFramework<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "TargetFramework"));
    #endregion
    #region ApplicationVersion
    /// <summary>Specifies the application version for ClickOnce deployment.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetApplicationVersion<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "ApplicationVersion", v));
    /// <summary>Specifies the application version for ClickOnce deployment.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetApplicationVersion<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "ApplicationVersion"));
    #endregion
    #region ApplicationRevision
    /// <summary>Specifies the application revision for ClickOnce deployment.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetApplicationRevision<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "ApplicationRevision", v));
    /// <summary>Specifies the application revision for ClickOnce deployment.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetApplicationRevision<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "ApplicationRevision"));
    #endregion
    #region CreateWebPageOnPublish
    /// <summary>Specifies whether to create a web page when publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetCreateWebPageOnPublish<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "CreateWebPageOnPublish", v));
    /// <summary>Specifies whether to create a web page when publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetCreateWebPageOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "CreateWebPageOnPublish"));
    /// <summary>Specifies whether to create a web page when publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableCreateWebPageOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "CreateWebPageOnPublish", true));
    /// <summary>Specifies whether to create a web page when publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableCreateWebPageOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "CreateWebPageOnPublish", false));
    /// <summary>Specifies whether to create a web page when publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleCreateWebPageOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "CreateWebPageOnPublish")));
    #endregion
    #region Install
    /// <summary><p>Determines whether the application is an installed application or a run-from-Web application <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-dotnet-clickonce-applications-from-the-command-line?view=vs-2022">Build .NET ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetInstall<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Install", v));
    /// <summary><p>Determines whether the application is an installed application or a run-from-Web application <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-dotnet-clickonce-applications-from-the-command-line?view=vs-2022">Build .NET ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetInstall<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "Install"));
    /// <summary><p>Determines whether the application is an installed application or a run-from-Web application <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-dotnet-clickonce-applications-from-the-command-line?view=vs-2022">Build .NET ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableInstall<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Install", true));
    /// <summary><p>Determines whether the application is an installed application or a run-from-Web application <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-dotnet-clickonce-applications-from-the-command-line?view=vs-2022">Build .NET ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableInstall<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "Install", false));
    /// <summary><p>Determines whether the application is an installed application or a run-from-Web application <a href="https://learn.microsoft.com/en-us/visualstudio/deployment/building-dotnet-clickonce-applications-from-the-command-line?view=vs-2022">Build .NET ClickOnce applications from the command line</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleInstall<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "Install")));
    #endregion
    #region InstallFrom
    /// <summary><p>Possible installation source <a href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.managedinterfaces.publish.installfrom?view=visualstudiosdk-2022">InstallFrom Enum</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetInstallFrom<T>(this T o, InstallFrom v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "InstallFrom", v));
    /// <summary><p>Possible installation source <a href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.managedinterfaces.publish.installfrom?view=visualstudiosdk-2022">InstallFrom Enum</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetInstallFrom<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "InstallFrom"));
    #endregion
    #region MapFileExtensions
    /// <summary><p>Optional. Defaults to false. If true, all files in the deployment must have a ".deploy" extension. <a href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.build.tasks.deployment.manifestutilities.deploymanifest.mapfileextensions?view=msbuild-17-netcore">DeployManifest.MapFileExtensions Property</a> </p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetMapFileExtensions<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "MapFileExtensions", v));
    /// <summary><p>Optional. Defaults to false. If true, all files in the deployment must have a ".deploy" extension. <a href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.build.tasks.deployment.manifestutilities.deploymanifest.mapfileextensions?view=msbuild-17-netcore">DeployManifest.MapFileExtensions Property</a> </p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetMapFileExtensions<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "MapFileExtensions"));
    /// <summary><p>Optional. Defaults to false. If true, all files in the deployment must have a ".deploy" extension. <a href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.build.tasks.deployment.manifestutilities.deploymanifest.mapfileextensions?view=msbuild-17-netcore">DeployManifest.MapFileExtensions Property</a> </p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableMapFileExtensions<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "MapFileExtensions", true));
    /// <summary><p>Optional. Defaults to false. If true, all files in the deployment must have a ".deploy" extension. <a href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.build.tasks.deployment.manifestutilities.deploymanifest.mapfileextensions?view=msbuild-17-netcore">DeployManifest.MapFileExtensions Property</a> </p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableMapFileExtensions<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "MapFileExtensions", false));
    /// <summary><p>Optional. Defaults to false. If true, all files in the deployment must have a ".deploy" extension. <a href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.build.tasks.deployment.manifestutilities.deploymanifest.mapfileextensions?view=msbuild-17-netcore">DeployManifest.MapFileExtensions Property</a> </p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleMapFileExtensions<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "MapFileExtensions")));
    #endregion
    #region OpenBrowserOnPublish
    /// <summary><p> <a href="https://learn.microsoft.com/es-es/dotnet/api/microsoft.visualstudio.managedinterfaces.publish.ipublishproperties3.openbrowseronpublish?view=visualstudiosdk-2022#microsoft-visualstudio-managedinterfaces-publish-ipublishproperties3-openbrowseronpublish" >OpenBrowserOnPublish</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetOpenBrowserOnPublish<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "OpenBrowserOnPublish", v));
    /// <summary><p> <a href="https://learn.microsoft.com/es-es/dotnet/api/microsoft.visualstudio.managedinterfaces.publish.ipublishproperties3.openbrowseronpublish?view=visualstudiosdk-2022#microsoft-visualstudio-managedinterfaces-publish-ipublishproperties3-openbrowseronpublish" >OpenBrowserOnPublish</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetOpenBrowserOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "OpenBrowserOnPublish"));
    /// <summary><p> <a href="https://learn.microsoft.com/es-es/dotnet/api/microsoft.visualstudio.managedinterfaces.publish.ipublishproperties3.openbrowseronpublish?view=visualstudiosdk-2022#microsoft-visualstudio-managedinterfaces-publish-ipublishproperties3-openbrowseronpublish" >OpenBrowserOnPublish</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableOpenBrowserOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "OpenBrowserOnPublish", true));
    /// <summary><p> <a href="https://learn.microsoft.com/es-es/dotnet/api/microsoft.visualstudio.managedinterfaces.publish.ipublishproperties3.openbrowseronpublish?view=visualstudiosdk-2022#microsoft-visualstudio-managedinterfaces-publish-ipublishproperties3-openbrowseronpublish" >OpenBrowserOnPublish</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableOpenBrowserOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "OpenBrowserOnPublish", false));
    /// <summary><p> <a href="https://learn.microsoft.com/es-es/dotnet/api/microsoft.visualstudio.managedinterfaces.publish.ipublishproperties3.openbrowseronpublish?view=visualstudiosdk-2022#microsoft-visualstudio-managedinterfaces-publish-ipublishproperties3-openbrowseronpublish" >OpenBrowserOnPublish</a></p></summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleOpenBrowserOnPublish<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "OpenBrowserOnPublish")));
    #endregion
    #region PublishProtocol
    /// <summary>Default is "ClickOnce"</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPublishProtocol<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishProtocol", v));
    /// <summary>Default is "ClickOnce"</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPublishProtocol<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PublishProtocol"));
    #endregion
    #region PublishReadyToRun
    /// <summary>Compiles application assemblies as ReadyToRun (R2R) format. R2R is a form of ahead-of-time (AOT) compilation.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPublishReadyToRun<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishReadyToRun", v));
    /// <summary>Compiles application assemblies as ReadyToRun (R2R) format. R2R is a form of ahead-of-time (AOT) compilation.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPublishReadyToRun<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PublishReadyToRun"));
    /// <summary>Compiles application assemblies as ReadyToRun (R2R) format. R2R is a form of ahead-of-time (AOT) compilation.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnablePublishReadyToRun<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishReadyToRun", true));
    /// <summary>Compiles application assemblies as ReadyToRun (R2R) format. R2R is a form of ahead-of-time (AOT) compilation.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisablePublishReadyToRun<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishReadyToRun", false));
    /// <summary>Compiles application assemblies as ReadyToRun (R2R) format. R2R is a form of ahead-of-time (AOT) compilation.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T TogglePublishReadyToRun<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "PublishReadyToRun")));
    #endregion
    #region PublishSingleFile
    /// <summary>Packages the app into a platform-specific single-file executable.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetPublishSingleFile<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishSingleFile", v));
    /// <summary>Packages the app into a platform-specific single-file executable.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetPublishSingleFile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "PublishSingleFile"));
    /// <summary>Packages the app into a platform-specific single-file executable.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnablePublishSingleFile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishSingleFile", true));
    /// <summary>Packages the app into a platform-specific single-file executable.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisablePublishSingleFile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "PublishSingleFile", false));
    /// <summary>Packages the app into a platform-specific single-file executable.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T TogglePublishSingleFile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "PublishSingleFile")));
    #endregion
    #region SelfContained
    /// <summary>Specifies whether to publish the .NET runtime with your application so the runtime doesn't need to be installed on the target machine.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetSelfContained<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SelfContained", v));
    /// <summary>Specifies whether to publish the .NET runtime with your application so the runtime doesn't need to be installed on the target machine.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetSelfContained<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "SelfContained"));
    /// <summary>Specifies whether to publish the .NET runtime with your application so the runtime doesn't need to be installed on the target machine.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableSelfContained<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SelfContained", true));
    /// <summary>Specifies whether to publish the .NET runtime with your application so the runtime doesn't need to be installed on the target machine.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableSelfContained<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SelfContained", false));
    /// <summary>Specifies whether to publish the .NET runtime with your application so the runtime doesn't need to be installed on the target machine.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleSelfContained<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "SelfContained")));
    #endregion
    #region SignManifests
    /// <summary>Specifies whether to sign the ClickOnce manifests.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetSignManifests<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SignManifests", v));
    /// <summary>Specifies whether to sign the ClickOnce manifests.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetSignManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "SignManifests"));
    /// <summary>Specifies whether to sign the ClickOnce manifests.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableSignManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SignManifests", true));
    /// <summary>Specifies whether to sign the ClickOnce manifests.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableSignManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SignManifests", false));
    /// <summary>Specifies whether to sign the ClickOnce manifests.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleSignManifests<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "SignManifests")));
    #endregion
    #region SkipPublishVerification
    /// <summary>Specifies whether to skip verification after publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetSkipPublishVerification<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SkipPublishVerification", v));
    /// <summary>Specifies whether to skip verification after publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetSkipPublishVerification<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "SkipPublishVerification"));
    /// <summary>Specifies whether to skip verification after publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableSkipPublishVerification<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SkipPublishVerification", true));
    /// <summary>Specifies whether to skip verification after publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableSkipPublishVerification<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "SkipPublishVerification", false));
    /// <summary>Specifies whether to skip verification after publishing.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleSkipPublishVerification<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "SkipPublishVerification")));
    #endregion
    #region UpdateEnabled
    /// <summary>Specifies whether ClickOnce updates are enabled.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetUpdateEnabled<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "UpdateEnabled", v));
    /// <summary>Specifies whether ClickOnce updates are enabled.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetUpdateEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "UpdateEnabled"));
    /// <summary>Specifies whether ClickOnce updates are enabled.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableUpdateEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "UpdateEnabled", true));
    /// <summary>Specifies whether ClickOnce updates are enabled.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableUpdateEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "UpdateEnabled", false));
    /// <summary>Specifies whether ClickOnce updates are enabled.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleUpdateEnabled<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "UpdateEnabled")));
    #endregion
    #region ManifestKeyFile
    /// <summary>Specifies the key file for signing the manifests.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetManifestKeyFile<T>(this T o, string v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "ManifestKeyFile", v));
    /// <summary>Specifies the key file for signing the manifests.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetManifestKeyFile<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "ManifestKeyFile"));
    #endregion
    #region CreateDesktopShortcut
    /// <summary>Specifies whether to create a desktop shortcut for the ClickOnce application.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetCreateDesktopShortcut<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "CreateDesktopShortcut", v));
    /// <summary>Specifies whether to create a desktop shortcut for the ClickOnce application.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetCreateDesktopShortcut<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "CreateDesktopShortcut"));
    /// <summary>Specifies whether to create a desktop shortcut for the ClickOnce application.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableCreateDesktopShortcut<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "CreateDesktopShortcut", true));
    /// <summary>Specifies whether to create a desktop shortcut for the ClickOnce application.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableCreateDesktopShortcut<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "CreateDesktopShortcut", false));
    /// <summary>Specifies whether to create a desktop shortcut for the ClickOnce application.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleCreateDesktopShortcut<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "CreateDesktopShortcut")));
    #endregion
    #region UpdateRequired
    /// <summary>Specifies whether a ClickOnce update is required.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T SetUpdateRequired<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "UpdateRequired", v));
    /// <summary>Specifies whether a ClickOnce update is required.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ResetUpdateRequired<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.RemoveDictionary(() => o.Properties, "UpdateRequired"));
    /// <summary>Specifies whether a ClickOnce update is required.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T EnableUpdateRequired<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "UpdateRequired", true));
    /// <summary>Specifies whether a ClickOnce update is required.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T DisableUpdateRequired<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.SetDictionary(() => o.Properties, "UpdateRequired", false));
    /// <summary>Specifies whether a ClickOnce update is required.</summary>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Properties))]
    public static T ToggleUpdateRequired<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Properties, DelegateHelper.Toggle(o.Properties, "UpdateRequired")));
    #endregion
    #endregion
    #region Restore
    /// <inheritdoc cref="MSBuildSettings.Restore"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Restore))]
    public static T SetRestore<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Restore, v));
    /// <inheritdoc cref="MSBuildSettings.Restore"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Restore))]
    public static T ResetRestore<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.Restore));
    /// <inheritdoc cref="MSBuildSettings.Restore"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Restore))]
    public static T EnableRestore<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Restore, true));
    /// <inheritdoc cref="MSBuildSettings.Restore"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Restore))]
    public static T DisableRestore<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Restore, false));
    /// <inheritdoc cref="MSBuildSettings.Restore"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Restore))]
    public static T ToggleRestore<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Restore, !o.Restore));
    #endregion
    #region Targets
    /// <inheritdoc cref="MSBuildSettings.Targets"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Targets))]
    public static T SetTargets<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Targets, v));
    /// <inheritdoc cref="MSBuildSettings.Targets"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Targets))]
    public static T SetTargets<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Targets, v));
    /// <inheritdoc cref="MSBuildSettings.Targets"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Targets))]
    public static T AddTargets<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.AddCollection(() => o.Targets, v));
    /// <inheritdoc cref="MSBuildSettings.Targets"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Targets))]
    public static T AddTargets<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.AddCollection(() => o.Targets, v));
    /// <inheritdoc cref="MSBuildSettings.Targets"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Targets))]
    public static T RemoveTargets<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.RemoveCollection(() => o.Targets, v));
    /// <inheritdoc cref="MSBuildSettings.Targets"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Targets))]
    public static T RemoveTargets<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.RemoveCollection(() => o.Targets, v));
    /// <inheritdoc cref="MSBuildSettings.Targets"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Targets))]
    public static T ClearTargets<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.ClearCollection(() => o.Targets));
    #endregion
    #region ToolsVersion
    /// <inheritdoc cref="MSBuildSettings.ToolsVersion"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.ToolsVersion))]
    public static T SetToolsVersion<T>(this T o, MSBuildToolsVersion v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.ToolsVersion, v));
    /// <inheritdoc cref="MSBuildSettings.ToolsVersion"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.ToolsVersion))]
    public static T ResetToolsVersion<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.ToolsVersion));
    #endregion
    #region MSBuildVersion
    /// <inheritdoc cref="MSBuildSettings.MSBuildVersion"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.MSBuildVersion))]
    public static T SetMSBuildVersion<T>(this T o, MSBuildVersion? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.MSBuildVersion, v));
    /// <inheritdoc cref="MSBuildSettings.MSBuildVersion"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.MSBuildVersion))]
    public static T ResetMSBuildVersion<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.MSBuildVersion));
    #endregion
    #region Verbosity
    /// <inheritdoc cref="MSBuildSettings.Verbosity"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Verbosity))]
    public static T SetVerbosity<T>(this T o, MSBuildVerbosity v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Verbosity, v));
    /// <inheritdoc cref="MSBuildSettings.Verbosity"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Verbosity))]
    public static T ResetVerbosity<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.Verbosity));
    #endregion
    #region MSBuildPlatform
    /// <inheritdoc cref="MSBuildSettings.MSBuildPlatform"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.MSBuildPlatform))]
    public static T SetMSBuildPlatform<T>(this T o, MSBuildPlatform? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.MSBuildPlatform, v));
    /// <inheritdoc cref="MSBuildSettings.MSBuildPlatform"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.MSBuildPlatform))]
    public static T ResetMSBuildPlatform<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.MSBuildPlatform));
    #endregion
    #region Loggers
    /// <inheritdoc cref="MSBuildSettings.Loggers"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Loggers))]
    public static T SetLoggers<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Loggers, v));
    /// <inheritdoc cref="MSBuildSettings.Loggers"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Loggers))]
    public static T SetLoggers<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.Loggers, v));
    /// <inheritdoc cref="MSBuildSettings.Loggers"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Loggers))]
    public static T AddLoggers<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.AddCollection(() => o.Loggers, v));
    /// <inheritdoc cref="MSBuildSettings.Loggers"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Loggers))]
    public static T AddLoggers<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.AddCollection(() => o.Loggers, v));
    /// <inheritdoc cref="MSBuildSettings.Loggers"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Loggers))]
    public static T RemoveLoggers<T>(this T o, params string[] v) where T : MSBuildSettings => o.Modify(b => b.RemoveCollection(() => o.Loggers, v));
    /// <inheritdoc cref="MSBuildSettings.Loggers"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Loggers))]
    public static T RemoveLoggers<T>(this T o, IEnumerable<string> v) where T : MSBuildSettings => o.Modify(b => b.RemoveCollection(() => o.Loggers, v));
    /// <inheritdoc cref="MSBuildSettings.Loggers"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.Loggers))]
    public static T ClearLoggers<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.ClearCollection(() => o.Loggers));
    #endregion
    #region NoConsoleLogger
    /// <inheritdoc cref="MSBuildSettings.NoConsoleLogger"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoConsoleLogger))]
    public static T SetNoConsoleLogger<T>(this T o, bool? v) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoConsoleLogger, v));
    /// <inheritdoc cref="MSBuildSettings.NoConsoleLogger"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoConsoleLogger))]
    public static T ResetNoConsoleLogger<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Remove(() => o.NoConsoleLogger));
    /// <inheritdoc cref="MSBuildSettings.NoConsoleLogger"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoConsoleLogger))]
    public static T EnableNoConsoleLogger<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoConsoleLogger, true));
    /// <inheritdoc cref="MSBuildSettings.NoConsoleLogger"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoConsoleLogger))]
    public static T DisableNoConsoleLogger<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoConsoleLogger, false));
    /// <inheritdoc cref="MSBuildSettings.NoConsoleLogger"/>
    [Builder(Type = typeof(MSBuildSettings), Property = nameof(MSBuildSettings.NoConsoleLogger))]
    public static T ToggleNoConsoleLogger<T>(this T o) where T : MSBuildSettings => o.Modify(b => b.Set(() => o.NoConsoleLogger, !o.NoConsoleLogger));
    #endregion
}
#endregion
#region MSBuildToolsVersion
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<MSBuildToolsVersion>))]
public partial class MSBuildToolsVersion : Enumeration
{
    public static MSBuildToolsVersion _2_0 = (MSBuildToolsVersion) "2.0";
    public static MSBuildToolsVersion _3_5 = (MSBuildToolsVersion) "3.5";
    public static MSBuildToolsVersion _4_0 = (MSBuildToolsVersion) "4.0";
    public static MSBuildToolsVersion _12_0 = (MSBuildToolsVersion) "12.0";
    public static MSBuildToolsVersion _14_0 = (MSBuildToolsVersion) "14.0";
    public static MSBuildToolsVersion _15_0 = (MSBuildToolsVersion) "15.0";
    public static implicit operator MSBuildToolsVersion(string value)
    {
        return new MSBuildToolsVersion { Value = value };
    }
}
#endregion
#region MSBuildVerbosity
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<MSBuildVerbosity>))]
public partial class MSBuildVerbosity : Enumeration
{
    public static MSBuildVerbosity Quiet = (MSBuildVerbosity) "Quiet";
    public static MSBuildVerbosity Minimal = (MSBuildVerbosity) "Minimal";
    public static MSBuildVerbosity Normal = (MSBuildVerbosity) "Normal";
    public static MSBuildVerbosity Detailed = (MSBuildVerbosity) "Detailed";
    public static MSBuildVerbosity Diagnostic = (MSBuildVerbosity) "Diagnostic";
    public static implicit operator MSBuildVerbosity(string value)
    {
        return new MSBuildVerbosity { Value = value };
    }
}
#endregion
#region MSBuildTargetPlatform
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<MSBuildTargetPlatform>))]
public partial class MSBuildTargetPlatform : Enumeration
{
    public static MSBuildTargetPlatform MSIL = (MSBuildTargetPlatform) "MSIL";
    public static MSBuildTargetPlatform x86 = (MSBuildTargetPlatform) "x86";
    public static MSBuildTargetPlatform x64 = (MSBuildTargetPlatform) "x64";
    public static MSBuildTargetPlatform arm = (MSBuildTargetPlatform) "arm";
    public static MSBuildTargetPlatform Win32 = (MSBuildTargetPlatform) "Win32";
    public static implicit operator MSBuildTargetPlatform(string value)
    {
        return new MSBuildTargetPlatform { Value = value };
    }
}
#endregion
#region MSBuildSymbolPackageFormat
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<MSBuildSymbolPackageFormat>))]
public partial class MSBuildSymbolPackageFormat : Enumeration
{
    public static MSBuildSymbolPackageFormat symbols_nupkg = (MSBuildSymbolPackageFormat) "symbols.nupkg";
    public static MSBuildSymbolPackageFormat snupkg = (MSBuildSymbolPackageFormat) "snupkg";
    public static implicit operator MSBuildSymbolPackageFormat(string value)
    {
        return new MSBuildSymbolPackageFormat { Value = value };
    }
}
#endregion
#region RuntimeIdentifiers
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<RuntimeIdentifiers>))]
public partial class RuntimeIdentifiers : Enumeration
{
    public static RuntimeIdentifiers android_arm64 = (RuntimeIdentifiers) "android-arm64";
    public static RuntimeIdentifiers android_arm = (RuntimeIdentifiers) "android-arm";
    public static RuntimeIdentifiers android_x64 = (RuntimeIdentifiers) "android-x64";
    public static RuntimeIdentifiers android_x86 = (RuntimeIdentifiers) "android-x86";
    public static RuntimeIdentifiers ios_arm64 = (RuntimeIdentifiers) "ios-arm64";
    public static RuntimeIdentifiers iossimulator_arm64 = (RuntimeIdentifiers) "iossimulator-arm64";
    public static RuntimeIdentifiers iossimulator_x64 = (RuntimeIdentifiers) "iossimulator-x64";
    public static RuntimeIdentifiers linux_x64 = (RuntimeIdentifiers) "linux-x64";
    public static RuntimeIdentifiers linux_musl_x64 = (RuntimeIdentifiers) "linux-musl-x64";
    public static RuntimeIdentifiers linux_musl_arm64 = (RuntimeIdentifiers) "linux-musl-arm64";
    public static RuntimeIdentifiers linux_arm = (RuntimeIdentifiers) "linux-arm";
    public static RuntimeIdentifiers linux_arm64 = (RuntimeIdentifiers) "linux-arm64";
    public static RuntimeIdentifiers linux_bionic_arm64 = (RuntimeIdentifiers) "linux-bionic-arm64";
    public static RuntimeIdentifiers linux_loongarch64 = (RuntimeIdentifiers) "linux-loongarch64";
    public static RuntimeIdentifiers osx_arm64 = (RuntimeIdentifiers) "osx-arm64";
    public static RuntimeIdentifiers osx_x64 = (RuntimeIdentifiers) "osx-x64";
    public static RuntimeIdentifiers win_arm64 = (RuntimeIdentifiers) "win-arm64";
    public static RuntimeIdentifiers win_x64 = (RuntimeIdentifiers) "win-x64";
    public static RuntimeIdentifiers win_x86 = (RuntimeIdentifiers) "win-x86";
    public static implicit operator RuntimeIdentifiers(string value)
    {
        return new RuntimeIdentifiers { Value = value };
    }
}
#endregion
#region Platform
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<Platform>))]
public partial class Platform : Enumeration
{
    public static Platform anycpu = (Platform) "anycpu";
    public static Platform anycpu32bitpreferred = (Platform) "anycpu32bitpreferred";
    public static Platform arm = (Platform) "arm";
    public static Platform Itanium = (Platform) "Itanium";
    public static Platform x64 = (Platform) "x64";
    public static Platform x86 = (Platform) "x86";
    public static implicit operator Platform(string value)
    {
        return new Platform { Value = value };
    }
}
#endregion
#region TargetFramework
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<TargetFramework>))]
public partial class TargetFramework : Enumeration
{
    public static TargetFramework NETFRAMEWORK = (TargetFramework) "NETFRAMEWORK";
    public static TargetFramework NET481 = (TargetFramework) "NET481";
    public static TargetFramework NET48 = (TargetFramework) "NET48";
    public static TargetFramework NET472 = (TargetFramework) "NET472";
    public static TargetFramework NET471 = (TargetFramework) "NET471";
    public static TargetFramework NET47 = (TargetFramework) "NET47";
    public static TargetFramework NET462 = (TargetFramework) "NET462";
    public static TargetFramework NET461 = (TargetFramework) "NET461";
    public static TargetFramework NET46 = (TargetFramework) "NET46";
    public static TargetFramework NET452 = (TargetFramework) "NET452";
    public static TargetFramework NET451 = (TargetFramework) "NET451";
    public static TargetFramework NET45 = (TargetFramework) "NET45";
    public static TargetFramework NET40 = (TargetFramework) "NET40";
    public static TargetFramework NET35 = (TargetFramework) "NET35";
    public static TargetFramework NET20 = (TargetFramework) "NET20";
    public static TargetFramework NETSTANDARD = (TargetFramework) "NETSTANDARD";
    public static TargetFramework NETSTANDARD2_1 = (TargetFramework) "NETSTANDARD2_1";
    public static TargetFramework NETSTANDARD2_0 = (TargetFramework) "NETSTANDARD2_0";
    public static TargetFramework NETSTANDARD1_6 = (TargetFramework) "NETSTANDARD1_6";
    public static TargetFramework NETSTANDARD1_5 = (TargetFramework) "NETSTANDARD1_5";
    public static TargetFramework NETSTANDARD1_4 = (TargetFramework) "NETSTANDARD1_4";
    public static TargetFramework NETSTANDARD1_3 = (TargetFramework) "NETSTANDARD1_3";
    public static TargetFramework NETSTANDARD1_2 = (TargetFramework) "NETSTANDARD1_2";
    public static TargetFramework NETSTANDARD1_1 = (TargetFramework) "NETSTANDARD1_1";
    public static TargetFramework NETSTANDARD1_0 = (TargetFramework) "NETSTANDARD1_0";
    public static TargetFramework NET = (TargetFramework) "NET";
    public static TargetFramework NET10_0 = (TargetFramework) "NET10_0";
    public static TargetFramework NET9_0 = (TargetFramework) "NET9_0";
    public static TargetFramework NET8_0 = (TargetFramework) "NET8_0";
    public static TargetFramework NET7_0 = (TargetFramework) "NET7_0";
    public static TargetFramework NET6_0 = (TargetFramework) "NET6_0";
    public static TargetFramework NET5_0 = (TargetFramework) "NET5_0";
    public static TargetFramework NETCOREAPP = (TargetFramework) "NETCOREAPP";
    public static TargetFramework NETCOREAPP3_1 = (TargetFramework) "NETCOREAPP3_1";
    public static TargetFramework NETCOREAPP3_0 = (TargetFramework) "NETCOREAPP3_0";
    public static TargetFramework NETCOREAPP2_2 = (TargetFramework) "NETCOREAPP2_2";
    public static TargetFramework NETCOREAPP2_1 = (TargetFramework) "NETCOREAPP2_1";
    public static TargetFramework NETCOREAPP2_0 = (TargetFramework) "NETCOREAPP2_0";
    public static TargetFramework NETCOREAPP1_1 = (TargetFramework) "NETCOREAPP1_1";
    public static TargetFramework NETCOREAPP1_0 = (TargetFramework) "NETCOREAPP1_0";
    public static implicit operator TargetFramework(string value)
    {
        return new TargetFramework { Value = value };
    }
}
#endregion
#region InstallFrom
/// <summary>Used within <see cref="MSBuildTasks"/>.</summary>
[Serializable]
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(TypeConverter<InstallFrom>))]
public partial class InstallFrom : Enumeration
{
    public static InstallFrom Web = (InstallFrom) "Web";
    public static InstallFrom Unc = (InstallFrom) "Unc";
    public static InstallFrom Disk = (InstallFrom) "Disk";
    public static implicit operator InstallFrom(string value)
    {
        return new InstallFrom { Value = value };
    }
}
#endregion
