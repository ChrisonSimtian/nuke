using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.CI;
using Fallout.Common.Execution.Theming;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

// Both Serilog and Microsoft.Extensions.Logging declare an ILogger. This file is the seam between
// them, so the unqualified name is bound to the abstraction the framework codes against; Serilog's
// own pipeline is reached through the static Log class below.
using ILogger = Microsoft.Extensions.Logging.ILogger;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Fallout.Common.Execution;

public static class Logging
{
    public static readonly LoggingLevelSwitch LevelSwitch = new();

    /// <summary>Category for framework log records written without a category of their own.</summary>
    internal const string DefaultCategoryName = "Fallout";

    private static ILoggerFactory loggerFactory;

    internal static bool SupportsAnsiOutput => Environment.GetEnvironmentVariable("TERM") is { } term && term.StartsWithOrdinalIgnoreCase("xterm");
    internal static IHostTheme DefaultTheme { get; } = SupportsAnsiOutput
        ? AnsiConsoleHostTheme.Default256AnsiColorTheme
        : SystemConsoleHostTheme.DefaultSystemColorTheme;

    internal static string ErrorsAndWarningsOutputTemplate => "[{Level:u3}] {ExecutingTarget}: {Message:l}{NewLine}";
    internal static string StandardOutputTemplate => "[{Level:u3}] {Message:l}{NewLine}{Exception}";
    internal static string TimestampOutputTemplate => $"{{Timestamp:HH:mm:ss}} {StandardOutputTemplate}";

    private const int TargetNameLength = 20;

    public static LogLevel Level
    {
        get => LevelSwitch.MinimumLevel.ToLogLevel();
        set => LevelSwitch.MinimumLevel = value.ToLogEventLevel();
    }

    /// <summary>
    /// Logger factory for the current build run, backed by the Serilog pipeline that
    /// <see cref="Configure"/> installs. <c>BuildManager</c> feeds this from its composition root
    /// (see <c>AddFalloutLogging</c>). Outside a run there is no container — the CLI commands call
    /// <see cref="Configure"/> directly — so this falls back to a factory over the ambient Serilog
    /// pipeline, and the seam is usable either way.
    /// </summary>
    internal static ILoggerFactory Factory => loggerFactory ??= CreateSerilogLoggerFactory();

    /// <summary>
    /// Logger for framework code that has no category of its own. Deliberately not cached — each
    /// access creates a logger against the pipeline that is current right now, which is what keeps
    /// the façade correct across the reassignments described on
    /// <see cref="CreateSerilogLoggerFactory"/>.
    /// </summary>
    internal static ILogger Logger => Factory.CreateLogger(DefaultCategoryName);

    /// <summary>
    /// Points <see cref="Factory"/> at <paramref name="factory"/> until the returned bracket is
    /// disposed. Ownership stays with the caller: disposing the bracket restores the previous
    /// factory, it does not dispose <paramref name="factory"/>.
    /// </summary>
    internal static IDisposable UseLoggerFactory(ILoggerFactory factory)
    {
        return DelegateDisposable.SetAndRestore(() => loggerFactory, factory.NotNull());
    }

    /// <summary>
    /// Bridges <see cref="ILogger"/> onto Serilog.
    /// </summary>
    /// <remarks>
    /// Passing no logger leaves the factory itself unbound, so each logger it hands out reads the
    /// ambient <see cref="Log.Logger"/> as it is created. That matters because the pipeline is not
    /// stable for the lifetime of the process: <see cref="Configure"/> installs it late and
    /// replaces it on re-entry, and <c>Host.WriteErrorsAndWarnings</c> swaps it again to render the
    /// end-of-build summary. Pinning a logger into the factory would strand every consumer on
    /// whichever pipeline happened to exist first.
    ///
    /// Binding still happens per logger rather than per write, because the category name is
    /// attached as Serilog's <c>SourceContext</c> at construction. Two consequences the callers
    /// depend on: <c>AddFalloutLogging</c> configures the pipeline <em>before</em> it registers the
    /// factory, so a container-resolved logger can never bind a stale one; and <see cref="Logger"/>
    /// is not cached.
    ///
    /// Serilog owns the pipeline's lifetime (<c>Log.CloseAndFlush</c>), hence <c>dispose: false</c>.
    /// </remarks>
    internal static ILoggerFactory CreateSerilogLoggerFactory()
    {
        return new SerilogLoggerFactory(logger: null, dispose: false);
    }

