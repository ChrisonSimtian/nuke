using Fallout.Common.Utilities;

namespace Fallout.Common.CI.SpaceAutomation.Configuration;

public class SpaceAutomationCronScheduleTrigger : SpaceAutomationTrigger
{
    public string CronExpression { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine($"schedule {{ cron({CronExpression.DoubleQuote()}) }}");
    }
}
