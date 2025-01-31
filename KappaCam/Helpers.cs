using UnityEngine;
using Koenigz.PerfectCulling;
using System.Collections.Generic;
using Koenigz.PerfectCulling.EFT;
using EFT.Communications;

namespace KappaCam {
    public class Helpers {
        /// <summary>
        /// Toggles culling on or off, depending on the current <paramref name="cullingIsDisabled"/> state.
        /// </summary>
        /// <param name="cullingIsDisabled">A boolean tracking whether culling is currently disabled.</param>
        /// <param name="allDisablerObjects">All <see cref="DisablerCullingObjectBase"/> objects found in the scene.</param>
        /// <param name="previouslyEnabledBakeGroups">List of culling bake groups that were already enabled before disabling culling.</param>
        public static void ToggleCulling(
            ref bool cullingIsDisabled,
            DisablerCullingObjectBase[] allDisablerObjects,
            List<PerfectCullingBakeGroup> previouslyEnabledBakeGroups) {
            if (!cullingIsDisabled) {
                DisableCulling(allDisablerObjects, previouslyEnabledBakeGroups);
                cullingIsDisabled = true;
                Debug.Log("Occlusion culling is now disabled.");
            } else {
                EnableCulling(allDisablerObjects, previouslyEnabledBakeGroups);
                cullingIsDisabled = false;
                Debug.Log("Occlusion culling is now enabled.");
            }
        }

        /// <summary>
        /// Disables occlusion culling across all <see cref="DisablerCullingObjectBase"/> triggers
        /// and updates the <see cref="PerfectCullingAdaptiveGrid"/> so that nothing is culled.
        /// </summary>
        public static void DisableCulling(
            DisablerCullingObjectBase[] allDisablerObjects,
            List<PerfectCullingBakeGroup> previouslyEnabledBakeGroups) {
            int count = 0;
            foreach (var cullingObject in allDisablerObjects) {
                if (cullingObject.HasEntered) continue;
                cullingObject.SetComponentsEnabled(true);
                count++;
            }
            Debug.Log($"Disabled {count} culling triggers.");

            // Turn off the main culling grid so nothing gets occluded
            var grid = Object.FindObjectOfType<PerfectCullingAdaptiveGrid>();
            if (grid != null && grid.RuntimeGroupMapping != null) {
                foreach (var sceneGroup in grid.RuntimeGroupMapping) {
                    // Deactivate the group
                    sceneGroup.enabled = false;

                    // Force every BakeGroup within to be enabled
                    foreach (var bakeGroup in sceneGroup.bakeGroups) {
                        if (!bakeGroup.IsEnabled) {
                            bakeGroup.IsEnabled = true;
                        } else {
                            // If it was already on, remember it so we can restore it
                            previouslyEnabledBakeGroups.Add(bakeGroup);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Re-enables occlusion culling by restoring triggers and <see cref="PerfectCullingAdaptiveGrid"/> settings
        /// to their original states.
        /// </summary>
        public static void EnableCulling(
            DisablerCullingObjectBase[] allDisablerObjects,
            List<PerfectCullingBakeGroup> previouslyEnabledBakeGroups) {
            int count = 0;
            foreach (var cullingObject in allDisablerObjects) {
                if (cullingObject.HasEntered) continue;
                cullingObject.SetComponentsEnabled(false);
                count++;
            }
            Debug.Log($"Re-enabled {count} culling triggers.");

            var grid = Object.FindObjectOfType<PerfectCullingAdaptiveGrid>();
            
            if (grid != null && grid.RuntimeGroupMapping != null) {
                foreach (var sceneGroup in grid.RuntimeGroupMapping) {
                    // Re-activate
                    sceneGroup.enabled = true;

                    foreach (var bakeGroup in sceneGroup.bakeGroups) {
                        // If it’s currently enabled and we didn't record it before, turn it off
                        if (bakeGroup.IsEnabled && !previouslyEnabledBakeGroups.Contains(bakeGroup)) {
                            bakeGroup.IsEnabled = false;
                        } else {
                            // If it was on previously, remove it from our tracking
                            previouslyEnabledBakeGroups.Remove(bakeGroup);
                        }
                    }
                }
            }

            // Clear the list so it doesn't grow repeatedly
            previouslyEnabledBakeGroups.Clear();
        }
    }
}
