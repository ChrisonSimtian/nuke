using System.Reflection;

namespace Fallout.Common.Tools.AzureKeyVault;

/// <summary>Attribute to obtain the KeyVault defined by <see cref="AzureKeyVaultConfigurationAttribute"/> to retrieve multiple items.</summary>
public class AzureKeyVaultAttribute : AzureKeyVaultAttributeBase
{
    protected override object GetValue(AzureKeyVaultConfiguration configuration, MemberInfo member)
    {
        return AzureKeyVaultTasks.LoadVault(configuration);
    }
}
