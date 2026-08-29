using System.Text;

namespace Fallout.Common.IO;

public static class TextTasks
{
    public static UTF8Encoding UTF8NoBom => new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
