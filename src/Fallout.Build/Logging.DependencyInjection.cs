using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fallout.Common.Execution;

/// <summary>
/// Composition root for the logging seam. Serilog stays the provider. This only puts
/// <see cref="ILogger"/> in front of it, so framework code can stop referencing Serilog directly.
/// </summary>
/// <remarks>
/// Internal on purpose: the abstraction is the framework's own foundation, not public surface yet.
/// The root <c>AssemblyInfo.cs</c> grants <c>InternalsVisibleTo</c> to <c>Fallout.Cli</c> and the
/// spec assemblies, which is everything that needs to wire a container today.
/// </remarks>
internal static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ILogger"/> abstraction over the Serilog pipeline.
    /// </summary>
    /// <remarks>
    /// Registration only. This method never touches the pipeline, so any container can call it, any
    /// number of times. Installing the pipeline is <see cref="Logging.Configure"/>, which the caller
    /// runs explicitly just before it builds the provider.
    ///
    /// The two are kept apart because <see cref="Logging.Configure"/> is not idempotent. It
    /// reassigns Serilog's <c>Log.Logger</c> on every call. Called with no build, it installs a
    /// pipeline with no file sinks, no host sink and no filter. A second container calling this
    /// method would then wipe out the pipeline the first one had set up.
    ///
    /// Deliberately not <c>services.AddLogging(...)</c>. That installs Microsoft.Extensions.Logging's
    /// own filter pipeline, whose default minimum is <see cref="LogLevel.Information"/>. It would be
    /// a second authority on levels, dropping trace and debug records before Serilog ever saw them.
    /// Registering the Serilog factory directly leaves <see cref="Logging.LevelSwitch"/> as the only
    /// thing that decides what gets logged.
    /// </remarks>
    public static IServiceCollection AddFalloutLogging(this IServiceCollection services)
    {
        // Safe as a singleton because the factory is left unbound: it reads the ambient pipeline
        // every time it creates a logger. See Logging.CreateSerilogLoggerFactory.
        services.TryAddSingleton(_ => Logging.CreateSerilogLoggerFactory());

        // Transient, not singleton. Logger<T> resolves its inner logger in its constructor, and the
        // non-generic lambda would run once per container. As singletons, both would pin every
        // consumer to whichever pipeline was current at the first resolution. Log.Logger does not
        // stay put during a run: Configure installs it late, and Host.WriteErrorsAndWarnings swaps
        // it again to render the end-of-build summary.
        //
        // Transient means each resolution binds to the pipeline that is current right now. It does
        // not rescue a component that resolves a logger once and holds it across a swap. Any
        // component that outlives a swap must read Logging.Logger at the point of writing instead.
        services.TryAddTransient(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddTransient(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(Logging.DefaultCategoryName));

        return services;
    }
}
