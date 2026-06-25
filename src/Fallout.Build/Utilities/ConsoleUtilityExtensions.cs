using System;
using System.Linq;

namespace Fallout.Common.Utilities;

public static class ConsoleUtilityExtensions
{
    private static readonly char[] AllowedSpecialCharacters = ['.', '/', '\\', '_', '-'];

    public static bool IsValidInputKey(this ConsoleKeyInfo key)
    {
        return key.Key is >= ConsoleKey.A and <= ConsoleKey.Z
               || key.Key is >= ConsoleKey.D0 and <= ConsoleKey.D9
               || AllowedSpecialCharacters.Any(x => x == key.KeyChar)
               || char.IsLetterOrDigit(key.KeyChar);
    }
}
