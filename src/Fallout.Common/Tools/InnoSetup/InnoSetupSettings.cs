namespace Fallout.Common.Tools.InnoSetup;

public partial class InnoSetupSettings
{
    private static string GetInnoSetupBool(bool? value)
    {
        return value switch
        {
            null => null,
            true => "+",
            false => "-"
        };
    }

    private string GetOutput()
    {
        return GetInnoSetupBool(Output);
    }
}
