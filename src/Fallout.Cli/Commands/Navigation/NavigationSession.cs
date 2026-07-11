using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;

namespace Fallout.Cli.Commands.Navigation;

/// <summary>
/// Shared per-terminal-session state for the directory-navigation commands. The companion shell
/// functions invoke the commands by name (preserved on conversion):
/// <code>
/// function fallout- { fallout :PopDirectory; cd $(fallout :GetNextDirectory) }
/// function fallout/ { fallout :PushWithChosenRootDirectory; cd $(fallout :GetNextDirectory) }
/// function fallout. { fallout :PushWithCurrentRootDirectory; cd $(fallout :GetNextDirectory) }
/// function fallout.. { fallout :PushWithParentRootDirectory; cd $(fallout :GetNextDirectory) }
/// </code>
/// </summary>
internal static class NavigationSession
{
    public static string SessionId
        => EnvironmentInfo.Platform switch
        {
            PlatformFamily.OSX => EnvironmentInfo.GetVariable("TERM_SESSION_ID").NotNull()[7..],
            PlatformFamily.Windows => EnvironmentInfo.GetVariable("WT_SESSION").NotNull(),
            _ => throw new NotSupportedException($"{EnvironmentInfo.Platform} has no session id selector.")
        };

    public static AbsolutePath SessionFile => GlobalTemporaryDirectory / $"fallout-{SessionId}.dat";

    public static int PushAndSetNext(Func<string> directoryProvider)
    {
        try
        {
            var content = SessionFile.Existing()?.ReadAllLines().ToList() ?? new List<string> { null };
            content[0] = directoryProvider.Invoke();
            content.Insert(index: 1, EnvironmentInfo.WorkingDirectory);
            SessionFile.WriteAllLines(content);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
