using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Fallout.Common.CI;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using static Fallout.Constants;
using static Fallout.Common.FalloutPaths;

namespace Fallout.Common;

public abstract partial class FalloutBuild
{
    static FalloutBuild()
    {
        RootDirectory = GetRootDirectory();
        TemporaryDirectory = GetTemporaryDirectory(RootDirectory).CreateDirectory();

        BuildAssemblyFile = GetBuildAssemblyFile();
        BuildAssemblyDirectory = BuildAssemblyFile?.Parent;

        BuildProjectFile = GetBuildProjectFile(BuildAssemblyDirectory);
        BuildProjectDirectory = BuildProjectFile?.Parent;

        Verbosity = ParameterService.GetParameter<Verbosity?>(() => Verbosity) ?? Verbosity.Normal;
        Host = ParameterService.GetParameter(() => Host) ?? Host.Default;
        LoadedLocalProfiles = ParameterService.GetParameter(() => LoadedLocalProfiles) ?? new string[0];
    }

    /// <summary>
    /// Gets the full path to the root directory.
    /// </summary>
    [Parameter("Root directory during build execution.", Name = RootDirectoryParameterName)]
    public static AbsolutePath RootDirectory { get; }

    /// <summary>
    /// Gets the full path to the temporary directory <c>/.fallout/temp</c>.
    /// </summary>
    public static AbsolutePath TemporaryDirectory { get; }

    /// <summary>
    /// Gets the full path to the build assembly file.
    /// </summary>
    public static AbsolutePath BuildAssemblyFile { get; }

    /// <summary>
    /// Gets the full path to the build assembly directory.
    /// </summary>
    public static AbsolutePath BuildAssemblyDirectory { get; }

    /// <summary>
    /// Gets the full path to the build project directory, or <c>null</c>
    /// </summary>
    public static AbsolutePath BuildProjectDirectory { get; }

    /// <summary>
    /// Gets the full path to the build project file, or <c>null</c>
    /// </summary>
    public static AbsolutePath BuildProjectFile { get; }

    /// <summary>
    /// Gets the logging verbosity during build execution. Default is <see cref="Fallout.Common.Verbosity.Normal"/>.
    /// </summary>
    [Parameter("Logging verbosity during build execution. Default is 'Normal'.")]
    public static Verbosity Verbosity
    {
        get => Logging.Level.ToVerbosity();
        set => Logging.Level = value.ToLogLevel();
    }

    /// <summary>
    /// Gets the host for execution. Default is <em>automatic</em>.
    /// </summary>
    [Parameter("Host for execution. Default is 'automatic'.")]
    public static Host Host { get; set; }

    [Parameter("Defines the profiles to load.", Name = LoadedLocalProfilesParameterName)]
    public static string[] LoadedLocalProfiles { get; }

    public static bool IsLocalBuild => !IsServerBuild;
    public static bool IsServerBuild => Host is IBuildServer;

    private static AbsolutePath GetRootDirectory()
    {
        var parameterValue = ParameterService.GetParameter(() => RootDirectory);
        if (parameterValue != null)
            return parameterValue;

        if (ParameterService.GetParameter<bool>(() => RootDirectory))
            return EnvironmentInfo.WorkingDirectory;

        return TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory)
            .NotNull(new[]
                     {
                         $"Could not locate '{FalloutDirectoryName}' directory/file while walking up from '{EnvironmentInfo.WorkingDirectory}'.",
                         "Either create a directory/file to mark the root directory, or add '--root [path]' to the invocation."
                     }.JoinNewLine());
    }

    private static AbsolutePath GetBuildAssemblyFile()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null || entryAssembly.GetTypes().All(x => !x.IsSubclassOf(typeof(FalloutBuild))))
        {
            var assemblyName = entryAssembly?.GetName().Name;
            Assert.True(assemblyName == null ||
                        assemblyName.StartsWith("ReSharperTestRunner") ||
                        assemblyName == "testhost",
                $"Assembly name was {assemblyName.SingleQuote()}");
            return null;
        }

        var assemblyLocation = entryAssembly.Location;
        var invokedLocation = Environment.GetCommandLineArgs().First();
        Assert.True(assemblyLocation == string.Empty || assemblyLocation == invokedLocation);

        return assemblyLocation != string.Empty ? assemblyLocation : invokedLocation;
    }

    private static AbsolutePath GetBuildProjectFile(AbsolutePath buildAssemblyDirectory)
    {
        if (buildAssemblyDirectory == null)
            return null;

        return new DirectoryInfo(buildAssemblyDirectory)
            .DescendantsAndSelf(x => x.Parent)
            .Select(x => x.GetFiles("*.csproj", SearchOption.TopDirectoryOnly)
                .SingleOrDefaultOrError($"Found multiple project files in '{x}'."))
            .FirstOrDefault(x => x != null)
            ?.FullName;
    }
}
