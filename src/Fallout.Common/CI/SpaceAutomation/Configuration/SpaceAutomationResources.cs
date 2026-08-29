using Fallout.Common.Utilities;

namespace Fallout.Common.CI.SpaceAutomation.Configuration;

public class SpaceAutomationResources : ConfigurationEntity
{
    public string Cpu { get; set; }

    public string Memory { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        if (Cpu != null || Memory != null)
        {
            using (writer.WriteBlock("resources"))
            {
                if (Cpu != null)
                {
                    writer.WriteLine($"cpu = {Cpu}");
                }

                if (Memory != null)
                {
                    writer.WriteLine($"memory = {Memory}");
                }
            }
        }
    }
}
