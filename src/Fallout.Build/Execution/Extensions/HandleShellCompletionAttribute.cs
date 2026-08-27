using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.CI;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using static Fallout.Common.Constants;

namespace Fallout.Common.Execution;

internal class HandleShellCompletionAttribute : BuildExtensionAttributeBase, IOnBuildCreated
{
    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        if (BuildServerConfigurationGeneration.IsActive)
            return;

        if (IsLegacy(Build.RootDirectory))
        {
            Host.Error(
                new[]
                {
                    "The old-style .nuke configuration is no longer supported.",
                    "You can convert to the new-style .nuke directory by calling:",
                    "   fallout :update"
                }.JoinNewLine());
            Environment.Exit(exitCode: -1);
        }
        else if (Build.BuildProjectFile != null)
        {
            var buildSchema = SchemaUtility.GetJsonString(Build);
            var buildSchemaFile = GetBuildSchemaFile(Build.RootDirectory);
            buildSchemaFile.WriteAllText(buildSchema);

            var parametersFile = GetDefaultParametersFile(Build.RootDirectory);
            if (!parametersFile.Exists())
            {
                parametersFile.WriteAllText($$"""
                    {
                      "$schema": "./{{BuildSchemaFileName}}"
                    }
                    """);
            }
        }
        else if (ParameterService.GetPositionalArgument<string>(0) == ":complete")
        {
            var schema = SchemaUtility.GetJsonDocument(Build);
            var profileNames = GetProfileNames(Build.RootDirectory);
            var completionItems = CompletionUtility.GetItemsFromSchema(schema, profileNames);

            var words = EnvironmentInfo.CommandLineArguments.Skip(2).JoinSpace();
            var relevantCompletionItems = CompletionUtility.GetRelevantItems(words, completionItems);
            foreach (var item in relevantCompletionItems)
                Console.WriteLine(item);

            Environment.Exit(exitCode: 0);
        }

        if (ParameterService.GetParameter<bool>(CompletionParameterName))
            Environment.Exit(exitCode: 0);
    }
}
