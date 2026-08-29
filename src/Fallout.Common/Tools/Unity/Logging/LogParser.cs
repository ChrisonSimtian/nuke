using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Common.Tools.Unity.Logging;

internal class LogParser
{
    private readonly Stack<MatchedBlock> blockStack;
    private readonly Action<string, LogLevel> logLineAction;
    private readonly Action<MatchedBlock> logBlockStartAction;
    private readonly Action<MatchedBlock> logBlockEndAction;

    private readonly IReadOnlyList<BlockMatcher> blockMatchers =
        new[]
        {
            new BlockMatcher("Player statistics", "\\*\\*\\*Player size statistics\\*\\*\\*", "Unloading.*",
                endMatchType: MatchType.Exclusive),
            new BlockMatcher("Lightmap", "---- Lightmapping Start for (.*) ----", "---- Lightmapping End for (.*) ----"),
            new BlockMatcher("Compile", "-----Compiler Commandline Arguments:", "-----EndCompilerOutput---------------"),
            new BlockMatcher("Update", "Updating (.+) - GUID: .*", "\\s*done: hash - .+"),
            new BlockMatcher("Prepare Build", "---- PrepareBuild Start ----", "---- PrepareBuild End ----")
        };

    private readonly IReadOnlyList<LineMatcher> lineMatchers =
        new[]
        {
            // Warnings
            new LineMatcher("Script attached to.*?is missing or no valid script is attached.", LogLevel.Warning),
            new LineMatcher(".*?warning CS\\d+.*?", LogLevel.Warning),
            new LineMatcher("There are inconsistent line endings in the.*?", LogLevel.Warning),
            new LineMatcher("This might lead to incorrect line numbers in stacktraces and compiler errors.*?", LogLevel.Warning),
            new LineMatcher("WARNING.*", LogLevel.Warning),

            // Errors
            new LineMatcher(".*?error CS\\d+.*?", LogLevel.Error),
            new LineMatcher("Compilation failed:.*", LogLevel.Error),
            new LineMatcher("Scripts have compiler errors\\..*", LogLevel.Error),
            new LineMatcher("An error occured", LogLevel.Warning)
        };

    public LogParser(
        Action<string, LogLevel> logLineAction,
        Action<MatchedBlock> logBlockStartAction,
        Action<MatchedBlock> logBlockEndAction)
    {
        this.logLineAction = logLineAction ?? throw new ArgumentNullException(nameof(logLineAction));
        this.logBlockStartAction = logBlockStartAction ?? throw new ArgumentNullException(nameof(logBlockStartAction));
        this.logBlockEndAction = logBlockEndAction ?? throw new ArgumentNullException(nameof(logBlockEndAction));

        blockStack = new Stack<MatchedBlock>();
    }

    public void Log(string message)
    {
        if (blockStack.Count != 0)
        {
            var match = blockStack.Peek().MatchesEnd(message);
            switch (match)
            {
                case MatchType.Inclusive:
                    LogLine(message);
                    LogBlockEnd();
                    return;

                case MatchType.Exclusive:
                    LogBlockEnd();
                    break;
            }
        }

        var block = blockMatchers.Select(x => x.MatchesBeginning(message)).FirstOrDefault(x => x != null);

        if (block != null)
        {
            switch (block.MatchType)
            {
                case MatchType.Inclusive:
                    LogBlockStart(block);
                    break;

                case MatchType.Exclusive:
                    LogLine(message);
                    LogBlockStart(block);
                    return;
            }
        }

        LogLine(message);
    }

    private void LogBlockStart(MatchedBlock block)
    {
        blockStack.Push(block);
        logBlockStartAction(block);
    }

    private void LogLine(string message)
    {
        var line = lineMatchers.FirstOrDefault(x => x.Matches(message));
        Log(message, line?.LogLevel ?? LogLevel.Normal);
    }

    private void LogBlockEnd()
    {
        var block = blockStack.Pop();
        logBlockEndAction(block);
    }

    private void Log(string message, LogLevel logLevel)
    {
        message = message.TrimEnd('\r', '\n');
        logLineAction(message, logLevel);
    }
}
