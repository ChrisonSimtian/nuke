using System;
using System.Collections.Generic;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsWorkflowDispatchTrigger : GitHubActionsDetailedTrigger
{
    [Obsolete($"Set {nameof(Inputs)} instead. Removed in 2027.x.x.")]
    public string[] OptionalInputs { get; set; } = new string[0];

    [Obsolete($"Set {nameof(Inputs)} instead. Removed in 2027.x.x.")]
    public string[] RequiredInputs { get; set; } = new string[0];

    public GitHubActionsWorkflowDispatchInput[] Inputs { get; set; } = new GitHubActionsWorkflowDispatchInput[0];

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("workflow_dispatch:");
        using (writer.Indent())
        {
            writer.WriteLine("inputs:");
            using (writer.Indent())
            {
                GetInputs().ForEach(WriteInput);
            }
        }

        void WriteInput(GitHubActionsWorkflowDispatchInput input)
        {
            writer.WriteLine($"{input.Name}:");
            using (writer.Indent())
            {
                var description = input.Description ?? input.Name.SplitCamelHumpsWithKnownWords().JoinSpace();
                writer.WriteLine($"description: {description.DoubleQuote()}");
                writer.WriteLine($"required: {input.Required.ToString().ToLowerInvariant()}");
                if (input.Type != GitHubActionsInputType.String)
                    writer.WriteLine($"type: {input.Type.GetValue()}");
                if (input.Default != null)
                    writer.WriteLine($"default: {input.Default}");
                if (input.Type == GitHubActionsInputType.Choice)
                {
                    writer.WriteLine("options:");
                    using (writer.Indent())
                        input.Options.ForEach(x => writer.WriteLine($"- {x.DoubleQuote()}"));
                }
            }
        }
    }

    // Legacy OptionalInputs/RequiredInputs emit first as untyped string inputs, preserving existing
    // output; typed Inputs follow. Direct consumers of the obsolete arrays keep working.
    private IEnumerable<GitHubActionsWorkflowDispatchInput> GetInputs()
    {
#pragma warning disable CS0618 // deliberate bridge for the obsolete legacy arrays
        foreach (var input in OptionalInputs)
            yield return new GitHubActionsWorkflowDispatchInput { Name = input, Required = false };
        foreach (var input in RequiredInputs)
            yield return new GitHubActionsWorkflowDispatchInput { Name = input, Required = true };
#pragma warning restore CS0618

        foreach (var input in Inputs)
            yield return input;
    }
}
