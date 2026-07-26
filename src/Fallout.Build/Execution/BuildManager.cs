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
using Serilog;
#pragma warning disable CA2255

namespace Fallout.Common.Execution;

internal static class BuildManager
{
    private const int ErrorExitCode = -1;

    // Facade over the active BuildContext (FT-2): callers register/unregister during a run; the
    // context owns the handler list and discards it on dispose.
    public static event Action CancellationHandler
    {
        add => BuildContext.Current?.RegisterCancellationHandler(value);
        remove => BuildContext.Current?.UnregisterCancellationHandler(value);
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

        // The per-run scope owns the global subscriptions + teardown (FT-2 / #307). Disposed at
        // method exit, so re-invocation in the same process starts clean even if `new T()` throws.
        using var context = BuildContext.Activate();
        var build = new T();

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
            // Per-run teardown (handler unsubscription + state reset) is owned by the BuildContext,
            // run when `context` is disposed at method exit.
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
