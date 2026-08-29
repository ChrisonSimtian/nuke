using System;
using System.Runtime.Serialization;

namespace Fallout.Common.Execution;

[Serializable]
internal class TargetExecutionException : Exception
{
    public TargetExecutionException(string targetName, Exception inner)
        : base($"Target '{targetName}' has thrown an exception.", inner)
    {
    }

    protected TargetExecutionException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
    }
}
