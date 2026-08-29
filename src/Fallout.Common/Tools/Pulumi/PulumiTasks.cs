using Fallout.Common.Tooling;
using Serilog.Events;

namespace Fallout.Common.Tools.Pulumi;

[LogLevelPattern(LogEventLevel.Warning, "^warning:")]
partial class PulumiTasks;
