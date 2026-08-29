using System;
using System.Collections.Generic;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;

namespace Fallout.Common.Tools.OctoVersion;

partial class OctoVersionTasks
{
    protected override object GetResult<T>(ToolOptions options, IReadOnlyCollection<Output> output)
    {
        if (options is OctoVersionGetVersionSettings getVersion)
        {
            Assert.FileExists(getVersion.OutputJsonFile);
            try
            {
                var file = (AbsolutePath)getVersion.OutputJsonFile;
                // STJ deserializes records natively via constructor binding, so
                // the AllWritableContractResolver workaround Newtonsoft needed
                // is no longer required.
                return file.ReadJson<OctoVersionInfo>(JsonExtensions.DefaultSerializerOptions);
            }
            catch (Exception exception)
            {
                throw new Exception($"Cannot parse {nameof(OctoVersion)} output from {getVersion.OutputJsonFile.SingleQuote()}.",
                    exception);
            }
        }

        return null;
    }
}

public record OctoVersionInfo(
    int? Major,
    int? Minor,
    int? Patch,
    string PreReleaseTag,
    string PreReleaseTagWithDash,
    string BuildMetaData,
    string BuildMetadataWithPlus,
    string MajorMinorPatch,
    string NuGetCompatiblePreReleaseWithDash,
    string FullSemVer,
    string InformationalVersion,
    string NuGetVersion);
