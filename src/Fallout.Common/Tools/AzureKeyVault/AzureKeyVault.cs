using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Fallout.Common.Utilities;

namespace Fallout.Common.Tools.AzureKeyVault;

public class AzureKeyVaultConfiguration
{
    public virtual string TenantId { get; internal set; }

    public virtual string ClientId { get; internal set; }

    public virtual string ClientSecret { get; internal set; }

    public virtual string BaseUrl { get; internal set; }
}

public class AzureKeyVault
{
    private readonly Lazy<CertificateClient> certificateClient;
    private readonly Lazy<KeyClient> keyClient;
    private readonly Lazy<SecretClient> secretClient;

    internal AzureKeyVault(string tenantId, string clientId, string clientSecret, string baseUrl)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var uri = new Uri(baseUrl);

        keyClient = new Lazy<KeyClient>(() => new KeyClient(uri, credential));
        secretClient = new Lazy<SecretClient>(() => new SecretClient(uri, credential));
        certificateClient = new Lazy<CertificateClient>(() => new CertificateClient(uri, credential));
    }

    public CertificateClient CertificateClient => certificateClient.Value;

    public KeyClient KeyClient => keyClient.Value;

    public SecretClient SecretClient => secretClient.Value;

    public async Task<AzureKeyVaultKey> GetKey(string keyName, string secretName = null)
    {
        return new AzureKeyVaultKey
        {
            Key = (await KeyClient.GetKeyAsync(keyName)).Value.Key,
            Secret = secretName != null ? await GetSecret(secretName) : null
        };
    }

    public async Task<string> GetSecret(string secretName)
    {
        return (await SecretClient.GetSecretAsync(secretName)).Value.Value;
    }

    public async Task<AzureKeyVaultCertificate> GetCertificate(string certificateName, bool includeKey = true,
        bool includeSecret = true)
    {
        var certificateBundleResponse = await CertificateClient.GetCertificateAsync(certificateName);
        var certificateBundle = certificateBundleResponse.Value;

        string Parse(Uri id)
        {
            Assert.True(id.Segments.Length < 3, $"The key/secret id {id.ToString().DoubleQuote()} does not contain a valid name");
            return id.Segments[2].TrimEnd('/');
        }

        return new AzureKeyVaultCertificate
        {
            Cer = certificateBundle.Cer,
            X509Thumbprint = certificateBundle.Properties.X509Thumbprint,
            Key = includeKey && certificateBundle.KeyId != null
                ? await GetKey(Parse(certificateBundle.KeyId))
                : null,
            Secret = includeSecret && certificateBundle.SecretId != null
                ? await GetSecret(Parse(certificateBundle.SecretId))
                : null
        };
    }
}