    public static void Configure(IFalloutBuild build = null)
    {
        if (build != null)
        {
            if (build.IsInterceptorExecution)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console(new CompactJsonFormatter())
                    .CreateLogger();
                return;
            }

            DeleteOldLogFiles(build);
        }

        Log.Logger = new LoggerConfiguration()
            .ConfigureEnricher()
            .ConfigureHost(build)
            .ConfigureConsole(build)
            .ConfigureInMemory(build)
            .ConfigureFiles(build)
            .ConfigureLevel()
            .ConfigureFilter(build)
            .CreateLogger();
    }

    public static LoggerConfiguration ConfigureEnricher(this LoggerConfiguration configuration)
    {
        return configuration.Enrich.With<ExecutingTargetLogEventEnricher>();
    }

    public static LoggerConfiguration ConfigureLevel(this LoggerConfiguration configuration)
    {
        return configuration.MinimumLevel.Verbose();
    }

    public static LoggerConfiguration ConfigureFilter(this LoggerConfiguration configuration, IFalloutBuild build)
    {
        if (build == null)
            return configuration;

        return configuration.Filter.ByExcluding(x => build.Host.FilterMessage(x.MessageTemplate.Text));
    }

    public static LoggerConfiguration ConfigureConsole(this LoggerConfiguration configuration, IFalloutBuild build)
    {
        return configuration
            .WriteTo.Console(outputTemplate: build != null && build.IsOutputEnabled(DefaultOutput.Timestamps)
                    ? build.Host.OutputTemplate
                    : StandardOutputTemplate,
                theme: (ConsoleTheme)(build != null ? build.Host.Theme : DefaultTheme),
                applyThemeToRedirectedOutput: true,
                levelSwitch: LevelSwitch);
    }

    public static LoggerConfiguration ConfigureHost(this LoggerConfiguration configuration, IFalloutBuild build)
    {
        if (build == null)
            return configuration;

        return configuration
            .WriteTo.Sink(new Host.LogEventSink(build.Host), restrictedToMinimumLevel: LogEventLevel.Warning);
    }

    public static LoggerConfiguration ConfigureInMemory(this LoggerConfiguration configuration, IFalloutBuild build)
    {
        if (build == null)
            return configuration;

        return configuration
            .WriteTo.Sink(InMemorySink.Instance, LogEventLevel.Warning);
    }

    public static LoggerConfiguration ConfigureFiles(this LoggerConfiguration configuration, IFalloutBuild build)
    {
        if (build == null || build.Host is IBuildServer)
            return configuration;

        var buildLogFile = build.TemporaryDirectory / "build.log";
        return configuration
            .WriteTo.File(
                path: buildLogFile,
                outputTemplate: $"{{Timestamp:HH:mm:ss.fff}} | {{Level:u1}} | {{ExecutingTarget,-{TargetNameLength}}} | {{Message:l}}{{NewLine}}{{Exception}}")
            .WriteTo.File(
                path: Path.ChangeExtension(buildLogFile, $".{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log"),
                outputTemplate: $"{{Level:u1}} | {{ExecutingTarget,-{TargetNameLength}}} | {{Message:l}}{{NewLine}}{{Exception}}");
    }

    private static void DeleteOldLogFiles(IFalloutBuild build)
    {
        if (BuildServerConfigurationGeneration.IsActive)
            return;

        build.TemporaryDirectory.GlobFiles("build.*.log").OrderByDescending(x => x.ToString()).Skip(5)
            .ForEach(x => x.DeleteFile());

        var buildLogFile = build.TemporaryDirectory / "build.log";
        if (buildLogFile.Exists())
        {
            using var filestream = File.OpenWrite(buildLogFile);
            filestream.SetLength(0);
        }
    }

    internal static void Test()
    {
        const string Esc = "\u001b[";
        const string Reset = "\u001b[0m";

        for (var i = 30; i < 47; i++)
            Console.Write($"{Esc}{i}m{i}  {Reset} ");
        Console.WriteLine();
        for (var i = 30; i < 47; i++)
            Console.Write($"{Esc}{i};1m{i};1{Reset} ");
        Console.WriteLine();
        for (var i = 30; i < 47; i++)
            Console.Write($"{Esc}{i};2m{i};2{Reset} ");
        Console.WriteLine();
        for (var i = 30; i < 47; i++)
            Console.Write($"{Esc}{i};3m{i};2{Reset} ");
        Console.WriteLine();

        for (var i = 90; i < 107; i++)
            Console.Write($"{Esc}{i}m{i}  {Reset} ");
        Console.WriteLine();
        for (var i = 90; i < 107; i++)
            Console.Write($"{Esc}{i};1m{i};1{Reset} ");
        Console.WriteLine();
        for (var i = 90; i < 107; i++)
            Console.Write($"{Esc}{i};2m{i};2{Reset} ");
        Console.WriteLine();
        for (var i = 90; i < 107; i++)
            Console.Write($"{Esc}{i};3m{i};2{Reset} ");
        Console.WriteLine();

        for (var i = 0; i < 255; i++)
        {
            var code = i.ToString().PadLeft(3, '0');
            Console.Write($"{Esc}38;5;{code}m{code}{Reset} ");
            if ((i + 1) % 16 == 0)
                Console.WriteLine();
        }

        Console.WriteLine();

        for (var i = 0; i <= 255; i++)
        {
            var code = i.ToString().PadLeft(3, '0');
            Console.Write($"{Esc}38;5;{code};1m{code}{Reset} ");
            if ((i + 1) % 16 == 0)
                Console.WriteLine();
        }
    }

    public static IDisposable SetTarget(string name)
    {
        return ExecutingTargetLogEventEnricher.SetTargetEventProperty(name);
    }

    public class InMemorySink : ILogEventSink, IDisposable
    {
        public static InMemorySink Instance { get; } = new();

        private readonly List<LogEvent> logEvents;

        private InMemorySink()
        {
            logEvents = new List<LogEvent>();
        }

        public IReadOnlyCollection<LogEvent> LogEvents => logEvents.AsReadOnly();

        public void Emit(LogEvent logEvent)
        {
            logEvent.AddOrUpdateProperty(ExecutingTargetLogEventEnricher.Current);
            logEvents.Add(logEvent);
        }

        /// <summary>Drops accumulated events so a subsequent build in the same process starts clean. FT-1 / #306.</summary>
        public void Clear()
        {
            logEvents.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }

    internal class ExecutingTargetLogEventEnricher : ILogEventEnricher
    {
        public static LogEventProperty Current => property ?? defaultProperty;

        private static readonly LogEventProperty defaultProperty = GetTargetEventProperty(string.Empty);
#pragma warning disable CS0649
        private static LogEventProperty property;
#pragma warning restore CS0649

        public static IDisposable SetTargetEventProperty(string name)
        {
            return DelegateDisposable.SetAndRestore(() => property, GetTargetEventProperty(name));
        }

        private static LogEventProperty GetTargetEventProperty(string name)
        {
            var length = Math.Min(name.Length, TargetNameLength);
            var paddedName = name[..length];
            return new LogEventProperty("ExecutingTarget", new ScalarValue(paddedName));
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(Current);
        }
    }
}
