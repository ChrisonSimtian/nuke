using System.Collections.Generic;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.SpaceAutomation.Configuration;

public class SpaceAutomationContainer : ConfigurationEntity
{
    public string Image { get; set; }

    public SpaceAutomationResources Resources { get; set; }

    public Dictionary<string, string> Imports { get; set; }

    public string BuildScript { get; set; }

    public string[] InvokedTargets { get; set; }

    public bool Submodules { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock($"container({Image.DoubleQuote()})"))
        {
            Resources.Write(writer);
            Imports.ForEach(x => writer.WriteLine($"{x.Key} = {x.Value}"));

            using (writer.WriteBlock("shellScript"))
            {
                var scriptContent = new List<string>();
                if (Submodules)
                {
                    scriptContent.Add("git submodule update --init --recursive");
                }

                scriptContent.Add($"./{BuildScript} {InvokedTargets.JoinSpace()}");

                writer.WriteArray("content", scriptContent.ToArray());
            }
        }
    }
}
