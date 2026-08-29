using System.Threading;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Persistence.Solution.Serializer;

namespace Fallout.Solutions;

public static class SolutionModelExtensions
{
    public static Solution ReadSolution(this AbsolutePath path)
    {
        return path.ReadSolution<Solution>();
    }

    public static Solution ReadSolution<T>(this AbsolutePath path)
        where T : Solution
    {
        var serializer = SolutionSerializers.GetSerializerByMoniker(path).NotNull();
        var model = AsyncHelper.RunSync(() => serializer.OpenAsync(path, CancellationToken.None));
        return typeof(T).CreateInstance<T>(model, path);
    }
}
