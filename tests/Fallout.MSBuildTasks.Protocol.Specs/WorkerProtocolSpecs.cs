using System.Linq;
using FluentAssertions;
using Xunit;

namespace Fallout.MSBuildTasks.Protocol.Specs;

/// <summary>
/// The wire contract between the net472 bridge and the net10 worker. Both ends share
/// <see cref="WorkerJson"/>, so these guard the two things that can break the bridge:
/// (1) the DTOs must survive a serialize→deserialize round-trip, and (2) the Protocol must carry
/// no System.Text.Json dependency — STJ can't bind when the bridge is loaded in-process into
/// full-framework MSBuild.exe (no host binding redirect). See ADR-0009 and
/// docs/agents/msbuild-bridge-smoke-test.md.
/// </summary>
public class WorkerProtocolSpecs
{
    [Fact]
    public void CodegenRequest_round_trips()
    {
        var original = new CodegenRequest
        {
            SpecificationFiles = ["a/Tool.json", "b/Other.json"],
            BaseDirectory = @"C:\src\gen",
            UseNestedNamespaces = true,
            BaseNamespace = "My.Tools",
            UpdateReferences = false,
        };

        var roundTripped = WorkerJson.Deserialize<CodegenRequest>(WorkerJson.Serialize(original));

        roundTripped.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void EmbedPackagesRequest_round_trips()
    {
        var original = new EmbedPackagesRequest { ProjectAssetsFile = @"obj\project.assets.json" };

        var roundTripped = WorkerJson.Deserialize<EmbedPackagesRequest>(WorkerJson.Serialize(original));

        roundTripped.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PackToolsRequest_round_trips()
    {
        var original = new PackToolsRequest
        {
            ProjectAssetsFile = @"obj\project.assets.json",
            NuGetPackageRoot = @"C:\Users\x\.nuget\packages",
            TargetFramework = "net10.0",
        };

        var roundTripped = WorkerJson.Deserialize<PackToolsRequest>(WorkerJson.Serialize(original));

        roundTripped.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void WorkerResponse_with_data_round_trips()
    {
        var original = new WorkerResponse
        {
            Files = ["pkg/one.dll", "pkg/two.dll"],
            ToolFiles =
            [
                new PackagedToolFileDto
                {
                    File = "tool.dll",
                    BuildAction = "Content",
                    PackagePath = "tools/net10.0/any/tool.dll",
                },
            ],
        };

        var roundTripped = WorkerJson.Deserialize<WorkerResponse>(WorkerJson.Serialize(original));

        roundTripped.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void WorkerResponse_defaults_round_trip_to_empty_collections()
    {
        var roundTripped = WorkerJson.Deserialize<WorkerResponse>(WorkerJson.Serialize(new WorkerResponse()));

        roundTripped.Files.Should().BeEmpty();
        roundTripped.ToolFiles.Should().BeEmpty();
    }

    [Fact]
    public void Protocol_has_no_System_Text_Json_dependency()
    {
        var referenced = typeof(WorkerJson).Assembly.GetReferencedAssemblies().Select(x => x.Name);

        referenced.Should().NotContain(
            "System.Text.Json",
            because: "the net472 bridge loads this assembly in-process into full-framework MSBuild.exe, "
                + "where STJ can't bind without a host binding redirect (ADR-0009)");
    }
}
