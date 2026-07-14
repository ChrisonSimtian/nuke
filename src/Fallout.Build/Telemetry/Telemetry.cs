using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Utilities;
using static Fallout.Common.ControlFlow;

namespace Fallout.Common.Execution;

internal static partial class Telemetry
{
    // https://docs.microsoft.com/en-us/dotnet/core/tools/telemetry
    // https://dotnet.microsoft.com/platform/telemetry
    // https://docs.microsoft.com/en-us/azure/azure-monitor/app/console
    // https://docs.microsoft.com/en-us/azure/azure-monitor/app/ip-collection

    public const string OptOutEnvironmentKey = "FALLOUT_TELEMETRY_OPTOUT";
    public const string LegacyOptOutEnvironmentKey = "NUKE_TELEMETRY_OPTOUT";
    public const int CurrentVersion = 1;

    // Telemetry is currently a no-op for Fallout. The original NUKE maintainer owned an
    // Azure Application Insights endpoint we can't reuse; Microsoft.ApplicationInsights was
    // dropped from our deps in #79. The awareness/disclosure machinery below stays so we can
    // wire up a Fallout-controlled backend later without re-introducing the consent flow.
    private const string VersionPropertyName = "FalloutTelemetryVersion";
    private const string LegacyVersionPropertyName = "NukeTelemetryVersion";

    static Telemetry()
    {
        var optoutParameter = ParameterService.GetParameter<string>(OptOutEnvironmentKey)
                              ?? ParameterService.GetParameter<string>(LegacyOptOutEnvironmentKey)
                              ?? string.Empty;
        if (optoutParameter == "1" || optoutParameter.EqualsOrdinalIgnoreCase(bool.TrueString))
            return;

        // No telemetry endpoint configured for Fallout yet — short-circuit. When a backend lands,
        // re-enable by calling ProjectModelTasks.Initialize() + CheckAwareness() here and wiring
        // the resulting consent through to TrackEvent (in Telemetry.Events.cs).
    }

    private static int? CheckAwareness()
    {
        AbsolutePath GetCookieFile(string name, int version)
            => Constants.GlobalFalloutDirectory / "telemetry-awareness" / $"v{version}" / name;

        // Check for calls from Fallout.Cli and custom global tools
        if (SuppressErrors(() => FalloutBuild.BuildProjectFile, logWarning: false) == null)
        {
            var cookieName = Assembly.GetEntryAssembly().NotNull().GetName().Name;
            var cookieFile = GetCookieFile(cookieName, CurrentVersion);
            if (!cookieFile.Exists())
            {
                PrintDisclosure($"create awareness cookie for {cookieName.SingleQuote()}");
                cookieFile.TouchFile();
            }

            return CurrentVersion;
        }

        var project = ProjectModelTasks.ParseProject(FalloutBuild.BuildProjectFile);
        var property = project.Properties.SingleOrDefault(x => x.Name.EqualsOrdinalIgnoreCase(VersionPropertyName))
                       ?? project.Properties.SingleOrDefault(x => x.Name.EqualsOrdinalIgnoreCase(LegacyVersionPropertyName));
        if (property?.EvaluatedValue != CurrentVersion.ToString())
        {
            if (FalloutBuild.IsServerBuild)
            {
                PrintDisclosure(action: null);
                return null;
            }

            PrintDisclosure($"set the {VersionPropertyName.SingleQuote()} property");
            project.SetProperty(VersionPropertyName, CurrentVersion.ToString());
            project.Save();
        }

        for (var version = CurrentVersion; version > 0; version--)
        {
            var cookieFile = GetCookieFile("Fallout.Cli", version);
            if (cookieFile.FileExists())
                return version;
        }

        return FalloutBuild.IsServerBuild ? CurrentVersion : null;
    }

    private static void PrintDisclosure(string action)
    {
        var disclosure =
            new[]
            {
                $"Telemetry v{CurrentVersion}",
                "------------",
                "Fallout collects anonymous usage data in order to help us improve your experience.",
                $"Read more about scope, data points, and opt-out: {Constants.FalloutTelemetryDocsUrl}",
                string.Empty
            }.JoinNewLine();

        if (action != null &&
            Environment.UserInteractive &&
            !Console.IsInputRedirected)
        {
            Host.Information(disclosure);
            Thread.Sleep(2000);
            Host.Information($"Press <Enter> to {action} ...");
            WaitForEnter();
        }
        else
        {
            Host.Warning(disclosure);
            Host.Warning("Run in interactive console to fix this warning");
        }
    }

    private static void WaitForEnter()
    {
        ConsoleKey response;
        do
        {
            response = Console.ReadKey(intercept: true).Key;
        } while (response != ConsoleKey.Enter);
    }
}
