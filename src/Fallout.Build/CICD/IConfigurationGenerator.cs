using System;
using System.Collections.Generic;
using Fallout.Common.Execution;
using Fallout.Common.IO;

namespace Fallout.Common.CI;

public interface IConfigurationGenerator
{
    string Id { get; }

    string DisplayName { get; }

    string HostName { get; }

    bool AutoGenerate { get; }

    Type HostType { get; }

    IEnumerable<AbsolutePath> GeneratedFiles { get; }

    void Generate(IReadOnlyCollection<ExecutableTarget> executableTargets);
    void SerializeState();
}
