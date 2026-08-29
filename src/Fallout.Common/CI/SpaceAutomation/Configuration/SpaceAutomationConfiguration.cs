using System.Linq;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.SpaceAutomation.Configuration;

public class SpaceAutomationConfiguration : ConfigurationEntity
{
    public string Name { get; set; }

    public string VolumeSize { get; set; }

    public string[] RefSpec { get; set; }

    public SpaceAutomationContainer Container { get; set; }

    public SpaceAutomationTrigger[] Triggers { get; set; }

    public int? TimeoutInMinutes { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock($"job({Name.DoubleQuote()})"))
        {
            if (VolumeSize != null)
            {
                writer.WriteLine($"volumeSize = {VolumeSize}");
                writer.WriteLine();
            }

            using (writer.WriteBlock("git"))
            {
                writer.WriteLine("depth = UNLIMITED_DEPTH");

                if (RefSpec != null)
                {
                    using (writer.WriteBlock("refSpec"))
                    {
                        RefSpec.ForEach(x => writer.WriteLine($"+{x.DoubleQuote()}"));
                    }
                }
            }

            writer.WriteLine();
            Container.Write(writer);

            if (Triggers.Any())
            {
                writer.WriteLine();
                using (writer.WriteBlock("startOn"))
                {
                    Triggers.ForEach(x => x.Write(writer));
                }
            }

            if (TimeoutInMinutes != null)
            {
                writer.WriteLine();
                using (writer.WriteBlock("failOn"))
                {
                    writer.WriteLine($"timeOut {{ timeOutInMinutes = {TimeoutInMinutes} }}");
                }
            }
        }
    }
}
