using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyModel;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Fallout.Common.ValueInjection;
using Serilog;
#pragma warning disable CA2255

namespace Fallout.Common.Execution;

internal static class BuildManager
{
    private const int ErrorExitCode = -1;

    private static readonly LinkedList<Action> cancellationHandlers = new();

    public static event Action CancellationHandler
    {
        add => cancellationHandlers.AddFirst(value);
        remove => cancellationHandlers.Remove(value);
    }

    [ModuleInitializer]
    public static void Initialize()
    {
        DependencyContext.Default?.GetRuntimeAssemblyNames(string.Empty)
            .Where(x => x.FullName.StartsWith("Fallout."))
            .ForEach(x => AppDomain.CurrentDomain.Load(x));
    }

    public static int Execute<T>(Expression<Func<T, Target>>[] defaultTargetExpressions)
        where T : FalloutBuild, new()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var build = new T();

        // Hold the per-run global subscriptions in locals so the finally can undo exactly them —
        // otherwise each Execute in the same process (tests, hosted scenarios) accumulates handlers.
        // FT-1 / #306.
        ConsoleCancelEventHandler onCancelKeyPress = (_, _) => cancellationHandlers.ForEach(x => x());
        EventHandler onToolOptionsCreated = (options, _) => VerbosityMapping.Apply((ToolOptions)options);
        Console.CancelKeyPress += onCancelKeyPress;
        ToolOptions.Created += onToolOptionsCreated;

        try
        {
            Logging.Configure(build);

            build.ExecutableTargets = ExecutableTargetFactory.CreateAll(build, defaultTargetExpressions);
            build.ExecuteExtension<IOnBuildCreated>(x => x.OnBuildCreated(build.ExecutableTargets));

            NuGetToolPathResolver.EmbeddedPackagesDirectory = build.EmbeddedPackagesDirectory;
            NuGetToolPathResolver.NuGetPackagesConfigFile = build.NuGetPackagesConfigFile;
            NuGetToolPathResolver.NuGetAssetsConfigFile = build.NuGetAssetsConfigFile;
            NpmToolPathResolver.NpmPackageJsonFile = build.NpmPackageJsonFile;

            if (!build.NoLogo)
                build.WriteLogo();

            // TODO: move InvokedTargets to ExecutableTargetFactory
            build.ExecutionPlan = ExecutionPlanner.GetExecutionPlan(
                build.ExecutableTargets,
                ParameterService.GetParameter<string[]>(() => build.InvokedTargets));

            ToolRequirementService.EnsureToolRequirements(build, build.ExecutionPlan);
            build.ExecuteExtension<IOnBuildInitialized>(x => x.OnBuildInitialized(build.ExecutableTargets, build.ExecutionPlan));

            CancellationHandler += Finish;
            BuildExecutor.Execute(
                build,
                ParameterService.GetParameter<string[]>(() => build.SkippedTargets));

            return build.ExitCode ??= build.IsSucceeding ? 0 : ErrorExitCode;
        }
        catch (Exception exception)
        {
            exception = exception.Unwrap();
            if (exception is not TargetExecutionException)
            {
                Log.Verbose(exception, "Target-unrelated exception was thrown");
                Host.Error(exception.Message);
            }

            return build.ExitCode ??= ErrorExitCode;
        }
        finally
        {
            Finish();
            Log.CloseAndFlush();

            // FT-1 (#306): undo this run's process-global state so a subsequent Execute in the same
            // process starts clean — no accumulated handlers, no carried-over log events / caches / config.
            CancellationHandler -= Finish;
            Console.CancelKeyPress -= onCancelKeyPress;
            ToolOptions.Created -= onToolOptionsCreated;
            Logging.InMemorySink.Instance.Clear();
            ValueInjectionUtility.ClearCache();
            NuGetToolPathResolver.Reset();
            NpmToolPathResolver.Reset();
        }

        void Finish()
        {
            if (build.ExecutionPlan == null)
                return;

            foreach (var target in build.ExecutionPlan)
            {
                target.Stopwatch.Stop();
                target.Status = target.Status switch
                {
                    ExecutionStatus.Running => ExecutionStatus.Aborted,
                    ExecutionStatus.Scheduled => ExecutionStatus.NotRun,
                    _ => target.Status
                };
            }

            build.WriteErrorsAndWarnings();
            build.WriteTargetOutcome();
            build.WriteBuildOutcome();
            build.ExecuteExtension<IOnBuildFinished>(x => x.OnBuildFinished());
        }
    }
}
