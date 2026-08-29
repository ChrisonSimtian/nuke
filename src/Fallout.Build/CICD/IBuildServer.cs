namespace Fallout.Common.CI;

public interface IBuildServer
{
    string Branch { get; }

    string Commit { get; }
}
