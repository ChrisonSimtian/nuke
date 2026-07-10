using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fallout.Common;
using Fallout.Common.IO;
using Xunit;
using Xunit.Sdk;

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

        TestName = TestContext.Current.Test?.TestDisplayName ?? "";
        TestName = Regex.Replace(TestName, @"[<>:""/\\|?*\x00-\x1F]", "_").Trim().TrimEnd('.', ' ');
        TestName =  Regex.IsMatch(TestName, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase)
            ? $"_{TestName}"
            : TestName;

        ExecutionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).NotNull();
        RootDirectory = Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory);
        TestProjectDirectory = ExecutionDirectory.FindParentOrSelf(x => x.ContainsFile("*.csproj"));
        TestTempDirectory = ExecutionDirectory / "temp" / $"{GetType().Name}.{TestName}";

        TestTempDirectory.CreateOrCleanDirectory();
    }
}
