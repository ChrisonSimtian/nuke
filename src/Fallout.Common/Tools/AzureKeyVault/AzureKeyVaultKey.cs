using Azure.Security.KeyVault.Keys;

namespace Fallout.Common.Tools.AzureKeyVault;

public class AzureKeyVaultKey
{
    public JsonWebKey Key { get; internal set; }

    public string Secret { get; internal set; }
}
