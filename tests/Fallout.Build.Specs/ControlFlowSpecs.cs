using System;
using System.Linq;
using FluentAssertions;
using Xunit;

// ReSharper disable ArgumentsStyleLiteral

namespace Fallout.Common.Specs;

public class ControlFlowSpecs
{
    [Fact]
    public void Test()
    {
        var executions = 0;

        void OnSecondExecution()
        {
            executions++;
            if (executions != 2)
                throw new Exception(executions.ToString());
        }

        ControlFlow.ExecuteWithRetry(OnSecondExecution);
        executions.Should().Be(2);
    }
}
