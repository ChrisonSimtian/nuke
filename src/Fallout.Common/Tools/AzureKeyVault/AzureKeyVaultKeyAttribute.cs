using System.Reflection;

namespace Fallout.Common.Tools.AzureKeyVault;

/// <summary>Attribute to obtain a key from from the Azure KeyVault defined by <see cref="AzureKeyVaultConfigurationAttribute"/>.</summary>
public class AzureKeyVaultKeyAttribute(string keyName = null) : AzureKeyVaultAttributeBase
{
    protected override object GetValue(AzureKeyVaultConfiguration configuration, MemberInfo member)
    {
        return AzureKeyVaultTasks.GetKeyBundle(configuration, keyName ?? member.Name);
    }
}
