// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Model;

internal sealed partial class SolutionConfigurationMap
{
    // Keeps track of changes to a specific dimension value.
    // This is used to tell if the values are the same and a configuration rule can be created.
    private struct DimensionDiffTracker<T>
    {
        private int itemsChecked;
        private int differences;
        private T firstDifferent;
        private bool anyDifferent;

        // There was at least one item that was different than the expected value.
        internal readonly bool HasDifferences => differences > 0;

        // All items are different than expected, but they are the same as each other.
        internal readonly bool SameDifference => !anyDifferent && itemsChecked == differences && itemsChecked > 0;

        internal void ObserveDifferentValue(T current)
        {
            itemsChecked++;
            differences++;
            if (differences == 1)
            {
                firstDifferent = current;
            }
            else
            {
                anyDifferent = anyDifferent || !EqualityComparer<T>.Default.Equals(firstDifferent, current);
            }
        }

        internal void ObserveValue(T expected, T current)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, current))
            {
                ObserveDifferentValue(current);
            }
            else
            {
                itemsChecked++;
            }
        }

        internal void ClearDifferences()
        {
            differences = 0;
            itemsChecked = 0;
            anyDifferent = false;
            firstDifferent = default!;
        }

        internal readonly bool TryGetSame(out T sameChanged)
        {
            sameChanged = firstDifferent;
            return SameDifference;
        }

        internal readonly bool TryGetSame(DimensionDiffTracker<T> alternate, out T sameChanged)
        {
            if (TryGetSame(out sameChanged))
            {
                return true;
            }

            if (HasDifferences)
            {
                return alternate.TryGetSame(out sameChanged);
            }

            return false;
        }
    }
}
