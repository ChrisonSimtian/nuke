using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fallout.Common.Execution;

/// <summary>
/// Composition root for the logging seam. Serilog stays the provider — this only puts
/// <see cref="ILogger"/> in front of it so framework code can stop referencing Serilog directly.
/// </summary>
/// <remarks>
/// Internal on purpose: the abstraction is the framework's own foundation, not public surface yet.
/// The root <c>AssemblyInfo.cs</c> grants <c>InternalsVisibleTo</c> to <c>Fallout.Cli</c> and the
/// spec assemblies, which is everything that needs to wire a container today.
/// </remarks>
internal static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Configures the Serilog pipeline for <paramref name="build"/> and registers the
    /// <see cref="ILogger"/> abstraction over it.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>services.AddLogging(...)</c>. That installs Microsoft.Extensions.Logging's
    /// own filter pipeline, whose default minimum is <see cref="LogLevel.Information"/> — a second
    /// level authority that would silently drop trace and debug records before Serilog ever saw
    /// them. Registering the Serilog factory directly leaves <see cref="Logging.LevelSwitch"/> as the
    /// only thing deciding what gets logged.
    /// </remarks>
    public static IServiceCollection AddFalloutLogging(this IServiceCollection services, IFalloutBuild build = null)
    {
        Logging.Configure(build);

        services.TryAddSingleton(_ => Logging.CreateSerilogLoggerFactory());

        // Logger<T> is a thin wrapper that defers to ILoggerFactory, so it inherits the factory
        // above rather than introducing a filter pipeline of its own.
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(Logging.DefaultCategoryName));

        return services;
    }
}
