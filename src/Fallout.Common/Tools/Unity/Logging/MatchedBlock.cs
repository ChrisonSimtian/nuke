namespace Fallout.Common.Tools.Unity.Logging;

internal class MatchedBlock
{
    public BlockMatcher BlockMatcher { get; }

    public string Name { get; }

    public MatchType MatchType { get; }

    public MatchedBlock(BlockMatcher blockMatcher, string name, MatchType matchType)
    {
        BlockMatcher = blockMatcher;
        Name = name;
        MatchType = matchType;
    }

    public MatchType MatchesEnd(string message)
    {
        return BlockMatcher.MatchesEnd(message);
    }
}
