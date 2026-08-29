using System;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.AzurePipelines.Configuration;

public static class AzurePipelinesCustomWriterExtensions
{
    public static IDisposable WriteBlock(this CustomFileWriter writer, string text)
    {
        return DelegateDisposable
            .CreateBracket(() => writer.WriteLine(text))
            .CombineWith(writer.Indent());
    }
}
