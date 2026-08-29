using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.CI.SpaceAutomation.Configuration;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.SpaceAutomation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SpaceAutomationAttribute : ConfigurationAttributeBase
{
    private readonly string name;
    private readonly string image;

    private bool submodules;
    private bool? onPush;
    private int? timeoutInMinutes;

    public SpaceAutomationAttribute(string name, string image)
    {
        this.name = name;
        this.image = image;
    }

    public override Type HostType => typeof(SpaceAutomation);

    public override AbsolutePath ConfigurationFile => Build.RootDirectory / ".space.kts";

    public override IEnumerable<AbsolutePath> GeneratedFiles => new[]
    {
        ConfigurationFile
    };

    public override IEnumerable<string> RelevantTargetNames => InvokedTargets;

    public override IEnumerable<string> IrrelevantTargetNames => new string[0];

    public string VolumeSize { get; set; }

    public string ResourcesCpu { get; set; }

    public string ResourcesMemory { get; set; }

    public string[] RefSpec { get; set; } =
    {
        "refs/heads/*:refs/heads/*"
    };

    public bool Submodules
    {
        set => submodules = value;
        get => throw new NotSupportedException();
    }

    public string[] InvokedTargets { get; set; } = new string[0];

    public bool OnPush
    {
        set => onPush = value;
        get => throw new NotSupportedException();
    }

    public string[] OnPushBranchIncludes { get; set; }

    public string[] OnPushBranchExcludes { get; set; }

    public string[] OnPushBranchRegexIncludes { get; set; }

    public string[] OnPushBranchRegexExcludes { get; set; }

    public string[] OnPushPathIncludes { get; set; }

    public string[] OnPushPathExcludes { get; set; }

    public string OnCronSchedule { get; set; }

    public string[] ImportSecrets { get; set; } = new string[0];

    public int TimeoutInMinutes
    {
        set => timeoutInMinutes = value;
        get => throw new NotSupportedException();
    }

    public override CustomFileWriter CreateWriter(StreamWriter streamWriter)
    {
        return new CustomFileWriter(streamWriter, indentationFactor: 4, commentPrefix: "//");
    }

    public override ConfigurationEntity GetConfiguration(IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        return new SpaceAutomationConfiguration
        {
            Name = name,
            VolumeSize = VolumeSize,
            RefSpec = RefSpec,
            Container = GetContainer(),
            Triggers = GetTriggers().ToArray(),
            TimeoutInMinutes = timeoutInMinutes
        };
    }

    protected virtual SpaceAutomationContainer GetContainer()
    {
        return new SpaceAutomationContainer
        {
            Image = image,
            Resources = GetResources(),
            Imports = GetImports().ToDictionary(x => x.Key, x => x.Value),
            Submodules = submodules,
            BuildScript = BuildCmdPath.Replace(".cmd", ".sh"),
            InvokedTargets = InvokedTargets
        };
    }

    protected virtual SpaceAutomationResources GetResources()
    {
        return new SpaceAutomationResources
        {
            Cpu = ResourcesCpu,
            Memory = ResourcesMemory
        };
    }

    private IEnumerable<(string Key, string Value)> GetImports()
    {
        return ImportSecrets.Select(x => ($"env[{x.DoubleQuote()}]",
            $"Secrets({x.SplitCamelHumpsWithKnownWords().JoinDash().ToLowerInvariant().DoubleQuote()})"));
    }

    protected virtual IEnumerable<SpaceAutomationTrigger> GetTriggers()
    {
        if (onPush != null ||
            OnPushBranchIncludes != null ||
            OnPushBranchExcludes != null ||
            OnPushBranchRegexIncludes != null ||
            OnPushBranchRegexExcludes != null ||
            OnPushPathIncludes != null ||
            OnPushPathExcludes != null)
        {
            yield return new SpaceAutomationPushTrigger
            {
                OnPush = onPush,
                OnPushBranchIncludes = OnPushBranchIncludes,
                OnPushBranchExcludes = OnPushBranchExcludes,
                OnPushBranchRegexIncludes = OnPushBranchRegexIncludes,
                OnPushBranchRegexExcludes = OnPushBranchRegexExcludes,
                OnPushPathIncludes = OnPushPathIncludes,
                OnPushPathExcludes = OnPushPathExcludes
            };
        }

        if (OnCronSchedule != null)
        {
            yield return new SpaceAutomationCronScheduleTrigger
            {
                CronExpression = OnCronSchedule
            };
        }
    }
}
