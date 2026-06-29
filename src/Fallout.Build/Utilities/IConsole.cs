using System;
using System.Drawing;

namespace Fallout.Build.Utilities;

public interface IConsole
{
    int BufferWidth { get; }
    int CursorLeft { get; set; }
    int CursorTop { get; set; }
    void Write(string value, Color? color = null);
    void WriteLine();
    void WriteLine(string value, Color? color = null);
    ConsoleKeyInfo ReadKey(bool intercept);
}
