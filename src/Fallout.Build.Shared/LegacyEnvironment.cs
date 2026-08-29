using System;
using System.Collections.Generic;

namespace Fallout.Common;

/// <summary>
/// Helpers for reading environment variables during the NUKE → Fallout rename.
/// Reads the preferred (FALLOUT_*) name first, falls back to the legacy (NUKE_*)
/// name if only the legacy is set, and emits a one-time warning per legacy key.
/// </summary>
internal static class LegacyEnvironment
{
    private static readonly HashSet<string> WarnedLegacyKeys = new(StringComparer.Ordinal);
    private static readonly object WarnedLegacyKeysLock = new();

    /// <summary>
    /// Returns the value of <paramref name="preferredName"/> if set; otherwise the value of
    /// <paramref name="legacyName"/> (with a deprecation warning); otherwise <c>null</c>.
    /// </summary>
    public static string Read(string preferredName, string legacyName)
    {
        var preferred = Environment.GetEnvironmentVariable(preferredName);
        if (!string.IsNullOrEmpty(preferred))
        {
            return preferred;
        }

        var legacy = Environment.GetEnvironmentVariable(legacyName);
        if (string.IsNullOrEmpty(legacy))
        {
            return null;
        }

        WarnOnce(legacyName, preferredName);
        return legacy;
    }

    /// <summary>
    /// Same as <see cref="Read"/> but for the dictionary-style access used by EnvironmentInfo.Variables.
    /// </summary>
    public static string ReadFromVariables(IReadOnlyDictionary<string, string> variables, string preferredName, string legacyName)
    {
        if (variables.TryGetValue(preferredName, out var preferred) && !string.IsNullOrEmpty(preferred))
        {
            return preferred;
        }

        if (!variables.TryGetValue(legacyName, out var legacy) || string.IsNullOrEmpty(legacy))
        {
            return null;
        }

        WarnOnce(legacyName, preferredName);
        return legacy;
    }

    private static void WarnOnce(string legacyName, string preferredName)
    {
        lock (WarnedLegacyKeysLock)
        {
            if (!WarnedLegacyKeys.Add(legacyName))
            {
                return;
            }
        }

        Console.Error.WriteLine(
            $"warning FALLOUT002: Environment variable '{legacyName}' is deprecated; rename to '{preferredName}'. " +
            "The legacy name still works in 10.x but will be removed in 11.0.");
    }
}
