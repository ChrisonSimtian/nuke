// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Model;

internal sealed partial class SolutionConfigurationMap
{
    // Keeps track of changes to all project configuration dimensions.
    // This is used to tell if the values are the same and configuration rules can be created.
    private struct ProjectDiffTracker
    {
        internal DimensionDiffTracker<string> BuildTypeTracker;
        internal DimensionDiffTracker<string> PlatformTracker;
        internal DimensionDiffTracker<bool> BuildTracker;
        internal DimensionDiffTracker<bool> DeployTracker;

        internal readonly bool HasDifferences => BuildTypeTracker.HasDifferences || PlatformTracker.HasDifferences ||
                                                 BuildTracker.HasDifferences || DeployTracker.HasDifferences;

        internal readonly bool HasSame => BuildTypeTracker.SameDifference || PlatformTracker.SameDifference ||
                                          BuildTracker.SameDifference || DeployTracker.SameDifference;

        // The ProjectDiffTracker is a struct, so this passes the array to
        // make sure this actually clears the diffs and a boxed copy.
        internal static void ClearDiffs(BuildDimension dimension, ProjectDiffTracker[] trackers)
        {
            for (int i = 0; i < trackers.Length; i++)
            {
                ref ProjectDiffTracker tracker = ref trackers[i];
                tracker.ClearDiffs(dimension);
            }
        }

        // The ProjectDiffTracker is a struct, so this passes the array to
        // make sure this actually clears the diffs and a boxed copy.
        internal static void ClearDiffs(ProjectDiffTracker[] trackers)
        {
            for (int i = 0; i < trackers.Length; i++)
            {
                ref ProjectDiffTracker tracker = ref trackers[i];
                tracker.ClearDiffs();
            }
        }

        internal void ObserveDifferentValue(in ProjectConfigMapping currentMapping)
        {
            BuildTypeTracker.ObserveDifferentValue(currentMapping.BuildType);
            PlatformTracker.ObserveDifferentValue(PlatformNames.Canonical(currentMapping.Platform));
            BuildTracker.ObserveDifferentValue(currentMapping.Build);
            DeployTracker.ObserveDifferentValue(currentMapping.Deploy);
        }

        internal void ObserveValue(in ProjectConfigMapping expectedMapping, in ProjectConfigMapping currentMapping)
        {
            BuildTypeTracker.ObserveValue(expectedMapping.BuildType, currentMapping.BuildType);
            PlatformTracker.ObserveValue(PlatformNames.Canonical(expectedMapping.Platform),
                PlatformNames.Canonical(currentMapping.Platform));

            BuildTracker.ObserveValue(expectedMapping.Build, currentMapping.Build);
            DeployTracker.ObserveValue(expectedMapping.Deploy, currentMapping.Deploy);
        }

        internal void ClearDiffs()
        {
            BuildTypeTracker.ClearDifferences();
            PlatformTracker.ClearDifferences();
            BuildTracker.ClearDifferences();
            DeployTracker.ClearDifferences();
        }

        internal void ClearDiffs(BuildDimension dimension)
        {
            switch (dimension)
            {
                case BuildDimension.BuildType: BuildTypeTracker.ClearDifferences(); break;

                case BuildDimension.Platform: PlatformTracker.ClearDifferences(); break;

                case BuildDimension.Build: BuildTracker.ClearDifferences(); break;

                case BuildDimension.Deploy: DeployTracker.ClearDifferences(); break;
            }
        }
    }
}
