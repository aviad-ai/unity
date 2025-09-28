using UnityEngine;
using UnityEditor;

namespace Aviad
{
    [InitializeOnLoad]
    public static class RunnerPlayModeValidator
    {
        static RunnerPlayModeValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Only check when entering play mode
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Find all AviadRunner instances in the scene
                Runner[] runners = Object.FindObjectsByType<Runner>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );
                foreach (var runner in runners)
                {
                    if (runner.saveToStreamingAssets &&
                        (runner.modelAsset == null ||
                         !System.IO.File.Exists(ModelRuntime.GetExpectedModelPath(runner.modelAsset.modelUrl))))
                    {
                        PackageLogger.Error(
                            $"Cannot enter Play Mode: The model asset '{runner.modelAsset?.name ?? "NULL"}' is missing on '{runner.name}'. Please download it from the ModelConfiguration Inspector first.");

                        // Cancel play mode
                        EditorApplication.isPlaying = false;
                        return;
                    }
                }
            }
        }
    }
}