// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// Represents an item in the solution model, either a project or a solution folder.
/// </summary>
public abstract class SolutionItemModel : PropertyContainerModel
{
    private Guid? id;
    private Guid? defaultId;

    private protected SolutionItemModel(SolutionModel solutionModel, SolutionFolderModel? parent)
    {
        Argument.ThrowIfNull(solutionModel, nameof(solutionModel));
        Solution = solutionModel;
        Parent = parent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionItemModel"/> class.
    /// Copy constructor. This does a shallow copy of the Parent.
    /// </summary>
    /// <param name="solutionModel">The new solution model parent.</param>
    /// <param name="itemModel">The item model to copy.</param>
    private protected SolutionItemModel(SolutionModel solutionModel, SolutionItemModel itemModel)
        : base(itemModel)
    {
        Solution = solutionModel;
        id = itemModel.id;
        defaultId = itemModel.defaultId;

        // This is a shallow copy of the parent, it needs to be swapped out to finish the deep copy.
        // But we can't find the new parent until all copy constructors have been called.
        Parent = itemModel.Parent;
    }

    /// <summary>
    /// Gets the solution model that contains this item.
    /// </summary>
    public SolutionModel Solution { get; }

    /// <summary>
    /// Gets the parent solution folder.
    /// </summary>
    public SolutionFolderModel? Parent { get; private set; }

    /// <summary>
    /// Gets or sets the unique Id of the item within the solution.
    /// </summary>
    /// <remarks>
    /// Set to <see cref="Guid.Empty"/> to use default guid.
    /// </remarks>
    public Guid Id
    {
        get => id ?? DefaultId;

        set
        {
            if (value != (id ?? defaultId))
            {
                if (Solution.FindItemById(value) is not null)
                {
                    throw new SolutionArgumentException(string.Format(Errors.DuplicateItemRef_Args2, value, GetType().Name),
                        nameof(value), SolutionErrorType.DuplicateItemRef);
                }

                Guid? oldId = id ?? defaultId;
                id = value == DefaultId ? null : value.NullIfEmpty();
                Solution.OnUpdateId(this, oldId);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the Id is the default Id generated from the ItemRef.
    /// </summary>
    public bool IsDefaultId => id is null;

    /// <summary>
    /// Gets the display name of the item. If there is a filename it will be used, otherwise the actual display name.
    /// </summary>
    public abstract string ActualDisplayName { get; }

    /// <summary>
    /// Gets the project type id of the item.
    /// </summary>
    public abstract Guid TypeId { get; }

    internal SolutionItemModel BeSolutionItemModel => this;

    /// <summary>
    /// Gets a unique reference to the item in the solution.
    /// This is designed as a replacement to Id and provides a human readable reference to the item.
    /// </summary>
    internal abstract string ItemRef { get; }

    private Guid DefaultId => defaultId ??= GetDefaultId();

    /// <summary>
    /// Moves the item to a new folder.
    /// </summary>
    /// <param name="folder">The folder to move to.</param>
    public void MoveToFolder(SolutionFolderModel? folder)
    {
        Solution.ValidateInModel(folder);
        if (ReferenceEquals(Parent, folder))
        {
            return;
        }

        // Check for moving parent folder under itself.
        for (SolutionFolderModel? parents = folder; parents is not null; parents = parents.Parent)
        {
            if (ReferenceEquals(parents, this))
            {
                throw new SolutionArgumentException(Errors.CannotMoveFolderToChildFolder, nameof(folder),
                    SolutionErrorType.CannotMoveFolderToChildFolder);
            }
        }

        SolutionFolderModel? oldParent = Parent;
        try
        {
            Parent = folder;

            // Reevaulate the id.
            if (id == DefaultId)
            {
                id = null;
            }

            if (this is SolutionProjectModel thisProject)
            {
                Solution.ValidateProjectName(thisProject);
            }
        }
        catch
        {
            // Revert the change if it fails validation.
            Parent = oldParent;
            throw;
        }

        OnParentChanged();
    }

    internal virtual void OnItemRefChanged()
    {
        defaultId = null;
        if (id is null)
        {
            Id = Guid.Empty;
        }
    }

    private protected abstract Guid GetDefaultId();

    private protected virtual void OnParentChanged()
    {
    }
}
