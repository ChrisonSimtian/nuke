using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Fallout.Common;
using Fallout.Common.IO;
using Xunit.Abstractions;

namespace Fallout.Common.Specs;

public abstract class FileSystemDependentSpecs
{
    public ITestOutputHelper TestOutputHelper { get; }
    public string TestName { get; }
    public AbsolutePath ExecutionDirectory { get; }
    public AbsolutePath TestProjectDirectory { get; }
    public AbsolutePath RootDirectory { get; }
    public AbsolutePath TestTempDirectory { get; }

    protected FileSystemDependentSpecs(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;

        TestName = ((ITest) testOutputHelper.GetType()
            .GetField("test", BindingFlags.NonPublic | BindingFlags.Instance).NotNull()
            .GetValue(testOutputHelper).NotNull()).TestCase.TestMethod.Method.Name;

        ExecutionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).NotNull();
        RootDirectory = Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory);
        TestProjectDirectory = ExecutionDirectory.FindParentOrSelf(x => x.ContainsFile("*.csproj"));
        TestTempDirectory = ExecutionDirectory / "temp" / $"{GetType().Name}.{TestName}";

        TestTempDirectory.CreateOrCleanDirectory();
    }
}
