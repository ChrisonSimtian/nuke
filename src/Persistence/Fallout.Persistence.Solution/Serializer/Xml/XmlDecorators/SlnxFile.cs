// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml;
using Fallout.Persistence.Solution.Model;
using Fallout.Persistence.Solution.Utilities;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

/// <summary>
/// Creates an Xml DOM model for reading and updating the slnx file.
/// </summary>
[DebuggerDisplay("{Solution}")]
internal sealed class SlnxFile
{
    internal const int CurrentVersion = 1;

    internal SlnxFile(
        XmlDocument xmlDocument,
        SlnxSerializerSettings serializationSettings,
        StringTable? stringTable,
        string? fullPath)
    {
        Document = xmlDocument;
        FullPath = fullPath;
        StringTable = stringTable ?? new StringTable().WithSolutionConstants();

        XmlElement? xmlSolution = Document.DocumentElement;
        if (xmlSolution is not null && Keywords.ToKeyword(xmlSolution.Name) == Keyword.Solution)
        {
            Solution = new XmlSolution(this, xmlSolution);
            Solution.UpdateFromXml();

            // This is a model part, but needs to be calculated before it can properly turn into a model.
            // These are used to calculate the actual project types from a project's Type attribute.
            ProjectTypes = Solution.GetProjectTypeTable();
        }
        else
        {
            throw new SolutionException(Errors.NotSolution, SolutionErrorType.NotSolution)
            {
                File = FullPath
            };
        }

        SerializationSettings = GetDefaultSerializationSettings(serializationSettings);
    }

    internal string? FullPath { get; }

    // Slnx file version.
    internal Version? FileVersion { get; set; }

    internal XmlDocument Document { get; }

    internal XmlSolution? Solution { get; private set; }

    internal SlnxSerializerSettings SerializationSettings { get; }

    internal StringTable StringTable { get; }

    internal ProjectTypeTable ProjectTypes { get; private set; }

    // Keep track of user project and file paths to preserve the user's path separators.
    internal Dictionary<string, string> UserPaths { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal bool Tarnished { get; private set; }

    internal SolutionModel ToModel()
    {
        UserPaths.Clear();
        SolutionModel model = Solution?.ToModel() ?? new SolutionModel
        {
            StringTable = StringTable
        };

        model.SerializerExtension = new SlnXmlModelExtension(SolutionSerializers.SlnXml, SerializationSettings, root: this);
        return model;
    }

    /// <summary>
    /// Converts a model project path to use the slashes the user provides, or default to forward slashes.
    /// </summary>
    internal string ConvertToUserPath(string projectPath)
    {
        return UserPaths.TryGetValue(projectPath, out string? userProjectPath)
            ? userProjectPath
            : PathExtensions.ConvertModelToForwardSlashPath(projectPath);
    }

    /// <summary>
    /// Update the Xml DOM with changes from the model.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if any changes were made to the XML.
    /// </returns>
    internal bool ApplyModel(SolutionModel model)
    {
        ProjectTypes = model.ProjectTypeTable;

        bool modified = false;
        if (Solution is null)
        {
            // Make the solution element the root element of the document.
            XmlElement xmlSolution = Document.CreateElement(Keyword.Solution.ToXmlString());
            _ = Document.AppendChild(xmlSolution);
            Solution = new XmlSolution(this, xmlSolution);
            Solution.UpdateFromXml();
            modified = true;
        }

        modified |= Solution.ApplyModelToXml(model);
        return modified;
    }

    internal string ToXmlString()
    {
        return Document.OuterXml;
    }

    // Fill out default values.
    private SlnxSerializerSettings GetDefaultSerializationSettings(SlnxSerializerSettings inputSettings)
    {
        string newLineChars = Environment.NewLine;
        string newIndentChars = "  ";
        if ((inputSettings.IndentChars is null || inputSettings.NewLine is null) &&
            Solution is not null &&
            Solution.TryGetFormatting(out StringSpan newLine, out StringSpan indent))
        {
            newLineChars = newLine.ToString();
            newIndentChars = indent.ToString();
        }

        return inputSettings with
        {
            PreserveWhitespace = inputSettings.PreserveWhitespace ?? Document.PreserveWhitespace,
            IndentChars = inputSettings.IndentChars ?? newIndentChars,
            NewLine = inputSettings.NewLine ?? newLineChars,
        };
    }
}
