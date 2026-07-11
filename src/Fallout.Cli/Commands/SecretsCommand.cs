using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Fallout.Cli.Prompts;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using static Fallout.Common.Constants;
using static Fallout.Common.Utilities.EncryptionUtility;

namespace Fallout.Cli.Commands;

// TODO: unlock prompt
// TODO: environment variable name
// TODO: profile vs. environment
// TODO: fallout :profile <name>

/// <summary>
/// <c>fallout :secrets</c>: interactively encrypts secret parameters into the (optionally profile-scoped)
/// parameters file, managing the keychain password.
/// </summary>
internal sealed class SecretsCommand : IFalloutCommand
{
    private const string SaveAndExit = "<Save & Exit>";
    private const string DiscardAndExit = "<Discard & Exit>";
    private const string DeletePasswordAndExit = "<Delete Password & Exit>";

    private readonly IConsolePrompts _prompts;

    public SecretsCommand(IConsolePrompts prompts) => _prompts = prompts;

    public string Name => "secrets";

    // ReSharper disable once CognitiveComplexity
    public Task<int> ExecuteAsync(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
        => Task.FromResult(Execute(args, rootDirectory, buildScript));

    private int Execute(string[] args, AbsolutePath rootDirectory, AbsolutePath buildScript)
    {
        var secretParameters = CompletionUtility.GetItemsFromSchema(
                GetBuildSchemaFile(rootDirectory.NotNull("No root directory")),
                filter: x => x.Value.TryGetProperty("default", out _))
            .Select(x => x.Key).ToList();
        if (secretParameters.Count == 0)
        {
            Host.Information($"There are no parameters marked with {nameof(SecretAttribute)}");
            return 0;
        }

        var profile = args.SingleOrDefault();
        var parametersFile = profile == null
            ? GetDefaultParametersFile(rootDirectory)
            : GetParametersProfileFile(rootDirectory, profile);

        var generatedPassword = false;
        var credentialStoreName = GetCredentialStoreName(rootDirectory, profile);
        var legacyCredentialStoreName = GetLegacyCredentialStoreName(rootDirectory, profile);
        var password = CredentialStore.TryGetPassword(credentialStoreName);
        var fromLegacyCredentialStore = false;
        if (password == null)
        {
            password = CredentialStore.TryGetPassword(legacyCredentialStoreName);
            fromLegacyCredentialStore = password != null;
            if (fromLegacyCredentialStore)
            {
                Host.Warning(
                    $"Found credentials under legacy keychain entry '{legacyCredentialStoreName}'. " +
                    $"Migrating to '{credentialStoreName}'. The legacy entry will no longer be read in 11.0.");
            }
        }

        var fromCredentialStore = password != null;
        password ??= CredentialStore.CreateNewPassword(out generatedPassword);
        var existingSecrets = LoadSecrets(secretParameters, password, parametersFile);

        if (EnvironmentInfo.IsOsx && existingSecrets.Count == 0 && !fromCredentialStore)
        {
            if (generatedPassword || _prompts.PromptForConfirmation($"Save password to keychain? (associated with '{rootDirectory}')"))
                CredentialStore.SavePassword(credentialStoreName, password);
        }
        else if (fromLegacyCredentialStore)
        {
            // Forward the legacy password to the canonical entry so future loads find it directly.
            CredentialStore.SavePassword(credentialStoreName, password);
        }

        var options = secretParameters
            .Concat(SaveAndExit, DiscardAndExit)
            .Concat(fromCredentialStore ? DeletePasswordAndExit : null).WhereNotNull().ToList();

        var addedSecrets = new Dictionary<string, string>();
        while (true)
        {
            var choice = _prompts.PromptForChoice(
                "Choose secret parameter to enter value:",
                options.Select(x => (x, addedSecrets.ContainsKey(x) || existingSecrets.ContainsKey(x) ? $"* {x}" : x)).ToArray());

            if (!choice.EqualsAnyOrdinalIgnoreCase(SaveAndExit, DiscardAndExit, DeletePasswordAndExit))
            {
                addedSecrets[choice] = _prompts.PromptForSecret(choice);
            }
            else
            {
                if (choice == SaveAndExit)
                    SaveSecrets(addedSecrets, password, parametersFile);

                if (choice == DeletePasswordAndExit)
                    CredentialStore.DeletePassword(credentialStoreName);

                if (addedSecrets.Any())
                    Host.Information("Remember to clear your clipboard!");

                return 0;
            }
        }
    }

    private static Dictionary<string, string> LoadSecrets(IReadOnlyCollection<string> secretParameters, string password, AbsolutePath parametersFile)
    {
        var jobject = parametersFile.ReadJsonObject();
        return jobject
            .Where(x => secretParameters.Contains(x.Key))
            .ToDictionary(x => x.Key, x => Decrypt(x.Value.GetValue<string>(), password, x.Key));
    }

    private static void SaveSecrets(Dictionary<string, string> secrets, string password, AbsolutePath parametersFile)
    {
        parametersFile.UpdateJsonObject(obj =>
        {
            foreach (var (name, secret) in secrets)
                obj[name] = Encrypt(secret, password);
        });
    }
}
