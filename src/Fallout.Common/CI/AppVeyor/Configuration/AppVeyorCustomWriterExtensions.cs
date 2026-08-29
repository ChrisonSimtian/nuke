using System;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.AppVeyor.Configuration;

public static class AppVeyorCustomWriterExtensions
{
    public static IDisposable WriteBlock(this CustomFileWriter writer, string text)
    {
        return DelegateDisposable
            .CreateBracket(() => writer.WriteLine(text))
            .CombineWith(writer.Indent());
    }
}
