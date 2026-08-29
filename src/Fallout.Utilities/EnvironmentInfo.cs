using System;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Common;

/// <summary>
/// Provides access to environment relevant information.
/// </summary>
public static partial class EnvironmentInfo
{
    public static string NewLine => Environment.NewLine;

    public static string MachineName => Environment.MachineName;

    /// <summary>
    /// Returns the working directory for the current process.
    /// </summary>
    public static AbsolutePath WorkingDirectory
    {
        get => Environment.CurrentDirectory;
        set => Environment.CurrentDirectory = value;
    }

    /// <summary>
    /// Switches to a new working directory. The previous working directory is restored once the <see cref="IDisposable"/> is disposed.
    /// </summary>
    public static IDisposable SwitchWorkingDirectory(this AbsolutePath path)
    {
        Assert.DirectoryExists(path);
        return DelegateDisposable.SetAndRestore(() => WorkingDirectory, path);
    }
}
