using System;
using Fallout.Common.Tooling;
using Serilog.Events;

namespace Fallout.Common.Tools.Npm;

[LogLevelPattern(LogEventLevel.Warning, "^(npmWARN|npm WARN|npm warn)")]
[LogLevelPattern(LogEventLevel.Debug, "^(npm notice)")]
partial class NpmTasks;
