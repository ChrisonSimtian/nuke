// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Model;

namespace Fallout.Persistence.Solution.Serializer;

internal abstract class SingleFileSerializerBase<TSettings> : ISolutionSingleFileSerializer<TSettings>
{
    public abstract string Name { get; }

    public string DefaultFileExtension => this.FileExtension;

    private protected abstract string FileExtension { get; }

    public abstract ISerializerModelExtension CreateModelExtension();

    public abstract ISerializerModelExtension CreateModelExtension(TSettings settings);

    Task<SolutionModel> ISolutionSingleFileSerializer<TSettings>.OpenAsync(Stream reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return this.ReadModelAsync(fullPath: null, reader, cancellationToken);
    }

    Task ISolutionSingleFileSerializer<TSettings>.SaveAsync(Stream writer, SolutionModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return this.WriteModelAsync(fullPath: null, model, writer, cancellationToken);
    }

    bool ISolutionSerializer.IsSupported(string fullPath)
    {
        return Path.GetExtension(fullPath.AsSpan()).EqualsOrdinalIgnoreCase(this.FileExtension);
    }

    async Task<SolutionModel> ISolutionSerializer.OpenAsync(string moniker, CancellationToken cancellationToken)
    {
        // Plain `using` (not `await using`): FileStream does not implement IAsyncDisposable on
        // netstandard2.0, so `await using` fails to compile there (CS8417). Synchronous disposal
        // of a local read stream is fine across all target frameworks.
        using FileStream reader = File.OpenRead(moniker);
        return await this.ReadModelAsync(moniker, reader, cancellationToken);
    }

    async Task ISolutionSerializer.SaveAsync(string moniker, SolutionModel model, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(moniker);
        if (directory is not null && !Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        // Plain `using` (not `await using`): see OpenAsync — FileStream is not IAsyncDisposable
        // on netstandard2.0 (CS8417). Synchronous disposal of a local write stream is fine here.
        using FileStream writer = File.OpenWrite(moniker);
        await this.WriteModelAsync(moniker, model, writer, cancellationToken);
    }

    private protected abstract Task<SolutionModel> ReadModelAsync(string? fullPath, Stream reader, CancellationToken cancellationToken);

    private protected abstract Task WriteModelAsync(string? fullPath, SolutionModel model, Stream writerStream, CancellationToken cancellationToken);
}
