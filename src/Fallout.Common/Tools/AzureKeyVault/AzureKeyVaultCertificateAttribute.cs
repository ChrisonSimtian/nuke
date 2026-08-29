using System.Reflection;

namespace Fallout.Common.Tools.AzureKeyVault;

/// <summary> Attribute to obtain a certificates from from the Azure KeyVault defined by <see cref="AzureKeyVaultConfigurationAttribute"/>.</summary>
public class AzureKeyVaultCertificateAttribute(string certificateName = null) : AzureKeyVaultAttributeBase
{
    /// <summary>If set to true, the key of the certificate is also obtained.</summary>
    public bool IncludeKey { get; set; } = true;

    /// <summary>If set to true, the secret of the certificate is also obtained.</summary>
    public bool IncludeSecret { get; set; } = true;

    protected override object GetValue(AzureKeyVaultConfiguration configuration, MemberInfo member)
    {
        return AzureKeyVaultTasks.GetCertificateBundle(configuration, certificateName, IncludeKey, IncludeSecret);
    }
}
