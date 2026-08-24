using System;
using System.Linq;
using Fallout.Common.Tooling;

namespace Fallout.Common.Utilities;

public static class CredentialStore
{
    public static void DeletePassword(string name)
    {
        switch (EnvironmentInfo.Platform)
        {
            case PlatformFamily.OSX:
                ProcessTasks.StartProcess(
                    Security,
                    $"delete-generic-password -a {EnvironmentInfo.Variables["LOGNAME"]} -s {name.DoubleQuoteIfNeeded()}",
                    logInvocation: false,
                    logOutput: false).AssertZeroExitCode();
                break;
            default:
                throw new NotSupportedException(EnvironmentInfo.Platform.ToString());
        }
    }

    public static void SavePassword(string name, string password)
    {
        switch (EnvironmentInfo.Platform)
        {
            case PlatformFamily.OSX:
                ProcessTasks.StartProcess(
                    Security,
                    $"add-generic-password -T \"\" -a {EnvironmentInfo.Variables["LOGNAME"]} -s {name.DoubleQuoteIfNeeded()} -w {password}",
                    logInvocation: false,
                    logOutput: false).AssertZeroExitCode();
                break;
            default:
                throw new NotSupportedException(EnvironmentInfo.Platform.ToString());
        }
    }

    public static string TryGetPassword(string name)
    {
        switch (EnvironmentInfo.Platform)
        {
            case PlatformFamily.OSX:
                var process = ProcessTasks.StartProcess(
                    Security,
                    $"find-generic-password -w -a {EnvironmentInfo.Variables["LOGNAME"]} -s {name.DoubleQuoteIfNeeded()}",
                    logInvocation: false,
                    logOutput: false);
                process.WaitForExit();
                return process.ExitCode == 0
                    ? process.Output.Single().Text
                    : null;
            default:
                return null;
        }
    }

    private static string Security => ToolPathResolver.GetPathExecutable("security");

    public static string GetPassword(string profile, string rootDirectory)
    {
        string PromptForPassword()
        {
            Host.Information($"Enter password for {FalloutPaths.GetParametersFileName(profile)}:");
            return ConsoleUtility.ReadSecret();
        }

        var credentialStoreName = FalloutPaths.GetCredentialStoreName(rootDirectory, profile);
        var legacyCredentialStoreName = FalloutPaths.GetLegacyCredentialStoreName(rootDirectory, profile);
        var passwordParameterName = FalloutPaths.GetProfilePasswordParameterName(profile);

        return TryGetPassword(credentialStoreName) ??
               TryGetLegacyPasswordWithWarning(legacyCredentialStoreName, credentialStoreName) ??
               ParameterService.GetParameter<string>(passwordParameterName) ??
               PromptForPassword();
    }

    private static string TryGetLegacyPasswordWithWarning(string legacyName, string newName)
    {
        var password = TryGetPassword(legacyName);
        if (password == null)
            return null;

        Console.Error.WriteLine(
            $"warning FALLOUT003: Found credentials under legacy keychain entry '{legacyName}'. " +
            $"Falling back to legacy entry for this run. Re-run `dotnet fallout :secrets` to migrate to '{newName}'. " +
            "The legacy entry will no longer be read in 11.0.");
        return password;
    }

    public static string CreateNewPassword(out bool generated)
    {
        while (true)
        {
            Host.Information(
                EnvironmentInfo.IsOsx
                    ? "Enter a minimum 10 character password (leave empty for auto-generated stored in macOS keychain):"
                    : "Enter a minimum 10 character password:");

            var password = ConsoleUtility.ReadSecret();
            if (password.IsNullOrEmpty() && EnvironmentInfo.IsOsx)
            {
                generated = true;
                return EncryptionUtility.GetGeneratedPassword();
            }

            if (!password.IsNullOrEmpty() && password.Length >= 10)
            {
                generated = false;
                return password;
            }
        }
    }
}
