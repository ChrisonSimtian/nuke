using Fallout.Common.IO;

namespace Fallout.Components;

public interface IHasReports : IHasArtifacts
{
    AbsolutePath ReportDirectory => ArtifactsDirectory / "reports";
}
