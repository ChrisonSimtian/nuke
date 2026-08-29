using System.Globalization;
using System.IO;
using System.Text;

namespace Fallout.Persistence.Solution.Benchmarks;

// Generates `.slnx` content of a given shape (project count, with or without
// folders) so benchmarks can isolate scaling behaviour without committing
// binary fixture blobs to the repo. The structure mirrors what Visual Studio
// would emit:
//
//   <Solution>
//     <Folder Name="/group-0/">
//       <Project Path="src/Project0/Project0.csproj" />
//       ...
//     </Folder>
//     <Project Path="src/ProjectN/ProjectN.csproj" />
//   </Solution>
//
// All projects point at the same Project.csproj stub (which doesn't need to
// exist on disk for the parser benchmark — the serializer reads the .slnx
// only, not the referenced csprojs).
internal static class SlnxFixtureBuilder
{
    public static string Build(int projectCount, bool withFolders)
    {
        var sb = new StringBuilder(capacity: 256 + projectCount * 96);
        sb.AppendLine("<Solution>");

        if (withFolders)
        {
            // Group projects into folders of 10 to exercise the folder
            // parsing path; for 1-project counts a single folder is created.
            const int projectsPerFolder = 10;
            var folderCount = (projectCount + projectsPerFolder - 1) / projectsPerFolder;
            for (var f = 0; f < folderCount; f++)
            {
                sb.Append("  <Folder Name=\"/group-").Append(f.ToString(CultureInfo.InvariantCulture)).AppendLine("/\">");
                var start = f * projectsPerFolder;
                var end = (start + projectsPerFolder).LessOrEqual(projectCount);
                for (var p = start; p < end; p++)
                {
                    AppendProject(sb, p, indent: "    ");
                }

                sb.AppendLine("  </Folder>");
            }
        }
        else
        {
            for (var p = 0; p < projectCount; p++)
            {
                AppendProject(sb, p, indent: "  ");
            }
        }

        sb.AppendLine("</Solution>");
        return sb.ToString();
    }

    private static void AppendProject(StringBuilder sb, int index, string indent)
    {
        var n = index.ToString(CultureInfo.InvariantCulture);
        sb.Append(indent).Append("<Project Path=\"src/Project").Append(n).Append("/Project").Append(n).AppendLine(".csproj\" />");
    }

    public static string WriteToTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"fallout-bench-{Path.GetRandomFileName()}.slnx");
        File.WriteAllText(path, content);
        return path;
    }

    private static int LessOrEqual(this int value, int max) => value <= max ? value : max;
}
