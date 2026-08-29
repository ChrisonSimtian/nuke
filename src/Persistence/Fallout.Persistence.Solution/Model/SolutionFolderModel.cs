// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Utilities;

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// Represents a solution folder in the solution model.
/// </summary>
public sealed class SolutionFolderModel : SolutionItemModel
{
    private const string CycleBreaker = "***"; // to ensure no cycles
    private string? itemRef; // folder fullPath
    private List<string>? files;
    private string name;

    internal SolutionFolderModel(SolutionModel solutionModel, string name, SolutionFolderModel? parent)
        : base(solutionModel, parent)
    {
        Argument.ThrowIfNullOrEmpty(name, nameof(name));
        this.name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionFolderModel"/> class.
    /// Copy constructor.
    /// </summary>
    /// <param name="solutionModel">The new solution model parent.</param>
    /// <param name="folderModel">The folder model to copy.</param>
    internal SolutionFolderModel(SolutionModel solutionModel, SolutionFolderModel folderModel)
        : base(solutionModel, folderModel.BeSolutionItemModel)
    {
        name = folderModel.name;
        if (folderModel.Files is not null)
        {
            files = [.. folderModel.Files];
        }
    }

    /// <summary>
    /// Gets the files in this solution folder.
    /// </summary>
    public IReadOnlyList<string>? Files => files;

    /// <summary>
    /// Gets or sets the name of the solution folder.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            Argument.ThrowIfNullOrEmpty(value, nameof(value));
            SolutionModel.ValidateName(value.AsSpan());

            if (name == value)
            {
                return;
            }

            string testName = $"{Parent?.ItemRef ?? "/"}{value}/";
            if (Solution.FindFolder(testName) is not null)
            {
                throw new SolutionArgumentException(string.Format(Errors.DuplicateItemRef_Args2, testName, "Folder"),
                    nameof(value), SolutionErrorType.DuplicateItemRef);
            }

            string oldName = name;
            try
            {
                name = value;
                OnItemRefChanged();
            }
            catch (Exception)
            {
                // On error revert the name.
                name = oldName;
                throw;
            }
        }
    }

    /// <summary>
    /// Gets a unique reference to this folder in the solution.
    /// </summary>
    public string Path => ItemRef;

    /// <inheritdoc/>
    public override string ActualDisplayName => Name;

    /// <inheritdoc/>
    public override Guid TypeId => ProjectTypeTable.SolutionFolder;

    /// <inheritdoc/>
    internal override string ItemRef
    {
        get
        {
            if (itemRef is not null)
            {
                return itemRef;
            }

            if (Parent is not null)
            {
                itemRef = CycleBreaker;
                string parentRef = Parent.ItemRef;
                if (!ReferenceEquals(parentRef, CycleBreaker))
                {
                    itemRef = $"{parentRef}{Name}/";
                    return itemRef;
                }
            }

            // no parent, or part of cycle move it on top.
            // potential duplicates in this case will be ignored/merged on save.
            itemRef = $"/{Name}/";
            return itemRef;
        }
    }

    /// <summary>
    /// Adds a file to this solution folder.
    /// </summary>
    /// <param name="file">The file to add.</param>
    public void AddFile(string file)
    {
        files ??= [];

        if (!files.Contains(file))
        {
            files.Add(file);
        }
    }

    /// <summary>
    /// Removes a file from this solution folder.
    /// </summary>
    /// <param name="file">The file to remove.</param>
    /// <returns><see langword="true"/> if the item was found and removed.</returns>
    public bool RemoveFile(string file)
    {
        return files is not null && files.Remove(file);
    }

    internal override void OnItemRefChanged()
    {
        base.OnItemRefChanged();
        itemRef = null;

        // Recursively update all children.
        foreach (SolutionItemModel item in Solution.SolutionItems)
        {
            if (ReferenceEquals(item.Parent, this))
            {
                item.OnItemRefChanged();
            }
        }
    }

    private protected override Guid GetDefaultId()
    {
        Guid parentId = Parent is null ? Guid.Empty : Parent.Id;
        return DefaultIdGenerator.CreateIdFrom(parentId, Name);
    }

    private protected override void OnParentChanged()
    {
        base.OnParentChanged();
        OnItemRefChanged();
    }
}
