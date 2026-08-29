using Fallout.Common.Tooling;
using Serilog.Events;

namespace Fallout.Common.Tools.Docker;

[LogErrorAsStandard]
[LogLevelPattern(LogEventLevel.Warning, "^WARNING!")]
partial class DockerTasks;
