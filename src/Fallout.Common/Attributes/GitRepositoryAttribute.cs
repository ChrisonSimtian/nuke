using System.Reflection;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Git;

/// <summary>
/// Injects an instance of <see cref="GitRepository"/> based on the local repository.
/// </summary>
public class GitRepositoryAttribute : ValueInjectionAttributeBase
{
    public override object GetValue(MemberInfo member, object instance)
    {
        return GitRepository.FromLocalDirectory(Build.RootDirectory);
    }
}
