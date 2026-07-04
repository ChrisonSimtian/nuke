using System;
using System.IO;
using System.Linq;
using Fallout.MSBuildTasks.Engine;
using Fallout.MSBuildTasks.Protocol;

// Invoked by the net472 bridge as: <verb> <input.json> <output.json>
// Diagnostics flow through the streams the bridge's ToolTask reads natively — messages to stdout,
// errors to stderr — and the exit code signals success (0 = ok, 1 = failure, 2 = bad invocation).
// The output JSON carries only item outputs (embed-packages / pack-tools).
if (args.Length != 3)
{
    Console.Error.WriteLine("usage: <verb> <input.json> <output.json>");
    return 2;
}

var (verb, inputPath, outputPath) = (args[0], args[1], args[2]);

try
{
    var inputJson = File.ReadAllText(inputPath);
    var response = new WorkerResponse();
    switch (verb)
    {
        case WorkerVerbs.Codegen:
            var cg = WorkerJson.Deserialize<CodegenRequest>(inputJson);
            CodeGenerationEngine.Generate(
                cg.SpecificationFiles, cg.BaseDirectory, cg.UseNestedNamespaces,
                cg.BaseNamespace, cg.UpdateReferences, Console.Out.WriteLine);
            break;

        case WorkerVerbs.EmbedPackages:
            var ep = WorkerJson.Deserialize<EmbedPackagesRequest>(inputJson);
            response.Files = PackageToolingEngine.GetEmbeddablePackageFiles(ep.ProjectAssetsFile).ToArray();
            break;

        case WorkerVerbs.PackTools:
            var pt = WorkerJson.Deserialize<PackToolsRequest>(inputJson);
            response.ToolFiles = PackageToolingEngine
                .GetPackageToolFiles(pt.ProjectAssetsFile, pt.NuGetPackageRoot, pt.TargetFramework)
                .Select(x => new PackagedToolFileDto { File = x.File, BuildAction = x.BuildAction, PackagePath = x.PackagePath })
                .ToArray();
            break;

        default:
            Console.Error.WriteLine($"Unknown verb '{verb}'.");
            return 2;
    }

    File.WriteAllText(outputPath, WorkerJson.Serialize(response));
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
