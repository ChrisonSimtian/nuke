using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Fallout.Common;

namespace Fallout.Build.Utilities;

public class SystemConsole : IConsole
{
    public int BufferWidth => EnvironmentInfo.IsWin ? Console.BufferWidth - 1 : Console.BufferWidth;

    public int CursorLeft
    {
        get => Console.CursorLeft;
        set => Console.CursorLeft = value;
    }

    public int CursorTop
    {
        get => Console.CursorTop;
        set => Console.CursorTop = value;
    }

    public void Write(string value, Color? color = null)
    {
        Console.Write(value);
    }

    public void WriteLine()
    {
        Console.WriteLine();
    }

    public void WriteLine(string value, Color? color = null)
    {
        Console.WriteLine(value);
    }

    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);
}
