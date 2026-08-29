using System;
using System.Collections.Generic;

namespace Fallout.Common.Tooling;

#if NET6_0_OR_GREATER
public delegate IReadOnlyCollection<Output> Tool(
    ArgumentStringHandler arguments,
    string workingDirectory = null,
    IReadOnlyDictionary<string, string> environmentVariables = null,
    int? timeout = null,
    bool? logOutput = null,
    bool? logInvocation = null,
    Action<OutputType, string> logger = null,
    Action<IProcess> exitHandler = null);
#else
public delegate IReadOnlyCollection<Output> Tool(
    string arguments,
    string workingDirectory = null,
    IReadOnlyDictionary<string, string> environmentVariables = null,
    int? timeout = null,
    bool? logOutput = null,
    bool? logInvocation = null,
    Action<OutputType, string> logger = null,
    Action<IProcess> exitHandler = null,
    Func<string, string> outputFilter = null);
#endif
