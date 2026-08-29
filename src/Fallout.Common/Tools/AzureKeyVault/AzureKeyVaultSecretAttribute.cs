using System.Reflection;

namespace Fallout.Common.Tools.AzureKeyVault;

/// <summary>Attribute to obtain a secret from the Azure KeyVault defined by <see cref="AzureKeyVaultConfigurationAttribute"/>.</summary>
public class AzureKeyVaultSecretAttribute(string secretName = null) : AzureKeyVaultAttributeBase
{
    protected override object GetValue(AzureKeyVaultConfiguration configuration, MemberInfo member)
    {
        return ParameterService.GetParameter<string>(member.Name) ??
               AzureKeyVaultTasks.GetSecret(configuration, secretName ?? member.Name);
    }
}
