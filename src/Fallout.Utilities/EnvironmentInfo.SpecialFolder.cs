using System;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Common;

public enum SpecialFolders
{
    ProgramFiles = Environment.SpecialFolder.ProgramFiles,
    ProgramFilesX86 = Environment.SpecialFolder.ProgramFilesX86,
    LocalApplicationData = Environment.SpecialFolder.LocalApplicationData,
    ApplicationData = Environment.SpecialFolder.ApplicationData,
    CommonApplicationData = Environment.SpecialFolder.CommonApplicationData,
    Windows = Environment.SpecialFolder.Windows,
    System = Environment.SpecialFolder.System,
    UserProfile = Environment.SpecialFolder.UserProfile
}

partial class EnvironmentInfo
{
    public static AbsolutePath SpecialFolder(SpecialFolders folder)
    {
        var path = Environment.GetFolderPath((Environment.SpecialFolder)folder);

        // https://github.com/nuke-build/nuke/pull/825#discussion_r848954724
        if (path.IsNullOrEmpty() && folder == SpecialFolders.UserProfile)
        {
            path = Environment.GetEnvironmentVariable("USERPROFILE");
        }

        return path;
    }
}
