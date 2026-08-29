namespace Fallout.Common.CI;

public static class BuildServerConfigurationGeneration
{
    public static bool IsActive { get; } = ParameterService.GetParameter<string>(ConfigurationParameterName) != null;

    public const string ConfigurationParameterName = "generate-configuration";
}
