using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fallout.Cli.Commands;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Fallout.Cli;

public partial class Program
{
    internal static string CurrentBuildScriptName => EnvironmentInfo.IsWin ? "build.ps1" : "build.sh";

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var rootDirectory = TryGetRootDirectory();

            var buildScript = rootDirectory != null
                ? rootDirectory.GetFiles(CurrentBuildScriptName, depth: 2)
                    .FirstOrDefault(x => Constants.TryGetRootDirectoryFrom(x.Parent) == rootDirectory)
                : null;

            using var services = BuildServiceProvider();
            return await services.GetRequiredService<CommandDispatcher>().DispatchAsync(args, rootDirectory, buildScript);
        }
        catch (Exception exception)
        {
            Host.Error(exception.Unwrap().Message);
            return 1;
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConsolePrompts, SpectreConsolePrompts>();
        services.AddSingleton<CommandDispatcher>();
        RegisterCommands(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterCommands(IServiceCollection services)
    {
        // Real command types — issue #392 converts one legacy handler per PR.
        services.AddSingleton<IFalloutCommand, RunCommand>();
        services.AddSingleton<IFalloutCommand, TriggerCommand>();

        // Legacy handlers still living on Program, adapted until they are extracted into command
        // types. Each conversion deletes one line here plus its Program.X.cs partial.
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("setup", Setup));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("update", Update));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("add-package", AddPackage));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("cake-convert", CakeConvert));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("cake-clean", CakeClean));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("complete", Complete));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("get-configuration", GetConfiguration));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("secrets", Secrets));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("GetNextDirectory", (_, _, _) => GetNextDirectory()));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("PopDirectory", (_, _, _) => PopDirectory()));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("PushWithCurrentRootDirectory", (_, rootDirectory, _) => PushWithCurrentRootDirectory(rootDirectory)));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("PushWithParentRootDirectory", (_, rootDirectory, _) => PushWithParentRootDirectory(rootDirectory)));
        services.AddSingleton<IFalloutCommand>(new DelegateCommand("PushWithChosenRootDirectory", (_, _, _) => PushWithChosenRootDirectory()));
    }

    internal static void PrintInfo()
    {
        Host.Information($"Fallout Global Tool 🌐 {typeof(Program).Assembly.GetInformationalText()}");
    }

    private static AbsolutePath TryGetRootDirectory()
    {
        // TODO: copied in FalloutBuild.GetRootDirectory
        AbsolutePath parameterValue = EnvironmentInfo.GetNamedArgument<AbsolutePath>(Constants.RootDirectoryParameterName);
        if (parameterValue != null)
            return parameterValue;

        if (EnvironmentInfo.GetNamedArgument<bool>(Constants.RootDirectoryParameterName))
            return EnvironmentInfo.WorkingDirectory;

        return Constants.TryGetRootDirectoryFrom(Directory.GetCurrentDirectory());
    }

    // ── Transitional Spectre prompt delegators ──────────────────────────────────────────────────
    // The implementations now live in SpectreConsolePrompts; these forwards keep the not-yet-extracted
    // Program.X.cs command handlers compiling. As each handler becomes a command type taking
    // IConsolePrompts via the constructor, its use of these disappears; the last conversion deletes them.
    private static readonly IConsolePrompts s_prompts = new SpectreConsolePrompts();

    private static void ShowInput(string emoji, string title, string value) => s_prompts.ShowInput(emoji, title, value);
    private static void ShowCompletion(string title) => s_prompts.ShowCompletion(title);
    private static void ClearPreviousLine() => s_prompts.ClearPreviousLine();
    private static bool PromptForConfirmation(string question) => s_prompts.PromptForConfirmation(question);
    private static string PromptForInput(string question, string defaultValue = null) => s_prompts.PromptForInput(question, defaultValue);
    private static string PromptForSecret(string title, int? minLength = null) => s_prompts.PromptForSecret(title, minLength);
    private static T PromptForChoice<T>(string question, params (T Value, string Description)[] choices) => s_prompts.PromptForChoice(question, choices);
    private static void ConfirmExecution(string title, Action action) => s_prompts.ConfirmExecution(title, action);
}
